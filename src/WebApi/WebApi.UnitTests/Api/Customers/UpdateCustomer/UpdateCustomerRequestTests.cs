using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Api.Customers.UpdateCustomer;

namespace MikeAllan.VerticalSliceArchitecture.UnitTests.Api.Customers.UpdateCustomer;

/// <remarks>
/// The customer's ID is a route parameter guarded by <c>[NotEmptyGuid]</c>, which
/// <see cref="System.ComponentModel.DataAnnotations.Validator"/> can't reach: see
/// <c>Validation/NotEmptyGuidAttributeTests</c> and the integration tests.
/// </remarks>
public sealed class UpdateCustomerRequestTests
{
    private const string EmailDomain = "@example.com";

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(Customer.NameMaxLength, true)]
    [InlineData(Customer.NameMaxLength + 1, false)]
    public void Validate_WithNameOfLength_ShouldMatchDomainLimit(int length, bool expectedValid)
    {
        // Arrange
        UpdateCustomerRequest request = CreateRequest(name: new string('a', length));

        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(request);

        // Assert
        if (expectedValid)
            Assert.Empty(invalidMembers);
        else
            Assert.Equal([nameof(UpdateCustomerRequest.Name)], invalidMembers);
    }

    [Theory]
    [InlineData(Customer.EmailMaxLength, true)]
    [InlineData(Customer.EmailMaxLength + 1, false)]
    public void Validate_WithEmailOfLength_ShouldMatchDomainLimit(int length, bool expectedValid)
    {
        // Arrange — a well-formed address of exactly that length
        UpdateCustomerRequest request = CreateRequest(email: new string('a', length - EmailDomain.Length) + EmailDomain);

        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(request);

        // Assert
        if (expectedValid)
            Assert.Empty(invalidMembers);
        else
            Assert.Equal([nameof(UpdateCustomerRequest.Email)], invalidMembers);
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validate_WithInvalidEmail_ShouldFail(string email)
    {
        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(CreateRequest(email: email));

        // Assert
        Assert.Equal([nameof(UpdateCustomerRequest.Email)], invalidMembers);
    }

    [Fact]
    public void Validate_WithValidRequest_ShouldPass()
    {
        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(CreateRequest());

        // Assert
        Assert.Empty(invalidMembers);
    }

    private static UpdateCustomerRequest CreateRequest(
        string name = "Contoso",
        string email = "accounts@contoso.com") =>
        new(name, email);
}
