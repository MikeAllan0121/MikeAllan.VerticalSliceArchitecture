namespace MikeAllan.VerticalSliceArchitecture.Domain.Aggregates;

/// <summary>
/// A strongly typed ID wrapping a <see cref="Guid"/>: <c>public readonly record struct CustomerId(Guid Value) : IGuidId&lt;CustomerId&gt;</c>.
/// EF Core maps it through <c>GuidIdConverter&lt;TId&gt;</c>, so a new ID is also registered in
/// <c>ApplicationDbContext.ConfigureConventions</c>.
/// </summary>
/// <remarks>
/// For strongly typed IDs, check out the rule: https://www.ssw.com.au/rules/do-you-use-strongly-typed-ids/
/// </remarks>
public interface IGuidId<TSelf> where TSelf : struct, IGuidId<TSelf>
{
    Guid Value { get; }

    static abstract TSelf From(Guid value);
}
