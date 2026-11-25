using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Internals.Api.Invoices.UpdateInvoice;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests.Api.Invoices.UpdateInvoice;

public sealed class UpdateInvoiceEndpointTests(TestingDatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    private static string Route(Guid invoiceId) => $"/api/invoices/{invoiceId}";

    [Fact]
    public async Task Put_ShouldReplaceTheLinesOfADraft()
    {
        // Arrange
        Invoice invoice = await AddInvoiceAsync();
        UpdateInvoiceRequest request = new(
            [
                new UpdateInvoiceRequest.InvoiceLineDto("Consulting", 3, 100m),
                new UpdateInvoiceRequest.InvoiceLineDto("Hosting", 1, 25.50m)
            ]);

        // Act
        HttpResponseMessage response = await Client.PutAsJsonAsync(Route(invoice.Id.Value), request, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        Invoice item = await GetQueryable<Invoice>().SingleAsync(i => i.Id == invoice.Id, CancellationToken);
        Assert.Equal(325.50m, item.Total);
        Assert.Equal(["Consulting", "Hosting"], item.Lines.Select(l => l.Description));
        Assert.NotNull(item.UpdatedAt);
    }

    [Fact]
    public async Task Put_WhenInvoiceDoesNotExist_ShouldReturnNotFoundProblem()
    {
        // Arrange
        UpdateInvoiceRequest request = new([new UpdateInvoiceRequest.InvoiceLineDto("Consulting", 1, 100m)]);

        // Act
        HttpResponseMessage response = await Client.PutAsJsonAsync(Route(Guid.CreateVersion7()), request, CancellationToken);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Invoice.NotFound", CancellationToken);
    }

    // A domain conflict is a 409, not a 400: the request was well-formed, the invoice's state forbids it.
    [Fact]
    public async Task Put_WhenInvoiceIsIssued_ShouldReturnConflictProblemAndLeaveItUnchanged()
    {
        // Arrange
        Invoice invoice = await AddInvoiceAsync(issue: true);
        UpdateInvoiceRequest request = new([new UpdateInvoiceRequest.InvoiceLineDto("Consulting", 1, 999m)]);

        // Act
        HttpResponseMessage response = await Client.PutAsJsonAsync(Route(invoice.Id.Value), request, CancellationToken);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invoice.NotDraft", CancellationToken);
        Assert.Equal(invoice.Total, (await GetQueryable<Invoice>().SingleAsync(CancellationToken)).Total);
    }

    [Fact]
    public async Task Put_WithEmptyInvoiceId_ShouldReturnValidationProblemForTheRouteParameter()
    {
        // Arrange
        UpdateInvoiceRequest request = new([new UpdateInvoiceRequest.InvoiceLineDto("Consulting", 1, 100m)]);

        // Act
        HttpResponseMessage response = await Client.PutAsJsonAsync(Route(Guid.Empty), request, CancellationToken);

        // Assert
        IDictionary<string, string[]> errors = await response.ShouldBeValidationProblemAsync(CancellationToken);
        Assert.Equal(["invoiceId"], errors.Keys);
    }

    [Fact]
    public async Task Put_WithInvalidLine_ShouldReturnValidationProblemAndLeaveInvoiceUnchanged()
    {
        // Arrange
        Invoice invoice = await AddInvoiceAsync();
        UpdateInvoiceRequest request = new([new UpdateInvoiceRequest.InvoiceLineDto("Consulting", InvoiceLine.QuantityMax + 1, 100m)]);

        // Act
        HttpResponseMessage response = await Client.PutAsJsonAsync(Route(invoice.Id.Value), request, CancellationToken);

        // Assert
        IDictionary<string, string[]> errors = await response.ShouldBeValidationProblemAsync(CancellationToken);
        Assert.Equal(["Lines[0].Quantity"], errors.Keys);

        Invoice item = await GetQueryable<Invoice>().SingleAsync(i => i.Id == invoice.Id, CancellationToken);
        Assert.Equal(invoice.Total, item.Total);
    }

    private async Task<Invoice> AddInvoiceAsync(bool issue = false)
    {
        Customer customer = CustomerFactory.Generate();
        await AddAsync(customer);
        Invoice invoice = InvoiceFactory.Generate(customer.Id);
        if (issue)
            invoice.Issue();
        await AddAsync(invoice);
        return invoice;
    }
}
