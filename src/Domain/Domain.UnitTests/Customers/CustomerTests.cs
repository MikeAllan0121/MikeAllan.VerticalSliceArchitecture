using MikeAllan.VerticalSliceArchitecture.Domain.Customers;

namespace MikeAllan.VerticalSliceArchitecture.Domain.UnitTests.Customers;

/// <remarks>
/// <see cref="Customer.RecalculateOutstandingBalance"/> reads the customer's invoices, which only EF Core
/// loads; it is covered by the integration tests of the event handler that calls it.
/// </remarks>
public sealed class CustomerTests
{
    [Theory]
    [InlineData("a3ac0d5a-2555-46fa-be04-75a57476e9dd", "d405d1b3-cd2e-428d-9c64-fd06dbb4f9d7", false)]
    [InlineData("a3ac0d5a-2555-46fa-be04-75a57476e9dd", "a3ac0d5a-2555-46fa-be04-75a57476e9dd", true)]
    public void CustomerId_ShouldBeComparable(string stringGuid1, string stringGuid2, bool isEqual)
    {
        // Arrange
        Guid guid1 = Guid.Parse(stringGuid1);
        Guid guid2 = Guid.Parse(stringGuid2);
        CustomerId id1 = CustomerId.From(guid1);
        CustomerId id2 = CustomerId.From(guid2);

        // Act
        bool areEqual = id1 == id2;

        // Assert
        Assert.Equal(isEqual, areEqual);
        Assert.Equal(guid1, id1.Value);
        Assert.Equal(guid2, id2.Value);
    }

    [Fact]
    public void Create_WithValidNameAndEmail_ShouldSucceed()
    {
        // Arrange
        const string name = "Contoso";
        const string email = "accounts@contoso.com";

        // Act
        Customer customer = Customer.Create(name, email);

        // Assert
        Assert.Equal(name, customer.Name);
        Assert.Equal(email, customer.Email);
        Assert.Equal(0m, customer.OutstandingBalance);
        Assert.Empty(customer.Invoices);
    }

    [Theory]
    [InlineData(null, "accounts@contoso.com")]
    [InlineData("Contoso", null)]
    [InlineData(null, null)]
    public void Create_WithNullNameOrEmail_ShouldThrow(string? name, string? email)
    {
        // Act
        ArgumentException exception = Assert.ThrowsAny<ArgumentException>(() => Customer.Create(name!, email!));

        // Assert
        Assert.StartsWith("Value cannot be null", exception.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(Customer.NameMaxLength, false)]
    [InlineData(Customer.NameMaxLength + 1, true)]
    public void Name_OfLength_ShouldEnforceDomainLimit(int length, bool shouldThrow)
    {
        // Arrange
        Customer customer = Customer.Create("Contoso", "accounts@contoso.com");

        // Act
        Exception? exception = Record.Exception(() => customer.Name = new string('a', length));

        // Assert
        if (shouldThrow)
            Assert.IsType<ArgumentOutOfRangeException>(exception);
        else
            Assert.Null(exception);
    }

    [Theory]
    [InlineData(Customer.EmailMaxLength, false)]
    [InlineData(Customer.EmailMaxLength + 1, true)]
    public void Email_OfLength_ShouldEnforceDomainLimit(int length, bool shouldThrow)
    {
        // Arrange
        Customer customer = Customer.Create("Contoso", "accounts@contoso.com");

        // Act
        Exception? exception = Record.Exception(() => customer.Email = new string('a', length));

        // Assert
        if (shouldThrow)
            Assert.IsType<ArgumentOutOfRangeException>(exception);
        else
            Assert.Null(exception);
    }

    [Fact]
    public void RecalculateOutstandingBalance_WithNoInvoices_ShouldBeZero()
    {
        // Arrange
        Customer customer = Customer.Create("Contoso", "accounts@contoso.com");

        // Act
        customer.RecalculateOutstandingBalance();

        // Assert
        Assert.Equal(0m, customer.OutstandingBalance);
    }
}
