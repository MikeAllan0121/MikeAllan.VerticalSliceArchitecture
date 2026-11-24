namespace MikeAllan.VerticalSliceArchitecture.Domain.Errors;

/// <summary>
/// The success case of <see cref="Result"/>: the operation worked and has nothing to return.
/// </summary>
/// <remarks>
/// A class rather than a struct so a <see cref="Result"/> holding it stores a reference instead of boxing
/// a value, and a singleton because every success is the same: return <see cref="Instance"/>.
/// </remarks>
public sealed class Success
{
    /// <summary>The one success value.</summary>
    public static Success Instance { get; } = new();

    private Success() { }
}
