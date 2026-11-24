using System.ComponentModel.DataAnnotations;
using MikeAllan.VerticalSliceArchitecture.Validation;

namespace MikeAllan.VerticalSliceArchitecture.UnitTests.Validation;

/// <summary>
/// Route GUIDs (<c>{customerId}</c>, <c>{invoiceId}</c>) carry <see cref="NotEmptyGuidAttribute"/>, which
/// <see cref="Validator.TryValidateObject(object, ValidationContext, ICollection{ValidationResult}?, bool)"/>
/// never reaches, so the attribute is tested on its own here and over HTTP in the integration tests.
/// </summary>
public sealed class NotEmptyGuidAttributeTests
{
    private readonly NotEmptyGuidAttribute _attribute = new();

    [Fact]
    public void IsValid_WithEmptyGuid_ShouldFail()
    {
        Assert.False(_attribute.IsValid(Guid.Empty));
    }

    [Fact]
    public void IsValid_WithNonEmptyGuid_ShouldPass()
    {
        Assert.True(_attribute.IsValid(Guid.CreateVersion7()));
    }

    // Anything that isn't a Guid is another attribute's job (Required for null, binding for the type).
    [Fact]
    public void IsValid_WithNull_ShouldPass()
    {
        Assert.True(_attribute.IsValid(null));
    }

    [Fact]
    public void GetValidationResult_WithEmptyGuid_ShouldNameTheMember()
    {
        // Arrange
        ValidationContext context = new(new object()) { DisplayName = "customerId", MemberName = "customerId" };

        // Act
        ValidationResult? result = _attribute.GetValidationResult(Guid.Empty, context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("The customerId field must not be an empty GUID.", result.ErrorMessage);
        Assert.Equal(["customerId"], result.MemberNames);
    }
}
