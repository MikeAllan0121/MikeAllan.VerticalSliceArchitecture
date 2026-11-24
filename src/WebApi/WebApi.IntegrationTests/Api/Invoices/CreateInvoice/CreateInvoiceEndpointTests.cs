using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Api.Invoices.CreateInvoice;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests.Api.Invoices.CreateInvoice;

public sealed class CreateInvoiceEndpointTests(TestingDatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    private const string Route = "/api/invoices";

    [Fact]
    public async Task Post_ShouldCreateADraftInvoiceAndReturnItsId()
    {
        // Arrange
        Customer customer = CustomerFactory.Generate();
        await AddAsync(customer);
        CreateInvoiceRequest request = new(
            customer.Id.Value,
            [
                new CreateInvoiceRequest.InvoiceLineDto("Consulting", 2, 150m),
                new CreateInvoiceRequest.InvoiceLineDto("Hosting", 1, 49.99m),
                new CreateInvoiceRequest.InvoiceLineDto("Hosting", 1, 49.99m)
            ]);

        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(Route, request, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        CreateInvoiceResponse body = await ReadJsonAsync<CreateInvoiceResponse>(response);

        Invoice item = await GetQueryable<Invoice>().SingleAsync(CancellationToken);
        Assert.Equal(body.Id, item.Id.Value);
        Assert.Equal(customer.Id, item.CustomerId);
        Assert.Equal(InvoiceStatus.Draft, item.Status);
        Assert.Equal(399.98m, item.Total);
        Assert.Equal(3, item.Lines.Count);
        Assert.InRange(item.CreatedAt, DateTimeOffset.UtcNow.AddSeconds(-10), DateTimeOffset.UtcNow.AddSeconds(10));
    }

    // Nested items are validated too, and the keys index into the collection.
    [Fact]
    public async Task Post_WithInvalidLine_ShouldReturnValidationProblemKeyedByIndex()
    {
        // Arrange
        Customer customer = CustomerFactory.Generate();
        await AddAsync(customer);
        CreateInvoiceRequest request = new(
            customer.Id.Value,
            [
                new CreateInvoiceRequest.InvoiceLineDto("Consulting", 2, 150m),
                new CreateInvoiceRequest.InvoiceLineDto("Hosting", 0, 10.555m)
            ]);

        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(Route, request, CancellationToken);

        // Assert
        IDictionary<string, string[]> errors = await response.ShouldBeValidationProblemAsync(CancellationToken);
        Assert.Equal(["Lines[1].Quantity", "Lines[1].UnitPrice"], errors.Keys.Order(StringComparer.Ordinal));
        Assert.False(await GetQueryable<Invoice>().AnyAsync(CancellationToken));
    }

    [Fact]
    public async Task Post_WhenCustomerDoesNotExist_ShouldReturnNotFoundProblem()
    {
        // Arrange
        CreateInvoiceRequest request = new(Guid.CreateVersion7(), [new CreateInvoiceRequest.InvoiceLineDto("Consulting", 1, 100m)]);

        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(Route, request, CancellationToken);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Customer.NotFound", CancellationToken);
        Assert.False(await GetQueryable<Invoice>().AnyAsync(CancellationToken));
    }

    // A body that omits "customerId" binds Guid.Empty, which [NotEmptyGuid] rejects.
    [Fact]
    public async Task Post_WithoutCustomerId_ShouldReturnValidationProblem()
    {
        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(Route, new { lines = Array.Empty<object>() }, CancellationToken);

        // Assert
        IDictionary<string, string[]> errors = await response.ShouldBeValidationProblemAsync(CancellationToken);
        Assert.Equal([nameof(CreateInvoiceRequest.CustomerId)], errors.Keys);
    }

    // The endpoint enumerates Lines unconditionally: without [Required] this would be a 500.
    [Fact]
    public async Task Post_WithoutLines_ShouldReturnValidationProblem()
    {
        // Arrange
        Customer customer = CustomerFactory.Generate();
        await AddAsync(customer);

        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(Route, new { customerId = customer.Id.Value }, CancellationToken);

        // Assert
        IDictionary<string, string[]> errors = await response.ShouldBeValidationProblemAsync(CancellationToken);
        Assert.Equal([nameof(CreateInvoiceRequest.Lines)], errors.Keys);
    }

    [Fact]
    public async Task Post_WithUnitPriceAboveTheDomainLimit_ShouldReturnValidationProblem()
    {
        // Arrange
        Customer customer = CustomerFactory.Generate();
        await AddAsync(customer);
        CreateInvoiceRequest request = new(
            customer.Id.Value,
            [new CreateInvoiceRequest.InvoiceLineDto("Consulting", 1, InvoiceLine.UnitPriceMax + 0.01m)]);

        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(Route, request, CancellationToken);

        // Assert
        IDictionary<string, string[]> errors = await response.ShouldBeValidationProblemAsync(CancellationToken);
        Assert.Equal(["Lines[0].UnitPrice"], errors.Keys);
    }
}
