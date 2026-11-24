using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;

namespace MikeAllan.VerticalSliceArchitecture.Domain.UnitTests.Pagination;

public sealed class SortDirectionsTests
{
    [Theory]
    [InlineData("asc", SortDirection.Ascending)]
    [InlineData("ASC", SortDirection.Ascending)]
    [InlineData("ascending", SortDirection.Ascending)]
    [InlineData("desc", SortDirection.Descending)]
    [InlineData("Descending", SortDirection.Descending)]
    public void From_ShouldParseKnownDirections_CaseInsensitively(string value, SortDirection expected)
    {
        Assert.Equal(expected, SortDirections.From(value));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void From_ShouldDefaultToAscending_WhenUnspecified(string? value)
    {
        Assert.Equal(SortDirection.Ascending, SortDirections.From(value));
    }

    [Fact]
    public void From_ShouldThrow_WhenDirectionIsUnknown()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => SortDirections.From("sideways"));
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("desc", true)]
    [InlineData("DESC", true)]
    [InlineData("sideways", false)]
    public void IsAllowed_ShouldMatchWhatFromAccepts(string? value, bool expected)
    {
        Assert.Equal(expected, SortDirections.IsAllowed(value));
    }
}
