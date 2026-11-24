using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;

namespace MikeAllan.VerticalSliceArchitecture.Domain.UnitTests.Pagination;

public sealed class PagingParamsTests
{
    [Theory]
    [InlineData(1, 1)]
    [InlineData(5, 5)]
    [InlineData(0, PagingParams.FirstPage)]
    [InlineData(-3, PagingParams.FirstPage)]
    public void From_ShouldClampPageToFirstPage_WhenBelowRange(int requested, int expected)
    {
        // Act
        PagingParams paging = PagingParams.From(requested, PagingParams.DefaultPageSize);

        // Assert
        Assert.Equal(expected, paging.Page);
    }

    [Theory]
    [InlineData(10, 10)]
    [InlineData(PagingParams.MaxPageSize, PagingParams.MaxPageSize)]
    [InlineData(PagingParams.MaxPageSize + 1, PagingParams.MaxPageSize)]
    [InlineData(5000, PagingParams.MaxPageSize)]
    [InlineData(0, PagingParams.MinPageSize)]
    [InlineData(-1, PagingParams.MinPageSize)]
    public void From_ShouldClampPageSizeIntoRange(int requested, int expected)
    {
        // Act
        PagingParams paging = PagingParams.From(PagingParams.FirstPage, requested);

        // Assert
        Assert.Equal(expected, paging.PageSize);
    }

    [Theory]
    [InlineData(1, 10, 0)]
    [InlineData(2, 10, 10)]
    [InlineData(3, 25, 50)]
    public void Skip_ShouldBeRowsBeforeThePage(int page, int pageSize, int expectedSkip)
    {
        // Act
        PagingParams paging = PagingParams.From(page, pageSize);

        // Assert
        Assert.Equal(expectedSkip, paging.Skip);
    }

    [Fact]
    public void From_ShouldApplyDefaults_WhenNothingIsSpecified()
    {
        // Act
        PagingParams paging = PagingParams.From(null, null);

        // Assert
        Assert.Equal(PagingParams.FirstPage, paging.Page);
        Assert.Equal(PagingParams.DefaultPageSize, paging.PageSize);
    }

    [Fact]
    public void Skip_ShouldNeverBeNegative_WhenPageIsOutOfRange()
    {
        // Act
        PagingParams paging = PagingParams.From(-10, PagingParams.DefaultPageSize);

        // Assert
        Assert.Equal(0, paging.Skip);
    }

    // (Page - 1) * PageSize overflows int long before Page does, and a wrapped offset reaches PostgreSQL
    // as a negative or arbitrary positive OFFSET.
    [Theory]
    [InlineData(int.MaxValue, PagingParams.MaxPageSize)]
    [InlineData(100_000_000, PagingParams.MaxPageSize)]
    [InlineData(int.MaxValue, PagingParams.MinPageSize)]
    public void Skip_ShouldSaturate_RatherThanOverflow(int page, int pageSize)
    {
        // Act
        PagingParams paging = PagingParams.From(page, pageSize);

        // Assert
        Assert.True(paging.Skip >= 0, $"Skip was {paging.Skip}");
        Assert.Equal((int)Math.Min((long)(page - 1) * pageSize, int.MaxValue), paging.Skip);
    }
}
