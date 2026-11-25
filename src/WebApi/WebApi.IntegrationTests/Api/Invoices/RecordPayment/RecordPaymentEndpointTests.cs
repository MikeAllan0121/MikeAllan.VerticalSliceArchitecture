using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Internals.Api.Invoices.RecordPayment;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests.Api.Invoices.RecordPayment;

public sealed class RecordPaymentEndpointTests(TestingDatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    private readonly TestingDatabaseFixture _fixture = fixture;

    private static string Route(Guid invoiceId) => $"/api/invoices/{invoiceId}/payments";

    // Another request changed the invoice between this one loading it and saving it. Without a row version
    // both would pass the amount-due check against the same stale AmountPaid.
    [Fact]
    public async Task Post_WhenTheInvoiceChangesBeforeTheSave_ShouldReturnConcurrencyConflictAndRecordNothing()
    {
        // Arrange
        Invoice invoice = await AddInvoiceAsync(issue: true);
        ConcurrentWriteInterceptor<Invoice> otherRequest = new(
            _fixture.ConnectionString,
            (db, modified) => db.Invoices
                .Where(i => i.Id == modified.Id)
                .ExecuteUpdateAsync(set => set.SetProperty(i => i.UpdatedBy, "OtherRequest"), CancellationToken));
        using HttpClient client = _fixture.CreateClient(otherRequest);

        // Act
        HttpResponseMessage response =
            await client.PostAsJsonAsync(Route(invoice.Id.Value), new RecordPaymentRequest(40m), CancellationToken);

        // Assert
        Assert.True(otherRequest.Fired);
        await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Concurrency.Conflict", CancellationToken);
        Assert.False(await GetQueryable<Payment>().AnyAsync(CancellationToken));
        Assert.Equal(0m, (await GetQueryable<Invoice>().SingleAsync(CancellationToken)).AmountPaid);
    }

    // The 409 committed nothing, so sending the same request again is safe and goes through.
    [Fact]
    public async Task Post_RetriedAfterAConcurrencyConflict_ShouldRecordThePaymentOnce()
    {
        // Arrange
        Invoice invoice = await AddInvoiceAsync(issue: true);
        ConcurrentWriteInterceptor<Invoice> otherRequest = new(
            _fixture.ConnectionString,
            (db, modified) => db.Invoices
                .Where(i => i.Id == modified.Id)
                .ExecuteUpdateAsync(set => set.SetProperty(i => i.UpdatedBy, "OtherRequest"), CancellationToken));
        using HttpClient client = _fixture.CreateClient(otherRequest);
        HttpResponseMessage conflict =
            await client.PostAsJsonAsync(Route(invoice.Id.Value), new RecordPaymentRequest(40m), CancellationToken);
        await conflict.ShouldBeProblemAsync(HttpStatusCode.Conflict, "Concurrency.Conflict", CancellationToken);

        // Act
        HttpResponseMessage retry =
            await client.PostAsJsonAsync(Route(invoice.Id.Value), new RecordPaymentRequest(40m), CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, retry.StatusCode);
        Assert.Equal(40m, (await GetQueryable<Payment>().SingleAsync(CancellationToken)).Amount);
        Assert.Equal(40m, (await GetQueryable<Invoice>().SingleAsync(CancellationToken)).AmountPaid);
    }

    // Real requests racing on one invoice: 10 payments of 30 against 100 due. At most three fit. Every request
    // either records its payment (204) or records nothing (409), so what was saved always adds up.
    [Fact]
    public async Task Post_ConcurrentPayments_ShouldNeverOverpayOrLoseAPayment()
    {
        // Arrange
        const int requests = 10;
        const decimal amount = 30m;
        Invoice invoice = await AddInvoiceAsync(issue: true);

        // Act
        HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, requests).Select(_ =>
            Client.PostAsJsonAsync(Route(invoice.Id.Value), new RecordPaymentRequest(amount), CancellationToken)));

        // Assert
        int accepted = 0;
        foreach (HttpResponseMessage response in responses)
        {
            if (response.StatusCode == HttpStatusCode.NoContent)
            {
                accepted++;
                continue;
            }

            ProblemDetails problem = await response.ShouldBeProblemAsync(HttpStatusCode.Conflict, CancellationToken);
            Assert.Contains(problem.Extensions["code"]?.ToString(), (string?[])["Invoice.Overpayment", "Concurrency.Conflict"]);
        }

        Invoice saved = await GetQueryable<Invoice>().ById(invoice.Id).SingleAsync(CancellationToken);
        Assert.InRange(accepted, 1, 3);
        Assert.Equal(accepted, saved.Payments.Count);
        Assert.Equal(accepted * amount, saved.AmountPaid);
        Assert.Equal(saved.Payments.Sum(p => p.Amount), saved.AmountPaid);
        Assert.True(saved.AmountPaid <= saved.Total, $"Overpaid: {saved.AmountPaid} paid against {saved.Total}.");

        // And the customer's balance, recalculated by the racing event handlers, ends at what is still due.
        bool settled = await Wait.UntilAsync(
            async () => await GetQueryable<Customer>().Select(c => c.OutstandingBalance).SingleAsync(CancellationToken)
                == saved.Total - saved.AmountPaid,
            CancellationToken);
        Assert.True(settled, $"The customer's balance did not settle at {saved.Total - saved.AmountPaid}.");
    }

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
