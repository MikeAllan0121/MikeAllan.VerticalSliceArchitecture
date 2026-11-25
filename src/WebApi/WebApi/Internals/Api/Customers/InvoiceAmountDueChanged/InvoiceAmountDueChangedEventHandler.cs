using MikeAllan.VerticalSliceArchitecture.Domain.Concurrency;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.DomainEvents;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Internals.EventualConsistency;

namespace MikeAllan.VerticalSliceArchitecture.Internals.Api.Customers.InvoiceAmountDueChanged;

/// <summary>
/// Keeps a customer's outstanding balance in step when one of their invoices is issued or paid.
/// Registered in <c>Customers/Setup.cs</c>.
/// </summary>
/// <remarks>
/// Two invoices of one customer can change at once, so two of these handlers can race: each loads the
/// customer and their invoices, recalculates, and saves. The customer's row version makes the later save
/// fail rather than overwrite the balance with one computed from a stale read. On that conflict the handler
/// starts again with a fresh context, so it recalculates from what is committed now. Whichever handler saves
/// last therefore read every invoice change committed before it, and the balance ends correct. There is no
/// caller to hand a 409 to: after <see cref="MaxAttempts"/> it gives up with
/// <see cref="ConcurrencyErrors.Conflict"/>, which the eventual-consistency middleware logs.
/// </remarks>
internal sealed class InvoiceAmountDueChangedEventHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<InvoiceAmountDueChangedEventHandler> logger)
    : IDomainEventHandler<InvoiceAmountDueChangedEvent>
{
    /// <summary>Attempts before giving up; a conflict means another save just committed, so one more usually wins.</summary>
    internal const int MaxAttempts = 5;

    public static readonly DomainError CustomerNotFound = new NotFoundError(
        "InvoiceAmountDueChanged.CustomerNotFound",
        "Customer not found");

    public async Task HandleAsync(InvoiceAmountDueChangedEvent domainEvent, CancellationToken ct)
    {
        logger.InvoiceAmountDueChanged(domainEvent.Invoice.Id.Value, domainEvent.Invoice.AmountDue);

        for (int attempt = 1; ; attempt++)
        {
            // Own scope, so its own DbContext: see IDomainEventHandler<TEvent> for why. A new one per attempt,
            // so a retry reloads the customer instead of reusing the stale tracked copy.
            using IServiceScope scope = scopeFactory.CreateScope();
            ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            Customer? customer = await dbContext.Customers
                .ById(domainEvent.Invoice.CustomerId)
                .FirstOrDefaultAsync(ct);

            if (customer is null)
                throw new EventualConsistencyException(CustomerNotFound);

            customer.RecalculateOutstandingBalance();

            try
            {
                await dbContext.SaveChangesAsync(ct);
                return;
            }
            catch (DbUpdateConcurrencyException) when (attempt < MaxAttempts)
            {
                logger.OutstandingBalanceConflict(customer.Id.Value, attempt);
            }
            catch (DbUpdateConcurrencyException)
            {
                throw new EventualConsistencyException(ConcurrencyErrors.Conflict);
            }
        }
    }
}

// Compile-time logging: the source generator emits a level-checked, allocation-free
// method, which also satisfies CA1873 (the generated call is not an ILogger.Log* shape).
internal static partial class InvoiceAmountDueChangedEventHandlerLog
{
    [LoggerMessage(LogLevel.Information,
        "InvoiceAmountDueChangedEventHandler: invoice {InvoiceId} amount due changed to {AmountDue}")]
    public static partial void InvoiceAmountDueChanged(this ILogger logger, Guid invoiceId, decimal amountDue);

    [LoggerMessage(LogLevel.Information,
        "InvoiceAmountDueChangedEventHandler: customer {CustomerId} changed during attempt {Attempt}; recalculating")]
    public static partial void OutstandingBalanceConflict(this ILogger logger, Guid customerId, int attempt);
}
