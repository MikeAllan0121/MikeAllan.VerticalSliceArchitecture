using System.ComponentModel.DataAnnotations;
using MikeAllan.VerticalSliceArchitecture.Internals.Validation;

namespace MikeAllan.VerticalSliceArchitecture.UnitTests.Validation;

public sealed class MaxDecimalPlacesAttributeTests
{
    private readonly MaxDecimalPlacesAttribute _attribute = new(2);

    public static TheoryData<decimal, bool> Values => new()
    {
        { 10m, true },
        { 10.5m, true },
        { 10.55m, true },
        { 10.550m, true }, // trailing zeros don't count
        { 10.555m, false },
        { -10.555m, false }
    };

    [Theory]
    [MemberData(nameof(Values))]
    public void IsValid_ShouldAllowAtMostTheGivenDecimalPlaces(decimal value, bool expectedValid)
    {
        Assert.Equal(expectedValid, _attribute.IsValid(value));
    }

    // Anything that isn't a decimal is another attribute's job (Required for null, binding for the type).
    [Fact]
    public void IsValid_WithNull_ShouldPass()
    {
        Assert.True(_attribute.IsValid(null));
    }

    [Fact]
    public void GetValidationResult_WithTooManyDecimalPlaces_ShouldNameTheMemberAndTheLimit()
    {
        // Arrange
        ValidationContext context = new(new object()) { DisplayName = "UnitPrice", MemberName = "UnitPrice" };

        // Act
        ValidationResult? result = _attribute.GetValidationResult(10.555m, context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("The UnitPrice field must have at most 2 decimal places.", result.ErrorMessage);
        Assert.Equal(["UnitPrice"], result.MemberNames);
    }
}
