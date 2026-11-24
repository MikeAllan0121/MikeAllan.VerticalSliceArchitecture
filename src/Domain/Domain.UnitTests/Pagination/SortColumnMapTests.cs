using System.Linq.Expressions;
using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;

namespace MikeAllan.VerticalSliceArchitecture.Domain.UnitTests.Pagination;

public sealed class SortColumnMapTests
{
    // Size ties between Ids 1 and 3, and name ties between Ids 2 and 3, so every ordering test also
    // exercises the tie-breaker.
    private static readonly Widget[] Widgets = [new(1, "b", 2), new(2, "a", 1), new(3, "a", 2)];

    private static SortColumnMap<Widget> Map() => new(
        defaultColumn: "name",
        columns: new Dictionary<string, Expression<Func<Widget, object?>>>
        {
            ["name"] = w => w.Name,
            ["size"] = w => w.Size
        });

    [Theory]
    [InlineData(null, true)]
    [InlineData("", true)]
    [InlineData("name", true)]
    [InlineData("NAME", true)]
    [InlineData("size", true)]
    [InlineData("password", false)]
    public void IsAllowed_ShouldOnlyAcceptMappedColumns(string? sortBy, bool expected)
    {
        Assert.Equal(expected, Map().IsAllowed(sortBy));
    }

    [Fact]
    public void AllowedColumns_ShouldListEveryMappedColumn()
    {
        string[] expected = ["name", "size"];

        Assert.Equivalent(expected, Map().AllowedColumns, strict: true);
    }

    [Fact]
    public void Ctor_ShouldThrow_WhenDefaultColumnIsNotMapped()
    {
        Assert.Throws<ArgumentException>(() => new SortColumnMap<Widget>(
            defaultColumn: "missing",
            columns: new Dictionary<string, Expression<Func<Widget, object?>>>
            {
                ["name"] = w => w.Name
            }));
    }

    [Fact]
    public void Apply_ShouldOrderByColumnThenTieBreaker()
    {
        // Act
        int[] ids = [.. Map().Apply(Widgets.AsQueryable(), "size", SortDirection.Ascending, w => w.Id).Select(w => w.Id)];

        // Assert
        Assert.Equal([2, 1, 3], ids);
    }

    [Fact]
    public void Apply_WhenDescending_ShouldReverseColumnButKeepTieBreakerAscending()
    {
        // Act
        int[] ids = [.. Map().Apply(Widgets.AsQueryable(), "size", SortDirection.Descending, w => w.Id).Select(w => w.Id)];

        // Assert
        Assert.Equal([1, 3, 2], ids);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Apply_WhenSortByIsBlank_ShouldUseDefaultColumn(string? sortBy)
    {
        // Act
        int[] ids = [.. Map().Apply(Widgets.AsQueryable(), sortBy, SortDirection.Ascending, w => w.Id).Select(w => w.Id)];

        // Assert
        Assert.Equal([2, 3, 1], ids);
    }

    [Fact]
    public void Apply_WhenColumnIsNotMapped_ShouldThrow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            Map().Apply(Widgets.AsQueryable(), "password", SortDirection.Ascending, w => w.Id));
    }

    private sealed record Widget(int Id, string Name, int Size);
}
