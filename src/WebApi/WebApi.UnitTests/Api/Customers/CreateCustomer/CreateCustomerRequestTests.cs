using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Internals.Api.Customers.CreateCustomer;

namespace MikeAllan.VerticalSliceArchitecture.UnitTests.Api.Customers.CreateCustomer;

public sealed class CreateCustomerRequestTests
{
    private const string EmailDomain = "@example.com";

    // The domain setters throw on over-length input, so anything validation lets through surfaces as a
    // 500 instead of a 400. These boundaries are the contract between the two.
    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(Customer.NameMaxLength, true)]
    [InlineData(Customer.NameMaxLength + 1, false)]
    public void Validate_WithNameOfLength_ShouldMatchDomainLimit(int length, bool expectedValid)
    {
        // Arrange
        CreateCustomerRequest request = CreateRequest(name: new string('a', length));

        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(request);

        // Assert
        if (expectedValid)
            Assert.Empty(invalidMembers);
        else
            Assert.Equal([nameof(CreateCustomerRequest.Name)], invalidMembers);
    }

    [Theory]
    [InlineData(Customer.EmailMaxLength, true)]
    [InlineData(Customer.EmailMaxLength + 1, false)]
    public void Validate_WithEmailOfLength_ShouldMatchDomainLimit(int length, bool expectedValid)
    {
        // Arrange — a well-formed address of exactly that length
        CreateCustomerRequest request = CreateRequest(email: new string('a', length - EmailDomain.Length) + EmailDomain);

        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(request);

        // Assert
        if (expectedValid)
            Assert.Empty(invalidMembers);
        else
            Assert.Equal([nameof(CreateCustomerRequest.Email)], invalidMembers);
    }

    // The domain guards only blank and length, so the format is the request's job alone.
    [Theory]
    [InlineData("")]
    [InlineData("not-an-email")]
    [InlineData("@example.com")]
    [InlineData("accounts@")]
    public void Validate_WithInvalidEmail_ShouldFail(string email)
    {
        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(CreateRequest(email: email));

        // Assert
        Assert.Equal([nameof(CreateCustomerRequest.Email)], invalidMembers);
    }

    [Fact]
    public void Validate_WithNullEmail_ShouldFail()
    {
        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(new CreateCustomerRequest("Contoso", null!));

        // Assert
        Assert.Equal([nameof(CreateCustomerRequest.Email)], invalidMembers);
    }

    [Fact]
    public void Validate_WithValidRequest_ShouldPass()
    {
        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(CreateRequest());

        // Assert
        Assert.Empty(invalidMembers);
    }

    private static CreateCustomerRequest CreateRequest(
        string name = "Contoso",
        string email = "accounts@contoso.com") =>
        new(name, email);
}
