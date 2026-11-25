using System.ComponentModel.DataAnnotations;

namespace MikeAllan.VerticalSliceArchitecture.Internals.Validation;

/// <summary>
/// Rejects a <see cref="decimal"/> with more than <see cref="DecimalPlaces"/> significant decimal places,
/// e.g. <c>10.005</c> for a money amount. Trailing zeros don't count: <c>10.50</c> and <c>10.500</c> pass.
/// </summary>
/// <remarks>
/// <see cref="RangeAttribute"/> can't express this, and the domain guards it with a throw, so without it
/// the third decimal place is a 500 rather than a 400.
/// </remarks>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
internal sealed class MaxDecimalPlacesAttribute : ValidationAttribute
{
    public MaxDecimalPlacesAttribute(int decimalPlaces)
        : base("The {0} field must have at most {1} decimal places.")
    {
        ArgumentOutOfRangeException.ThrowIfNegative(decimalPlaces);
        DecimalPlaces = decimalPlaces;
    }

    public int DecimalPlaces { get; }

    public override string FormatErrorMessage(string name) =>
        string.Format(System.Globalization.CultureInfo.CurrentCulture, ErrorMessageString, name, DecimalPlaces);

    // Anything that isn't a decimal is another attribute's job (Required for null, binding for the type).
    public override bool IsValid(object? value) =>
        value is not decimal number || decimal.Round(number, DecimalPlaces) == number;
}
