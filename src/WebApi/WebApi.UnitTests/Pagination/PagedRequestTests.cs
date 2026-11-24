using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;
using MikeAllan.VerticalSliceArchitecture.Pagination;

namespace MikeAllan.VerticalSliceArchitecture.UnitTests.Pagination;

/// <summary>
/// <see cref="PagedRequest{TEntity}.Validate"/> is what keeps an unknown sort column or direction a 400:
/// the primitives behind it (<see cref="SortColumnMap{T}"/>, <see cref="SortDirections"/>) throw instead.
/// </summary>
public sealed class PagedRequestTests
{
    [Theory]
    [InlineData(null, null)]
    [InlineData("", "")]
    [InlineData("name", "asc")]
    [InlineData("SIZE", "Descending")]
    public void Validate_WithAllowedSortInputs_ShouldPass(string? sortBy, string? sortDirection)
    {
        // Act
        IReadOnlyList<ValidationResult> results =
            RequestValidation.Validate(new WidgetRequest { SortBy = sortBy, SortDirection = sortDirection });

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void Validate_WithUnknownSortColumn_ShouldNameTheMemberAndListTheAllowedColumns()
    {
        // Act
        IReadOnlyList<ValidationResult> results = RequestValidation.Validate(new WidgetRequest { SortBy = "password" });

        // Assert
        ValidationResult error = Assert.Single(results);
        Assert.Equal([nameof(WidgetRequest.SortBy)], error.MemberNames);
        Assert.Equal("'password' is not a sortable column. Allowed values: name, size.", error.ErrorMessage);
    }

    [Fact]
    public void Validate_WithUnknownSortDirection_ShouldNameTheMemberAndListTheAllowedDirections()
    {
        // Act
        IReadOnlyList<ValidationResult> results = RequestValidation.Validate(new WidgetRequest { SortDirection = "sideways" });

        // Assert
        ValidationResult error = Assert.Single(results);
        Assert.Equal([nameof(WidgetRequest.SortDirection)], error.MemberNames);
        Assert.StartsWith("'sideways' is not a valid sort direction. Allowed values: ", error.ErrorMessage, StringComparison.Ordinal);
        foreach (string allowed in SortDirections.Allowed)
            Assert.Contains(allowed, error.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_WithUnknownColumnAndDirection_ShouldReportBoth()
    {
        // Act
        IReadOnlyList<string> invalidMembers =
            RequestValidation.InvalidMembers(new WidgetRequest { SortBy = "password", SortDirection = "sideways" });

        // Assert
        Assert.Equal([nameof(WidgetRequest.SortBy), nameof(WidgetRequest.SortDirection)], invalidMembers);
    }

    // Paging is clamped, not rejected: a rule here would turn a clamped request into a 400.
    [Theory]
    [InlineData(0, 0)]
    [InlineData(-5, 5000)]
    [InlineData(int.MaxValue, int.MinValue)]
    public void Validate_WithOutOfRangePaging_ShouldPass(int page, int pageSize)
    {
        // Act
        IReadOnlyList<ValidationResult> results =
            RequestValidation.Validate(new WidgetRequest { Page = page, PageSize = pageSize });

        // Assert
        Assert.Empty(results);
    }

    [Fact]
    public void ToPaging_ShouldClampTheRawInput()
    {
        // Arrange
        WidgetRequest request = new() { Page = 0, PageSize = 5000 };

        // Act
        PagingParams paging = request.ToPaging();

        // Assert
        Assert.Equal(PagingParams.FirstPage, paging.Page);
        Assert.Equal(PagingParams.MaxPageSize, paging.PageSize);
    }

    [Theory]
    [InlineData(null, SortDirection.Ascending)]
    [InlineData("desc", SortDirection.Descending)]
    public void ToSortDirection_ShouldParseTheRawInput(string? sortDirection, SortDirection expected)
    {
        // Arrange
        WidgetRequest request = new() { SortDirection = sortDirection };

        // Act
        SortDirection direction = request.ToSortDirection();

        // Assert
        Assert.Equal(expected, direction);
    }

    private sealed record Widget(string Name, int Size);

    private sealed record WidgetRequest : PagedRequest<Widget>
    {
        private static readonly SortColumnMap<Widget> Columns = new(
            defaultColumn: "name",
            columns: new Dictionary<string, Expression<Func<Widget, object?>>>
            {
                ["name"] = w => w.Name,
                ["size"] = w => w.Size
            });

        protected override SortColumnMap<Widget> SortColumns => Columns;
    }
}
