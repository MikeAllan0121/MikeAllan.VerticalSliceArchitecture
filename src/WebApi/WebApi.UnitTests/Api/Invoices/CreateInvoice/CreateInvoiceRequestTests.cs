using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Api.Invoices.CreateInvoice;

namespace MikeAllan.VerticalSliceArchitecture.UnitTests.Api.Invoices.CreateInvoice;

public sealed class CreateInvoiceRequestTests
{
    // The InvoiceLine constructor throws on anything out of range, so anything validation lets through
    // surfaces as a 500 instead of a 400. These boundaries are the contract between the two.
    public static TheoryData<decimal, bool> UnitPrices => new()
    {
        { 0m, false },
        { InvoiceLine.UnitPriceMin, true },
        { InvoiceLine.UnitPriceMax, true },
        { InvoiceLine.UnitPriceMax + 0.01m, false },
        { 10.55m, true },
        { 10.555m, false }
    };

    [Theory]
    [InlineData(0, false)]
    [InlineData(1, true)]
    [InlineData(InvoiceLine.DescriptionMaxLength, true)]
    [InlineData(InvoiceLine.DescriptionMaxLength + 1, false)]
    public void Validate_WithLineDescriptionOfLength_ShouldMatchDomainLimit(int length, bool expectedValid)
    {
        // Arrange
        CreateInvoiceRequest.InvoiceLineDto line = new(new string('a', length), 1, 100m);

        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(line);

        // Assert
        if (expectedValid)
            Assert.Empty(invalidMembers);
        else
            Assert.Equal([nameof(CreateInvoiceRequest.InvoiceLineDto.Description)], invalidMembers);
    }

    [Theory]
    [InlineData(InvoiceLine.QuantityMin - 1, false)]
    [InlineData(InvoiceLine.QuantityMin, true)]
    [InlineData(InvoiceLine.QuantityMax, true)]
    [InlineData(InvoiceLine.QuantityMax + 1, false)]
    public void Validate_WithLineQuantity_ShouldMatchDomainRange(int quantity, bool expectedValid)
    {
        // Arrange
        CreateInvoiceRequest.InvoiceLineDto line = new("Consulting", quantity, 100m);

        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(line);

        // Assert
        if (expectedValid)
            Assert.Empty(invalidMembers);
        else
            Assert.Equal([nameof(CreateInvoiceRequest.InvoiceLineDto.Quantity)], invalidMembers);
    }

    [Theory]
    [MemberData(nameof(UnitPrices))]
    public void Validate_WithLineUnitPrice_ShouldMatchDomainRules(decimal unitPrice, bool expectedValid)
    {
        // Arrange
        CreateInvoiceRequest.InvoiceLineDto line = new("Consulting", 1, unitPrice);

        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(line);

        // Assert
        if (expectedValid)
            Assert.Empty(invalidMembers);
        else
            Assert.Equal([nameof(CreateInvoiceRequest.InvoiceLineDto.UnitPrice)], invalidMembers);
    }

    // CustomerId is a body property, not a route parameter, so the validator does see its [NotEmptyGuid].
    [Fact]
    public void Validate_WithEmptyCustomerId_ShouldFail()
    {
        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(CreateRequest(customerId: Guid.Empty));

        // Assert
        Assert.Equal([nameof(CreateInvoiceRequest.CustomerId)], invalidMembers);
    }

    [Fact]
    public void Validate_WithNullLines_ShouldFail()
    {
        // Arrange — a body that omits "lines" deserializes to null, and the endpoint enumerates it
        CreateInvoiceRequest request = new(Guid.CreateVersion7(), null!);

        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(request);

        // Assert
        Assert.Equal([nameof(CreateInvoiceRequest.Lines)], invalidMembers);
    }

    // A draft may start empty; it just can't be issued until it has a line.
    [Fact]
    public void Validate_WithNoLines_ShouldPass()
    {
        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(new CreateInvoiceRequest(Guid.CreateVersion7(), []));

        // Assert
        Assert.Empty(invalidMembers);
    }

    [Fact]
    public void Validate_WithValidRequest_ShouldPass()
    {
        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(CreateRequest());

        // Assert
        Assert.Empty(invalidMembers);
    }

    private static CreateInvoiceRequest CreateRequest(Guid? customerId = null) =>
        new(customerId ?? Guid.CreateVersion7(), [new CreateInvoiceRequest.InvoiceLineDto("Consulting", 2, 150m)]);
}
