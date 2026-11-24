using MikeAllan.VerticalSliceArchitecture.Domain.Aggregates;

namespace MikeAllan.VerticalSliceArchitecture.Domain.Invoices;

// Register new strongly typed IDs in ApplicationDbContext.ConfigureConventions
public readonly record struct PaymentId(Guid Value) : IGuidId<PaymentId>
{
    public static PaymentId From(Guid value) => new(value);
}

public sealed class Payment : Entity<PaymentId>
{
    public const int AmountDecimalPlaces = 2;

    public decimal Amount
    {
        get;
        private set
        {
            ThrowIfNegativeOrZero(value, nameof(Amount));

            // The column is numeric(18, 2): a third decimal place would be rounded away on save.
            if (decimal.Round(value, AmountDecimalPlaces) != value)
                throw new ArgumentOutOfRangeException(
                    nameof(Amount), value, $"Must have at most {AmountDecimalPlaces} decimal places.");

            field = value;
        }
    }

    private Payment() { } // Needed for EF Core

    // NOTE: Internal so that payments can only be created by the aggregate
    internal static Payment Create(decimal amount)
    {
        return new Payment
        {
            Id = PaymentId.From(Guid.CreateVersion7()),
            Amount = amount
        };
    }
}
