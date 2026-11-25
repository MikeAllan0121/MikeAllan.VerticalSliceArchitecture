using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MikeAllan.VerticalSliceArchitecture.Domain.Auditing;
using MikeAllan.VerticalSliceArchitecture.Domain.Concurrency;

namespace MikeAllan.VerticalSliceArchitecture.Domain.Invoices;

internal sealed class InvoiceConfiguration : AuditableConfiguration<Invoice>
{
    public override void PostConfigure(EntityTypeBuilder<Invoice> builder)
    {
        builder.HasKey(i => i.Id);

        // Two concurrent payments must not both pass the amount-due check; see HasRowVersion.
        builder.HasRowVersion();

        builder.Property(i => i.Total)
            .HasPrecision(18, 2);

        builder.Property(i => i.AmountPaid)
            .HasPrecision(18, 2);

        // Computed from Total and AmountPaid, so it has no column. Queries use Total - AmountPaid.
        builder.Ignore(i => i.AmountDue);

        // This is to highlight that we can also serialise to JSON for simple content instead of adding a new table
        builder.OwnsMany(i => i.Lines, b =>
        {
            b.ToJson();
            b.Property(l => l.Description)
                .HasMaxLength(InvoiceLine.DescriptionMaxLength)
                .IsRequired();
            b.Property(l => l.UnitPrice)
                .HasPrecision(18, 2);
        });

        builder.HasMany(i => i.Payments)
            .WithOne()
            .IsRequired();
    }
}
