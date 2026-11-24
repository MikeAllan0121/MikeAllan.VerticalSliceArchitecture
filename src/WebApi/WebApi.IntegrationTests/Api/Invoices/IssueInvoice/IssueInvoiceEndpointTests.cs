using System.Net;
using Microsoft.EntityFrameworkCore;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests.Api.Invoices.IssueInvoice;

public sealed class IssueInvoiceEndpointTests(TestingDatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    private static string Route(Guid invoiceId) => $"/api/invoices/{invoiceId}/issue";

    [Fact]
    public async Task Post_ShouldIssueTheInvoice()
    {
        // Arrange
        Invoice invoice = await AddInvoiceAsync(InvoiceFactory.GenerateLines(2));

        // Act
        HttpResponseMessage response = await Client.PostAsync(Route(invoice.Id.Value), null, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        Invoice? updated = await GetQueryable<Invoice>()
            .ById(invoice.Id)
            .FirstOrDefaultAsync(CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(InvoiceStatus.Issued, updated.Status);
        Assert.Equal(invoice.Total, updated.AmountDue);
    }

    [Fact]
    public async Task Post_WhenInvoiceIsAlreadyIssued_ShouldReturnConflictProblem()
    {
        // Arrange
        Invoice invoice = await AddInvoiceAsync(InvoiceFactory.GenerateLines(1), issue: true);

        // Act
        HttpResponseMessage response = await Client.PostAsync(Route(invoice.Id.Value), null, CancellationToken);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invoice.NotDraft", CancellationToken);
    }

    [Fact]
    public async Task Post_WhenInvoiceHasNoLines_ShouldReturnConflictProblem()
    {
        // Arrange
        Invoice invoice = await AddInvoiceAsync([]);

        // Act
        HttpResponseMessage response = await Client.PostAsync(Route(invoice.Id.Value), null, CancellationToken);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invoice.NoLines", CancellationToken);
        Assert.Equal(InvoiceStatus.Draft, (await GetQueryable<Invoice>().SingleAsync(CancellationToken)).Status);
    }

    [Fact]
    public async Task Post_WhenInvoiceDoesNotExist_ShouldReturnNotFoundProblem()
    {
        // Act
        HttpResponseMessage response = await Client.PostAsync(Route(Guid.CreateVersion7()), null, CancellationToken);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Invoice.NotFound", CancellationToken);
    }

    [Fact]
    public async Task Post_WithEmptyInvoiceId_ShouldReturnValidationProblemForTheRouteParameter()
    {
        // Act
        HttpResponseMessage response = await Client.PostAsync(Route(Guid.Empty), null, CancellationToken);

        // Assert
        IDictionary<string, string[]> errors = await response.ShouldBeValidationProblemAsync(CancellationToken);
        Assert.Equal(["invoiceId"], errors.Keys);
    }

    private async Task<Invoice> AddInvoiceAsync(IReadOnlyList<InvoiceLine> lines, bool issue = false)
    {
        Customer customer = CustomerFactory.Generate();
        await AddAsync(customer);
        Invoice invoice = Invoice.Create(customer.Id, lines);
        if (issue)
            invoice.Issue();
        await AddAsync(invoice);
        return invoice;
    }
}
