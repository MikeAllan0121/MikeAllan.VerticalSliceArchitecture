using MikeAllan.VerticalSliceArchitecture.Domain.Customers;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests;

/// <summary>Random, valid customers for seeding. Shared by the Customers, Invoices and EventualConsistency tests.</summary>
public static class CustomerFactory
{
    private static readonly Faker<Customer> CustomerFaker = new Faker<Customer>().CustomInstantiator(f =>
    {
        string company = f.Company.CompanyName();
        return Customer.Create(company, f.Internet.Email(company));
    });

    public static Customer Generate() => CustomerFaker.Generate();

    public static IReadOnlyList<Customer> Generate(int count) => CustomerFaker.Generate(count);
}
