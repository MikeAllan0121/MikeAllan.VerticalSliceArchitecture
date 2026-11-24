using MikeAllan.VerticalSliceArchitecture.Domain.Aggregates;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;

namespace MikeAllan.VerticalSliceArchitecture.Domain.Database;

public sealed class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<Customer> Customers => AggregateRootSet<Customer>();

    public DbSet<Invoice> Invoices => AggregateRootSet<Invoice>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Listed, not scanned: a new aggregate's configuration is added here alongside its DbSet.
        modelBuilder.ApplyConfiguration(new CustomerConfiguration());
        modelBuilder.ApplyConfiguration(new InvoiceConfiguration());
        modelBuilder.ApplyConfiguration(new PaymentConfiguration());

        base.OnModelCreating(modelBuilder);
    }

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        base.ConfigureConventions(configurationBuilder);

        // Every strongly typed ID is listed here: one that isn't has no converter, and EF throws when it
        // first tries to map the property (at startup, not at build).
        configurationBuilder.Properties<CustomerId>().HaveConversion<GuidIdConverter<CustomerId>>();
        configurationBuilder.Properties<InvoiceId>().HaveConversion<GuidIdConverter<InvoiceId>>();
        configurationBuilder.Properties<PaymentId>().HaveConversion<GuidIdConverter<PaymentId>>();
    }

    private DbSet<T> AggregateRootSet<T>() where T : class, IAggregateRoot => Set<T>();
}
