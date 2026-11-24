using MikeAllan.VerticalSliceArchitecture.Domain.Aggregates;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;

namespace MikeAllan.VerticalSliceArchitecture.Domain.Invoices;

// Register new strongly typed IDs in ApplicationDbContext.ConfigureConventions
public readonly record struct InvoiceId(Guid Value) : IGuidId<InvoiceId>
{
    public static InvoiceId From(Guid value) => new(value);
}

public sealed class Invoice : AggregateRoot<InvoiceId>
{
    private readonly List<InvoiceLine> _lines = [];
    private readonly List<Payment> _payments = [];

    public CustomerId CustomerId { get; private set; }
    public InvoiceStatus Status { get; private set; }

    /// <summary>The sum of each line's quantity times unit price. Stored, so it can be sorted on.</summary>
    public decimal Total { get; private set; }

    public decimal AmountPaid { get; private set; }

    /// <summary>
    /// What is still owed. Computed and not mapped, so it can't appear in a query that EF Core translates to
    /// SQL: write <c>Total - AmountPaid</c> there instead.
    /// </summary>
    public decimal AmountDue => Total - AmountPaid;

    public IReadOnlyList<InvoiceLine> Lines => _lines.AsReadOnly();
    public IReadOnlyList<Payment> Payments => _payments.AsReadOnly();

    private Invoice() { } // Needed for EF Core

    /// <summary>A draft for <paramref name="customerId"/>. It may start with no lines, but can't be issued until it has one.</summary>
    public static Invoice Create(CustomerId customerId, IEnumerable<InvoiceLine> lines)
    {
        var invoice = new Invoice
        {
            Id = InvoiceId.From(Guid.CreateVersion7()),
            CustomerId = customerId,
            Status = InvoiceStatus.Draft
        };

        invoice.SetLines(lines);

        return invoice;
    }

    public Result ReplaceLines(IEnumerable<InvoiceLine> lines)
    {
        if (Status != InvoiceStatus.Draft)
            return InvoiceErrors.NotDraft;

        SetLines(lines);

        return Success.Instance;
    }

    public Result Issue()
    {
        if (Status != InvoiceStatus.Draft)
            return InvoiceErrors.NotDraft;

        if (_lines.Count == 0)
            return InvoiceErrors.NoLines;

        Status = InvoiceStatus.Issued;
        AddDomainEvent(new InvoiceAmountDueChangedEvent(this));

        return Success.Instance;
    }

    public Result RecordPayment(decimal amount)
    {
        // Invalid input, not a business rule: the request is validated before it gets here.
        ThrowIfNegativeOrZero(amount);

        if (Status != InvoiceStatus.Issued)
            return InvoiceErrors.NotIssued;

        if (amount > AmountDue)
            return InvoiceErrors.Overpayment;

        _payments.Add(Payment.Create(amount));
        AmountPaid += amount;

        if (AmountDue == 0)
            Status = InvoiceStatus.Paid;

        AddDomainEvent(new InvoiceAmountDueChangedEvent(this));

        return Success.Instance;
    }

    private void SetLines(IEnumerable<InvoiceLine> lines)
    {
        _lines.Clear();
        _lines.AddRange(lines);
        Total = _lines.Sum(l => l.Quantity * l.UnitPrice);
    }
}
