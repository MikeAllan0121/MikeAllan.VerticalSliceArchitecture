using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Api.Invoices.RecordPayment;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests.Api.Invoices.RecordPayment;

public sealed class RecordPaymentEndpointTests(TestingDatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    private static string Route(Guid invoiceId) => $"/api/invoices/{invoiceId}/payments";

    [Fact]
    public async Task Post_WithPartOfTheAmountDue_ShouldRecordItAndLeaveTheInvoiceIssued()
    {
        // Arrange
        Invoice invoice = await AddInvoiceAsync(issue: true);

        // Act
        HttpResponseMessage response =
            await Client.PostAsJsonAsync(Route(invoice.Id.Value), new RecordPaymentRequest(40m), CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        Invoice? updated = await GetQueryable<Invoice>()
            .ById(invoice.Id)
            .FirstOrDefaultAsync(CancellationToken);
        Assert.NotNull(updated);
        Assert.Equal(InvoiceStatus.Issued, updated.Status);
        Assert.Equal(40m, updated.AmountPaid);
        Assert.Equal(60m, updated.AmountDue);
        Assert.Equal(40m, Assert.Single(updated.Payments).Amount);
    }

    [Fact]
    public async Task Post_WithTheWholeAmountDue_ShouldMarkTheInvoicePaid()
    {
        // Arrange
        Invoice invoice = await AddInvoiceAsync(issue: true);

        // Act
        HttpResponseMessage response =
            await Client.PostAsJsonAsync(Route(invoice.Id.Value), new RecordPaymentRequest(100m), CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        Invoice updated = await GetQueryable<Invoice>().SingleAsync(CancellationToken);
        Assert.Equal(InvoiceStatus.Paid, updated.Status);
        Assert.Equal(0m, updated.AmountDue);
    }

    // A domain conflict is a 409, not a 400: the request was well-formed, the invoice's state forbids it.
    [Fact]
    public async Task Post_WhenInvoiceIsADraft_ShouldReturnConflictProblem()
    {
        // Arrange
        Invoice invoice = await AddInvoiceAsync(issue: false);

        // Act
        HttpResponseMessage response =
            await Client.PostAsJsonAsync(Route(invoice.Id.Value), new RecordPaymentRequest(10m), CancellationToken);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invoice.NotIssued", CancellationToken);
    }

    [Fact]
    public async Task Post_WithMoreThanTheAmountDue_ShouldReturnConflictProblemAndRecordNothing()
    {
        // Arrange
        Invoice invoice = await AddInvoiceAsync(issue: true);

        // Act
        HttpResponseMessage response =
            await Client.PostAsJsonAsync(Route(invoice.Id.Value), new RecordPaymentRequest(100.01m), CancellationToken);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Invoice.Overpayment", CancellationToken);
        Assert.False(await GetQueryable<Payment>().AnyAsync(CancellationToken));
    }

    [Fact]
    public async Task Post_WhenInvoiceDoesNotExist_ShouldReturnNotFoundProblem()
    {
        // Act
        HttpResponseMessage response =
            await Client.PostAsJsonAsync(Route(Guid.CreateVersion7()), new RecordPaymentRequest(10m), CancellationToken);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Invoice.NotFound", CancellationToken);
    }

    // The domain throws on these, so the request has to turn them into a 400 first.
    [Theory]
    [InlineData("0")]
    [InlineData("-5")]
    [InlineData("10.005")]
    public async Task Post_WithAnInvalidAmount_ShouldReturnValidationProblem(string amount)
    {
        // Arrange
        Invoice invoice = await AddInvoiceAsync(issue: true);
        RecordPaymentRequest request = new(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture));

        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(Route(invoice.Id.Value), request, CancellationToken);

        // Assert
        IDictionary<string, string[]> errors = await response.ShouldBeValidationProblemAsync(CancellationToken);
        Assert.Equal([nameof(RecordPaymentRequest.Amount)], errors.Keys);
    }

    [Fact]
    public async Task Post_WithEmptyInvoiceId_ShouldReturnValidationProblemForTheRouteParameter()
    {
        // Act
        HttpResponseMessage response =
            await Client.PostAsJsonAsync(Route(Guid.Empty), new RecordPaymentRequest(10m), CancellationToken);

        // Assert
        IDictionary<string, string[]> errors = await response.ShouldBeValidationProblemAsync(CancellationToken);
        Assert.Equal(["invoiceId"], errors.Keys);
    }

    private async Task<Invoice> AddInvoiceAsync(bool issue)
    {
        Customer customer = CustomerFactory.Generate();
        await AddAsync(customer);
        Invoice invoice = Invoice.Create(customer.Id, [new InvoiceLine("Consulting", 1, 100m)]);
        if (issue)
            invoice.Issue();
        await AddAsync(invoice);
        return invoice;
    }
}
