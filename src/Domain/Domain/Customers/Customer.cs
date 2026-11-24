using MikeAllan.VerticalSliceArchitecture.Domain.Aggregates;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;

namespace MikeAllan.VerticalSliceArchitecture.Domain.Customers;

// Register new strongly typed IDs in ApplicationDbContext.ConfigureConventions
public readonly record struct CustomerId(Guid Value) : IGuidId<CustomerId>
{
    public static CustomerId From(Guid value) => new(value);
}

public sealed class Customer : AggregateRoot<CustomerId>
{
    public const int NameMaxLength = 100;
    public const int EmailMaxLength = 254;

    private readonly List<Invoice> _invoices = [];

    public string Name
    {
        get;
        set
        {
            ThrowIfNullOrWhiteSpace(value, nameof(Name));
            ThrowIfGreaterThan(value.Length, NameMaxLength, nameof(Name));
            field = value;
        }
    } = null!;

    // The domain guards only blank and length; the request checks the address format.
    public string Email
    {
        get;
        set
        {
            ThrowIfNullOrWhiteSpace(value, nameof(Email));
            ThrowIfGreaterThan(value.Length, EmailMaxLength, nameof(Email));
            field = value;
        }
    } = null!;

    /// <summary>The sum of <see cref="Invoice.AmountDue"/> over this customer's issued invoices.</summary>
    public decimal OutstandingBalance { get; private set; }

    public IReadOnlyList<Invoice> Invoices => _invoices.AsReadOnly();

    private Customer() { } // Needed for EF Core

    public static Customer Create(string name, string email)
    {
        var customer = new Customer { Id = CustomerId.From(Guid.CreateVersion7()), Name = name, Email = email };

        return customer;
    }

    // Drafts aren't owed yet and paid invoices owe nothing, so only issued invoices count.
    public void RecalculateOutstandingBalance()
    {
        OutstandingBalance = _invoices
            .Where(i => i.Status == InvoiceStatus.Issued)
            .Sum(i => i.AmountDue);
    }
}
