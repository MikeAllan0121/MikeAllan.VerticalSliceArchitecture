using MikeAllan.VerticalSliceArchitecture.Internals.Api.Invoices.GetAllInvoices;

namespace MikeAllan.VerticalSliceArchitecture.UnitTests.Api.Invoices.GetAllInvoices;

/// <remarks>
/// The rules themselves live in <c>PagedRequest&lt;TEntity&gt;</c> (see <c>Pagination/PagedRequestTests</c>);
/// these pin down that this slice plugs in the invoice sort allow-list.
/// </remarks>
public sealed class GetAllInvoicesRequestTests
{
    [Theory]
    [InlineData("createdAt")]
    [InlineData("total")]
    [InlineData("amountDue")]
    [InlineData(null)]
    public void Validate_WithAllowedSortColumn_ShouldPass(string? sortBy)
    {
        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(new GetAllInvoicesRequest { SortBy = sortBy });

        // Assert
        Assert.Empty(invalidMembers);
    }

    [Theory]
    [InlineData("name")]
    [InlineData("outstandingBalance")]
    public void Validate_WithACustomerOnlySortColumn_ShouldFail(string sortBy)
    {
        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(new GetAllInvoicesRequest { SortBy = sortBy });

        // Assert
        Assert.Equal([nameof(GetAllInvoicesRequest.SortBy)], invalidMembers);
    }
}
