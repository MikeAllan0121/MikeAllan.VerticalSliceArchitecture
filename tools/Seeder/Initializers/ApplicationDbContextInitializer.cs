using Bogus;
using Microsoft.EntityFrameworkCore;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.Database;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;

namespace Seeder.Initializers;

public sealed class ApplicationDbContextInitializer(ApplicationDbContext dbContext)
{
    private const int NumCustomers = 20;

    private const int NumInvoices = 40;

    private readonly string[] _services =
    [
        "Consulting",
        "Design workshop",
        "Development",
        "Code review",
        "Architecture review",
        "Training session",
        "Support retainer",
        "Hosting",
        "Licence renewal",
        "Data migration"
    ];

    private enum SeededState
    {
        Draft,
        Issued,
        PartlyPaid,
        Paid
    }

    public async Task SeedDataAsync(CancellationToken cancellationToken)
    {
        var strategy = dbContext.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            // Seed the database
            await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);
            var customers = await SeedCustomers();
            await SeedInvoices(customers);
            await transaction.CommitAsync(cancellationToken);
        });
    }

    private async Task<List<Customer>> SeedCustomers()
    {
        if (dbContext.Customers.Any())
            return [];

        var faker = new Faker<Customer>()
            .CustomInstantiator(f =>
            {
                var company = f.Company.CompanyName();
                return Customer.Create(company, f.Internet.Email(company));
            });

        var customers = faker.Generate(NumCustomers);
        await dbContext.Customers.AddRangeAsync(customers);
        await dbContext.SaveChangesAsync();

        return customers;
    }

    private async Task SeedInvoices(List<Customer> customers)
    {
        if (customers.Count == 0 || dbContext.Invoices.Any())
            return;

        var lineFaker = new Faker<InvoiceLine>()
            .CustomInstantiator(f => new InvoiceLine(
                f.PickRandom(_services),
                f.Random.Number(1, 10),
                Math.Round(f.Random.Decimal(50, 2_000), 2)));

        var faker = new Faker<Invoice>()
            .CustomInstantiator(f =>
            {
                var customer = f.PickRandom(customers);
                var invoice = Invoice.Create(customer.Id, lineFaker.Generate(f.Random.Number(1, 4)));

                // Every state a real ledger has: some drafts, some owed, some part-paid, some settled.
                var state = f.PickRandom<SeededState>();

                if (state != SeededState.Draft)
                    invoice.Issue();

                if (state == SeededState.PartlyPaid)
                    invoice.RecordPayment(Math.Round(invoice.AmountDue * f.Random.Decimal(0.1m, 0.9m), 2));

                if (state == SeededState.Paid)
                    invoice.RecordPayment(invoice.AmountDue);

                return invoice;
            });

        var invoices = faker.Generate(NumInvoices);
        await dbContext.Invoices.AddRangeAsync(invoices);
        await dbContext.SaveChangesAsync();

        // No event handlers run in the Seeder, so bring each balance up to date here. Adding the invoices
        // fixed up each tracked customer's Invoices, so there is nothing to reload.
        foreach (var customer in customers)
            customer.RecalculateOutstandingBalance();

        await dbContext.SaveChangesAsync();
    }
}
