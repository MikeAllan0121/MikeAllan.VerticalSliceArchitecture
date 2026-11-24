namespace MikeAllan.VerticalSliceArchitecture.Domain.Invoices;

/// <summary>A value object: no identity, compared by its values, so a <c>sealed record</c>.</summary>
/// <remarks>
/// Two identical lines are two lines: an invoice keeps every line it is given, duplicates included.
/// </remarks>
public sealed record InvoiceLine
{
    public const int DescriptionMaxLength = 200;
    public const int QuantityMin = 1;
    public const int QuantityMax = 1000;
    public const decimal UnitPriceMin = 0.01m;
    public const decimal UnitPriceMax = 1_000_000m;
    public const int UnitPriceDecimalPlaces = 2;

    // Private setters needed for EF
    public string Description
    {
        get;
        private set
        {
            ThrowIfNullOrWhiteSpace(value, nameof(Description));
            ThrowIfGreaterThan(value.Length, DescriptionMaxLength, nameof(Description));
            field = value;
        }
    } = null!;

    // Private setters needed for EF
    public int Quantity
    {
        get;
        private set
        {
            ThrowIfLessThan(value, QuantityMin, nameof(Quantity));
            ThrowIfGreaterThan(value, QuantityMax, nameof(Quantity));
            field = value;
        }
    }

    // Private setters needed for EF
    public decimal UnitPrice
    {
        get;
        private set
        {
            ThrowIfLessThan(value, UnitPriceMin, nameof(UnitPrice));
            ThrowIfGreaterThan(value, UnitPriceMax, nameof(UnitPrice));

            // Mapped as numeric(18, 2), like the invoice's Total column it adds up to: a third decimal
            // place would be rounded away on save.
            if (decimal.Round(value, UnitPriceDecimalPlaces) != value)
                throw new ArgumentOutOfRangeException(
                    nameof(UnitPrice), value, $"Must have at most {UnitPriceDecimalPlaces} decimal places.");

            field = value;
        }
    }

    public InvoiceLine(string description, int quantity, decimal unitPrice)
    {
        Description = description;
        Quantity = quantity;
        UnitPrice = unitPrice;
    }
}
