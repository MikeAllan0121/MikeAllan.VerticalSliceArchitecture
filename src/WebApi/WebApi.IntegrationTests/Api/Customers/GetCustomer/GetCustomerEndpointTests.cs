using System.Net;
using System.Text.Json;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Internals.Api.Customers.GetCustomer;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests.Api.Customers.GetCustomer;

public sealed class GetCustomerEndpointTests(TestingDatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    private static string Route(Guid customerId) => $"/api/customers/{customerId}";

    [Fact]
    public async Task Get_ShouldReturnCustomerWithTheirInvoicesAndLines()
    {
        // Arrange
        Customer customer = CustomerFactory.Generate();
        await AddAsync(customer);
        Invoice invoice = InvoiceFactory.Generate(customer.Id);
        invoice.Issue();
        invoice.RecordPayment(0.01m);
        await AddAsync(invoice);

        // Act
        HttpResponseMessage response = await Client.GetAsync(Route(customer.Id.Value), CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        GetCustomerResponse body = await ReadJsonAsync<GetCustomerResponse>(response);
        Assert.Equal(customer.Id.Value, body.Id);
        Assert.Equal(customer.Name, body.Name);
        Assert.Equal(customer.Email, body.Email);

        GetCustomerResponse.GetCustomerInvoiceDto invoiceDto = Assert.Single(body.Invoices);
        Assert.Equal(invoice.Id.Value, invoiceDto.Id);
        Assert.Equal(InvoiceStatus.Issued, invoiceDto.Status);
        Assert.Equal(invoice.Total, invoiceDto.Total);
        Assert.Equal(invoice.Total - 0.01m, invoiceDto.AmountDue);
        Assert.Equal(
            invoice.Lines.Select(l => (l.Description, l.Quantity, l.UnitPrice)).Order(),
            invoiceDto.Lines.Select(l => (l.Description, l.Quantity, l.UnitPrice)).Order());
    }

    // InvoiceStatus is the first enum the API returns: it travels as its name, not its number.
    [Fact]
    public async Task Get_ShouldReturnTheInvoiceStatusAsItsName()
    {
        // Arrange
        Customer customer = CustomerFactory.Generate();
        await AddAsync(customer);
        await AddAsync(InvoiceFactory.Generate(customer.Id));

        // Act
        HttpResponseMessage response = await Client.GetAsync(Route(customer.Id.Value), CancellationToken);

        // Assert
        using JsonDocument document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(CancellationToken));
        JsonElement status = document.RootElement.GetProperty("invoices")[0].GetProperty("status");
        Assert.Equal(JsonValueKind.String, status.ValueKind);
        Assert.Equal(nameof(InvoiceStatus.Draft), status.GetString());
    }

    [Fact]
    public async Task Get_WhenCustomerDoesNotExist_ShouldReturnNotFoundProblem()
    {
        // Act
        HttpResponseMessage response = await Client.GetAsync(Route(Guid.CreateVersion7()), CancellationToken);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Customer.NotFound", CancellationToken);
    }

    [Fact]
    public async Task Get_WithEmptyCustomerId_ShouldReturnValidationProblemForTheRouteParameter()
    {
        // Act
        HttpResponseMessage response = await Client.GetAsync(Route(Guid.Empty), CancellationToken);

        // Assert
        IDictionary<string, string[]> errors = await response.ShouldBeValidationProblemAsync(CancellationToken);
        Assert.Equal(["customerId"], errors.Keys);
    }
}
