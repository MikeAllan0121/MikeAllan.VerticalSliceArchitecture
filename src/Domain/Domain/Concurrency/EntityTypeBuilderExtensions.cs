using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MikeAllan.VerticalSliceArchitecture.Domain.Aggregates;

namespace MikeAllan.VerticalSliceArchitecture.Domain.Concurrency;

/// <summary>
/// Optimistic concurrency for an aggregate, through PostgreSQL's <c>xmin</c> system column.
/// </summary>
/// <remarks>
/// Every aggregate is loaded, changed in memory, then saved, so two requests that load the same row can both
/// pass a rule like "no more than the amount due" and both save: one payment overpays or is lost. With a row
/// version, EF Core adds <c>AND xmin = @original</c> to the <c>UPDATE</c>. The second writer's update matches
/// no row, nothing of its save is committed (the whole <c>SaveChanges</c> is one transaction), and it gets a
/// <c>DbUpdateConcurrencyException</c>.
/// <para>
/// <c>xmin</c> is the id of the transaction that last wrote the row, which PostgreSQL changes on every
/// update, so there is no column to add or maintain. It is a shadow property, so the domain model doesn't
/// carry persistence plumbing. It guards only writes to the aggregate's own row: an aggregate method must
/// change a root property (as <c>Invoice.RecordPayment</c> does with <c>AmountPaid</c>), not just add a child.
/// </para>
/// </remarks>
internal static class EntityTypeBuilderExtensions
{
    /// <summary>The shadow property's name; Npgsql maps it to the <c>xmin</c> column.</summary>
    public const string RowVersion = "Version";

    extension<T>(EntityTypeBuilder<T> builder)
        where T : class, IAggregateRoot
    {
        public EntityTypeBuilder<T> HasRowVersion()
        {
            builder.Property<uint>(RowVersion).IsRowVersion();

            return builder;
        }
    }
}
