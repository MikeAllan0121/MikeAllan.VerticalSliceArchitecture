using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MikeAllan.VerticalSliceArchitecture.Domain.Auditing;

namespace MikeAllan.VerticalSliceArchitecture.Domain.Customers;

internal sealed class CustomerConfiguration : AuditableConfiguration<Customer>
{
    public override void PostConfigure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name)
            .HasMaxLength(Customer.NameMaxLength)
            .IsRequired();

        builder.Property(c => c.Email)
            .HasMaxLength(Customer.EmailMaxLength)
            .IsRequired();

        builder.Property(c => c.OutstandingBalance)
            .HasPrecision(18, 2);

        builder.HasMany(c => c.Invoices)
            .WithOne()
            .HasForeignKey(i => i.CustomerId)
            .IsRequired();
    }
}
