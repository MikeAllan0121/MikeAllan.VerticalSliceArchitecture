using MikeAllan.VerticalSliceArchitecture.Internals.Api.Invoices.RecordPayment;

namespace MikeAllan.VerticalSliceArchitecture.UnitTests.Api.Invoices.RecordPayment;

/// <remarks>
/// The invoice's ID is a route parameter guarded by <c>[NotEmptyGuid]</c>: see
/// <c>Validation/NotEmptyGuidAttributeTests</c> and the integration tests. A payment above the amount due is
/// the domain's call (a 409), so no upper limit below the column's is tested here.
/// </remarks>
public sealed class RecordPaymentRequestTests
{
    // The domain throws on a non-positive amount or a third decimal place, so anything validation lets
    // through surfaces as a 500 instead of a 400.
    public static TheoryData<decimal, bool> Amounts => new()
    {
        { -1m, false },
        { 0m, false },
        { 0.01m, true },
        { 10.5m, true },
        { 10.555m, false },
        { 9_999_999_999_999_999.99m, true }
    };

    [Theory]
    [MemberData(nameof(Amounts))]
    public void Validate_WithAmount_ShouldMatchDomainRules(decimal amount, bool expectedValid)
    {
        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(new RecordPaymentRequest(amount));

        // Assert
        if (expectedValid)
            Assert.Empty(invalidMembers);
        else
            Assert.Equal([nameof(RecordPaymentRequest.Amount)], invalidMembers);
    }
}
