using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.DomainEvents;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.EventualConsistency;

namespace MikeAllan.VerticalSliceArchitecture.Api.Customers.InvoiceAmountDueChanged;

/// <summary>
/// Keeps a customer's outstanding balance in step when one of their invoices is issued or paid.
/// Registered in <c>Customers/Setup.cs</c>.
/// </summary>
public sealed class InvoiceAmountDueChangedEventHandler(
    IServiceScopeFactory scopeFactory,
    ILogger<InvoiceAmountDueChangedEventHandler> logger)
    : IDomainEventHandler<InvoiceAmountDueChangedEvent>
{
    public static readonly DomainError CustomerNotFound = new NotFoundError(
        "InvoiceAmountDueChanged.CustomerNotFound",
        "Customer not found");

    public async Task HandleAsync(InvoiceAmountDueChangedEvent domainEvent, CancellationToken ct)
    {
        logger.InvoiceAmountDueChanged(domainEvent.Invoice.Id.Value, domainEvent.Invoice.AmountDue);

        // Own scope, so its own DbContext: see IDomainEventHandler<TEvent> for why.
        using IServiceScope scope = scopeFactory.CreateScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Customer? customer = await dbContext.Customers
            .ById(domainEvent.Invoice.CustomerId)
            .FirstOrDefaultAsync(ct);

        if (customer is null)
            throw new EventualConsistencyException(CustomerNotFound);

        customer.RecalculateOutstandingBalance();
        await dbContext.SaveChangesAsync(ct);
    }
}

// Compile-time logging: the source generator emits a level-checked, allocation-free
// method, which also satisfies CA1873 (the generated call is not an ILogger.Log* shape).
internal static partial class InvoiceAmountDueChangedEventHandlerLog
{
    [LoggerMessage(LogLevel.Information,
        "InvoiceAmountDueChangedEventHandler: invoice {InvoiceId} amount due changed to {AmountDue}")]
    public static partial void InvoiceAmountDueChanged(this ILogger logger, Guid invoiceId, decimal amountDue);
}
