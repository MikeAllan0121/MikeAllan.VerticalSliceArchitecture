using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MikeAllan.VerticalSliceArchitecture.Domain.Auditing;

namespace MikeAllan.VerticalSliceArchitecture.Domain.Invoices;

internal sealed class PaymentConfiguration : AuditableConfiguration<Payment>
{
    public override void PostConfigure(EntityTypeBuilder<Payment> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Amount)
            .HasPrecision(18, 2);
    }
}
