using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;

namespace MikeAllan.VerticalSliceArchitecture.Domain.UnitTests.Invoices;

/// <remarks>
/// <see cref="Payment"/> is created only by <see cref="Invoice.RecordPayment"/>; its behaviour through the
/// aggregate is covered in <see cref="InvoiceTests"/>.
/// </remarks>
public sealed class PaymentTests
{
    [Theory]
    [InlineData("a3ac0d5a-2555-46fa-be04-75a57476e9dd", "d405d1b3-cd2e-428d-9c64-fd06dbb4f9d7", false)]
    [InlineData("a3ac0d5a-2555-46fa-be04-75a57476e9dd", "a3ac0d5a-2555-46fa-be04-75a57476e9dd", true)]
    public void PaymentId_ShouldBeComparable(string stringGuid1, string stringGuid2, bool isEqual)
    {
        // Arrange
        Guid guid1 = Guid.Parse(stringGuid1);
        Guid guid2 = Guid.Parse(stringGuid2);
        PaymentId id1 = PaymentId.From(guid1);
        PaymentId id2 = PaymentId.From(guid2);

        // Act
        bool areEqual = id1 == id2;

        // Assert
        Assert.Equal(isEqual, areEqual);
        Assert.Equal(guid1, id1.Value);
        Assert.Equal(guid2, id2.Value);
    }
}
