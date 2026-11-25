using System.ComponentModel.DataAnnotations;
using MikeAllan.VerticalSliceArchitecture.Internals.Api.Customers.GetAllCustomers;

namespace MikeAllan.VerticalSliceArchitecture.UnitTests.Api.Customers.GetAllCustomers;

/// <remarks>
/// The rules themselves live in <c>PagedRequest&lt;TEntity&gt;</c> (see <c>Pagination/PagedRequestTests</c>);
/// these pin down that this slice plugs in the customer sort allow-list.
/// </remarks>
public sealed class GetAllCustomersRequestTests
{
    [Theory]
    [InlineData("name")]
    [InlineData("email")]
    [InlineData("outstandingBalance")]
    [InlineData("OUTSTANDINGBALANCE")]
    [InlineData(null)]
    public void Validate_WithAllowedSortColumn_ShouldPass(string? sortBy)
    {
        // Act
        IReadOnlyList<string> invalidMembers = RequestValidation.InvalidMembers(new GetAllCustomersRequest { SortBy = sortBy });

        // Assert
        Assert.Empty(invalidMembers);
    }

    [Fact]
    public void Validate_WithUnknownSortColumn_ShouldFailAndListTheAllowedColumns()
    {
        // Act
        IReadOnlyList<ValidationResult> results = RequestValidation.Validate(new GetAllCustomersRequest { SortBy = "createdAt" });

        // Assert
        ValidationResult error = Assert.Single(results);
        Assert.Equal([nameof(GetAllCustomersRequest.SortBy)], error.MemberNames);
        Assert.Contains("name", error.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("email", error.ErrorMessage, StringComparison.Ordinal);
        Assert.Contains("outstandingBalance", error.ErrorMessage, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("DESC")]
    [InlineData("ascending")]
    [InlineData(null)]
    public void Validate_WithAllowedSortDirection_ShouldPass(string? sortDirection)
    {
        // Act
        IReadOnlyList<string> invalidMembers =
            RequestValidation.InvalidMembers(new GetAllCustomersRequest { SortDirection = sortDirection });

        // Assert
        Assert.Empty(invalidMembers);
    }

    [Fact]
    public void Validate_WithUnknownSortDirection_ShouldFail()
    {
        // Act
        IReadOnlyList<string> invalidMembers =
            RequestValidation.InvalidMembers(new GetAllCustomersRequest { SortDirection = "sideways" });

        // Assert
        Assert.Equal([nameof(GetAllCustomersRequest.SortDirection)], invalidMembers);
    }

    // Out-of-range paging is clamped by PagingParams rather than rejected, so validation must let it
    // through — a rule added here would turn a clamped request into a 400.
    [Theory]
    [InlineData(0, 0)]
    [InlineData(-5, 5000)]
    public void Validate_WithOutOfRangePaging_ShouldPass(int page, int pageSize)
    {
        // Act
        IReadOnlyList<string> invalidMembers =
            RequestValidation.InvalidMembers(new GetAllCustomersRequest { Page = page, PageSize = pageSize });

        // Assert
        Assert.Empty(invalidMembers);
    }
}
