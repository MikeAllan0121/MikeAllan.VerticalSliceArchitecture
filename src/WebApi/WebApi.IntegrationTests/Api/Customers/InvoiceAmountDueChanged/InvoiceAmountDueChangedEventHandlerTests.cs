using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;
using MikeAllan.VerticalSliceArchitecture.Internals.Api.Customers.GetAllCustomers;
using MikeAllan.VerticalSliceArchitecture.Internals.Api.Invoices.RecordPayment;
using MikeAllan.VerticalSliceArchitecture.Internals.Api.Invoices.UpdateInvoice;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests.Api.Customers.InvoiceAmountDueChanged;

/// <summary>
/// The Invoices feature raises <see cref="InvoiceAmountDueChangedEvent"/>; the Customers feature handles it.
/// The handler runs after the response (the WebApi defers domain events), so the tests poll.
/// </summary>
public sealed class InvoiceAmountDueChangedEventHandlerTests(TestingDatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    private readonly TestingDatabaseFixture _fixture = fixture;

    // Another handler saved the customer between this one loading it and saving it. Its save fails on the
    // row version, so it reloads and recalculates rather than writing a balance computed from a stale read.
    [Fact]
    public async Task RecalculationThatLosesARace_ShouldRetryAndLeaveTheBalanceCorrect()
    {
        // Arrange
        Customer customer = CustomerFactory.Generate();
        await AddAsync(customer);
        Invoice invoice = Invoice.Create(customer.Id, [new InvoiceLine("Consulting", 2, 150m)]);
        invoice.Issue();
        await AddAsync(invoice);

        ConcurrentWriteInterceptor<Customer> otherHandler = new(
            _fixture.ConnectionString,
            (db, modified) => db.Customers
                .Where(c => c.Id == modified.Id)
                .ExecuteUpdateAsync(set => set.SetProperty(c => c.UpdatedBy, "OtherHandler"), CancellationToken));
        using HttpClient client = _fixture.CreateClient(otherHandler);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/api/invoices/{invoice.Id.Value}/payments", new RecordPaymentRequest(120m), CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        bool recalculated = await WaitForBalanceAsync(customer.Id, 180m);
        Assert.True(otherHandler.Fired, "The handler's first save was never interrupted.");
        Assert.True(recalculated, "The customer's OutstandingBalance was not recalculated to 180 after the retry.");
    }

    // Different invoices, so the payments themselves don't conflict, but every one raises an event whose
    // handler rewrites the same customer's balance. Whichever saves last must have seen every payment.
    [Fact]
    public async Task PayingSeveralInvoicesAtOnce_ShouldLeaveTheBalanceCorrect()
    {
        // Arrange
        const int invoiceCount = 5;
        Customer customer = CustomerFactory.Generate();
        await AddAsync(customer);
        List<Invoice> invoices = [];
        for (int i = 0; i < invoiceCount; i++)
        {
            Invoice invoice = Invoice.Create(customer.Id, [new InvoiceLine("Consulting", 1, 100m)]);
            invoice.Issue();
            invoices.Add(invoice);
        }

        await AddRangeAsync(invoices);

        // Act
        HttpResponseMessage[] responses = await Task.WhenAll(invoices.Select(invoice =>
            Client.PostAsJsonAsync(
                $"/api/invoices/{invoice.Id.Value}/payments", new RecordPaymentRequest(100m), CancellationToken)));

        // Assert
        Assert.All(responses, response => Assert.Equal(HttpStatusCode.NoContent, response.StatusCode));

        bool settled = await WaitForBalanceAsync(customer.Id, 0m);
        Assert.True(settled, "The customer's OutstandingBalance did not settle at 0 after every invoice was paid.");
    }

    [Fact]
    public async Task IssuingAnInvoice_ShouldRecalculateTheCustomersOutstandingBalance()
    {
        // Arrange
        Customer customer = CustomerFactory.Generate();
        await AddAsync(customer);
        Invoice invoice = Invoice.Create(customer.Id, [new InvoiceLine("Consulting", 2, 150m)]);
        await AddAsync(invoice);

        // Act
        HttpResponseMessage response = await Client.PostAsync($"/api/invoices/{invoice.Id.Value}/issue", null, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        bool recalculated = await WaitForBalanceAsync(customer.Id, 300m);
        Assert.True(recalculated, "The customer's OutstandingBalance was not recalculated to 300.");

        // And the API reports it.
        PagedList<GetAllCustomersResponse> page = await GetPage<GetAllCustomersResponse>("/api/customers");
        Assert.Equal(300m, Assert.Single(page.Items).OutstandingBalance);
    }

    [Fact]
    public async Task RecordingAPayment_ShouldReduceTheCustomersOutstandingBalance()
    {
        // Arrange — seeding an issued invoice runs the handler at once (no request), so the balance is 300
        Customer customer = CustomerFactory.Generate();
        await AddAsync(customer);
        Invoice invoice = Invoice.Create(customer.Id, [new InvoiceLine("Consulting", 2, 150m)]);
        invoice.Issue();
        await AddAsync(invoice);

        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(
            $"/api/invoices/{invoice.Id.Value}/payments", new RecordPaymentRequest(120m), CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        bool recalculated = await WaitForBalanceAsync(customer.Id, 180m);
        Assert.True(recalculated, "The customer's OutstandingBalance was not recalculated to 180.");
    }

    // A draft isn't owed, so changing one raises no event and leaves the balance alone.
    [Fact]
    public async Task UpdatingADraftInvoice_ShouldLeaveTheBalanceUntouched()
    {
        // Arrange
        Customer customer = CustomerFactory.Generate();
        await AddAsync(customer);
        Invoice issued = Invoice.Create(customer.Id, [new InvoiceLine("Hosting", 1, 50m)]);
        issued.Issue();
        Invoice draft = Invoice.Create(customer.Id, [new InvoiceLine("Consulting", 1, 100m)]);
        await AddRangeAsync([issued, draft]);

        UpdateInvoiceRequest request = new([new UpdateInvoiceRequest.InvoiceLineDto("Consulting", 5, 100m)]);

        // Act
        HttpResponseMessage response = await Client.PutAsJsonAsync($"/api/invoices/{draft.Id.Value}", request, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(500m, (await GetQueryable<Invoice>().SingleAsync(i => i.Id == draft.Id, CancellationToken)).Total);
        Assert.Equal(50m, (await GetQueryable<Customer>().SingleAsync(CancellationToken)).OutstandingBalance);
    }

    private Task<bool> WaitForBalanceAsync(CustomerId customerId, decimal expected) =>
        Wait.UntilAsync(
            async () => await GetQueryable<Customer>()
                .Where(c => c.Id == customerId)
                .Select(c => c.OutstandingBalance)
                .SingleAsync(CancellationToken) == expected,
            CancellationToken);
}
