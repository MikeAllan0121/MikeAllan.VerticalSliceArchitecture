using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using MikeAllan.VerticalSliceArchitecture.Domain.Aggregates;

namespace MikeAllan.VerticalSliceArchitecture.Domain.Database;

/// <summary>Stores a strongly typed ID as its <see cref="Guid"/>.</summary>
/// <remarks>
/// <c>TId.From</c> is a static abstract member, which an expression tree can't call, so the
/// conversion goes through <see cref="Create"/>.
/// </remarks>
internal sealed class GuidIdConverter<TId>() : ValueConverter<TId, Guid>(id => id.Value, value => Create(value))
    where TId : struct, IGuidId<TId>
{
    private static TId Create(Guid value) => TId.From(value);
}
