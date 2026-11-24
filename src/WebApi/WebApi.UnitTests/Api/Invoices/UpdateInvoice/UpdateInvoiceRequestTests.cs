using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Api.Invoices.UpdateInvoice;

namespace MikeAllan.VerticalSliceArchitecture.UnitTests.Api.Invoices.UpdateInvoice;

/// <remarks>
/// The invoice's ID is a route parameter guarded by <c>[NotEmptyGuid]</c>, which
/// <see cref="System.ComponentModel.DataAnnotations.Validator"/> can't reach: see
/// <c>Validation/NotEmptyGuidAttributeTests</c> and the integration tests.
/// </remarks>
public sealed class UpdateInvoiceRequestTests
{
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
        UpdateInvoiceRequest.InvoiceLineDto line = new(new string('a', length), 1, 100m);

        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(line);

        // Assert
        if (expectedValid)
            Assert.Empty(invalidMembers);
        else
            Assert.Equal([nameof(UpdateInvoiceRequest.InvoiceLineDto.Description)], invalidMembers);
    }

    [Theory]
    [InlineData(InvoiceLine.QuantityMin - 1, false)]
    [InlineData(InvoiceLine.QuantityMin, true)]
    [InlineData(InvoiceLine.QuantityMax, true)]
    [InlineData(InvoiceLine.QuantityMax + 1, false)]
    public void Validate_WithLineQuantity_ShouldMatchDomainRange(int quantity, bool expectedValid)
    {
        // Arrange
        UpdateInvoiceRequest.InvoiceLineDto line = new("Consulting", quantity, 100m);

        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(line);

        // Assert
        if (expectedValid)
            Assert.Empty(invalidMembers);
        else
            Assert.Equal([nameof(UpdateInvoiceRequest.InvoiceLineDto.Quantity)], invalidMembers);
    }

    [Theory]
    [MemberData(nameof(UnitPrices))]
    public void Validate_WithLineUnitPrice_ShouldMatchDomainRules(decimal unitPrice, bool expectedValid)
    {
        // Arrange
        UpdateInvoiceRequest.InvoiceLineDto line = new("Consulting", 1, unitPrice);

        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(line);

        // Assert
        if (expectedValid)
            Assert.Empty(invalidMembers);
        else
            Assert.Equal([nameof(UpdateInvoiceRequest.InvoiceLineDto.UnitPrice)], invalidMembers);
    }

    [Fact]
    public void Validate_WithNullLines_ShouldFail()
    {
        // Arrange — a body that omits "lines" deserializes to null, and the endpoint enumerates it
        UpdateInvoiceRequest request = new(null!);

        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(request);

        // Assert
        Assert.Equal([nameof(UpdateInvoiceRequest.Lines)], invalidMembers);
    }

    [Fact]
    public void Validate_WithValidRequest_ShouldPass()
    {
        // Act
        IReadOnlyList<string> invalidMembers =
            RequestValidation.InvalidMembers(new UpdateInvoiceRequest([new UpdateInvoiceRequest.InvoiceLineDto("Consulting", 2, 150m)]));

        // Assert
        Assert.Empty(invalidMembers);
    }
}
