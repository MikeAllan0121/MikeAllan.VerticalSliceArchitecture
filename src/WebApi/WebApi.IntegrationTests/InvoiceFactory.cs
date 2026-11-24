using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests;

/// <summary>
/// Random, valid draft invoices for seeding, each with one to four lines. Shared by the Customers, Invoices
/// and EventualConsistency tests.
/// </summary>
/// <remarks>
/// Seed the customer first. Issuing or paying an invoice raises an event, and outside an HTTP request it is
/// handled during the seeding <c>SaveChanges</c>: the handler loads the customer, and throws if it isn't there.
/// </remarks>
public static class InvoiceFactory
{
    private static readonly Faker<InvoiceLine> LineFaker =
        new Faker<InvoiceLine>().CustomInstantiator(f => new InvoiceLine(
            f.Commerce.ProductName(),
            f.Random.Number(1, 10),
            Math.Round(f.Random.Decimal(1, 500), 2)));

    public static Invoice Generate(CustomerId customerId) => InvoiceFaker(customerId).Generate();

    public static IReadOnlyList<Invoice> Generate(CustomerId customerId, int count) => InvoiceFaker(customerId).Generate(count);

    public static IReadOnlyList<InvoiceLine> GenerateLines(int count) => LineFaker.Generate(count);

    private static Faker<Invoice> InvoiceFaker(CustomerId customerId) =>
        new Faker<Invoice>().CustomInstantiator(f => Invoice.Create(customerId, LineFaker.Generate(f.Random.Number(1, 4))));
}
