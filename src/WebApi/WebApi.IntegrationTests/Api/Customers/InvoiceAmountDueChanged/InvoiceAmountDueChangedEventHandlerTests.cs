using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;
using MikeAllan.VerticalSliceArchitecture.Api.Customers.GetAllCustomers;
using MikeAllan.VerticalSliceArchitecture.Api.Invoices.RecordPayment;
using MikeAllan.VerticalSliceArchitecture.Api.Invoices.UpdateInvoice;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests.Api.Customers.InvoiceAmountDueChanged;

/// <summary>
/// The Invoices feature raises <see cref="InvoiceAmountDueChangedEvent"/>; the Customers feature handles it.
/// The handler runs after the response (the WebApi defers domain events), so the tests poll.
/// </summary>
public sealed class InvoiceAmountDueChangedEventHandlerTests(TestingDatabaseFixture fixture) : IntegrationTestBase(fixture)
{
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
