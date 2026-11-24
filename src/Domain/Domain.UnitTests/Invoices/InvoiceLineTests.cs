using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;

namespace MikeAllan.VerticalSliceArchitecture.Domain.UnitTests.Invoices;

public sealed class InvoiceLineTests
{
    [Fact]
    public void InvoiceLine_ShouldBeCreatable()
    {
        // Arrange
        const string description = "Consulting";
        const int quantity = 3;
        const decimal unitPrice = 150.25m;

        // Act
        InvoiceLine line = new(description, quantity, unitPrice);

        // Assert
        Assert.Equal(description, line.Description);
        Assert.Equal(quantity, line.Quantity);
        Assert.Equal(unitPrice, line.UnitPrice);
    }

    [Fact]
    public void InvoiceLine_ShouldBeComparable()
    {
        // Arrange
        InvoiceLine line1 = new("Consulting", 3, 150m);
        InvoiceLine line2 = new("Consulting", 3, 150m);

        // Act
        bool areEqual = line1 == line2;

        // Assert
        Assert.True(areEqual);
    }

    [Theory]
    [InlineData(-1, true)]
    [InlineData(InvoiceLine.QuantityMin - 1, true)]
    [InlineData(InvoiceLine.QuantityMin, false)]
    [InlineData(InvoiceLine.QuantityMax, false)]
    [InlineData(InvoiceLine.QuantityMax + 1, true)]
    public void InvoiceLine_WithQuantity_ShouldEnforceRange(int quantity, bool shouldThrow)
    {
        // Act
        Exception? exception = Record.Exception(() => new InvoiceLine("Consulting", quantity, 10m));

        // Assert
        if (shouldThrow)
            Assert.IsType<ArgumentOutOfRangeException>(exception);
        else
            Assert.Null(exception);
    }

    [Theory]
    [InlineData("-1", true)]
    [InlineData("0", true)]
    [InlineData("0.01", false)]
    [InlineData("1000000", false)]
    [InlineData("1000000.01", true)]
    public void InvoiceLine_WithUnitPrice_ShouldEnforceRange(string unitPriceText, bool shouldThrow)
    {
        // Arrange — decimals can't be attribute arguments, so they arrive as invariant-culture text
        decimal unitPrice = decimal.Parse(unitPriceText, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        Exception? exception = Record.Exception(() => new InvoiceLine("Consulting", 1, unitPrice));

        // Assert
        if (shouldThrow)
            Assert.IsType<ArgumentOutOfRangeException>(exception);
        else
            Assert.Null(exception);
    }

    [Fact]
    public void InvoiceLine_WithUnitPriceAtTheDomainLimits_ShouldSucceed()
    {
        // Act
        Exception? atMin = Record.Exception(() => new InvoiceLine("Consulting", 1, InvoiceLine.UnitPriceMin));
        Exception? atMax = Record.Exception(() => new InvoiceLine("Consulting", 1, InvoiceLine.UnitPriceMax));

        // Assert
        Assert.Null(atMin);
        Assert.Null(atMax);
    }

    [Theory]
    [InlineData("10.5", false)]
    [InlineData("10.55", false)]
    [InlineData("10.550", false)]
    [InlineData("10.555", true)]
    public void InvoiceLine_WithUnitPriceDecimalPlaces_ShouldAllowAtMostTwo(string unitPriceText, bool shouldThrow)
    {
        // Arrange
        decimal unitPrice = decimal.Parse(unitPriceText, System.Globalization.CultureInfo.InvariantCulture);

        // Act
        Exception? exception = Record.Exception(() => new InvoiceLine("Consulting", 1, unitPrice));

        // Assert
        if (shouldThrow)
            Assert.IsType<ArgumentOutOfRangeException>(exception);
        else
            Assert.Null(exception);
    }

    [Theory]
    [InlineData(1, false)]
    [InlineData(InvoiceLine.DescriptionMaxLength, false)]
    [InlineData(InvoiceLine.DescriptionMaxLength + 1, true)]
    public void InvoiceLine_WithDescriptionOfLength_ShouldEnforceDomainLimit(int length, bool shouldThrow)
    {
        // Act
        Exception? exception = Record.Exception(() => new InvoiceLine(new string('a', length), 1, 10m));

        // Assert
        if (shouldThrow)
            Assert.IsType<ArgumentOutOfRangeException>(exception);
        else
            Assert.Null(exception);
    }
}
