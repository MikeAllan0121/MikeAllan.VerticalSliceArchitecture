using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;

namespace MikeAllan.VerticalSliceArchitecture.Domain.UnitTests.Pagination;

public sealed class PagedListTests
{
    [Theory]
    [InlineData(0, 10, 0)]
    [InlineData(10, 10, 1)]
    [InlineData(11, 10, 2)]
    [InlineData(25, 10, 3)]
    public void TotalPages_ShouldRoundUp(int totalCount, int pageSize, int expected)
    {
        // Act
        PagedList<string> page = new([], 1, pageSize, totalCount);

        // Assert
        Assert.Equal(expected, page.TotalPages);
    }

    [Fact]
    public void TotalPages_ShouldBeZero_WhenPageSizeIsZero()
    {
        // A page size of zero can't come from an endpoint (PagingParams clamps it), but the envelope is
        // public. The division is in double, so it yields Infinity rather than throwing, and
        // (int)Math.Ceiling(Infinity) is int.MinValue — the guard is what stops a negative totalPages
        // reaching a client.
        PagedList<string> page = new([], 1, 0, 10);

        Assert.Equal(0, page.TotalPages);
    }

    [Theory]
    [InlineData(1, 25, false, true)]
    [InlineData(2, 25, true, true)]
    [InlineData(3, 25, true, false)]
    [InlineData(1, 0, false, false)]
    public void HasPreviousAndNextPage_ShouldReflectPosition(
        int page,
        int totalCount,
        bool expectedHasPrevious,
        bool expectedHasNext)
    {
        // Act
        PagedList<string> pagedList = new([], page, 10, totalCount);

        // Assert
        Assert.Equal(expectedHasPrevious, pagedList.HasPreviousPage);
        Assert.Equal(expectedHasNext, pagedList.HasNextPage);
    }
}
