using Microsoft.EntityFrameworkCore;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;
using MikeAllan.VerticalSliceArchitecture.Api.Invoices.GetAllInvoices;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests.Api.Invoices.GetAllInvoices;

public sealed class GetAllInvoicesEndpointTests(TestingDatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    private const string Route = "/api/invoices";

    [Fact]
    public async Task Get_ShouldReturnFirstPage_WhenPagingIsNotSpecified()
    {
        // Arrange
        const int entityCount = 25;
        Customer customer = await AddCustomerAsync();
        await AddRangeAsync(InvoiceFactory.Generate(customer.Id, entityCount));

        // Act
        PagedList<GetAllInvoicesResponse> page = await GetPage<GetAllInvoicesResponse>(Route);

        // Assert
        Assert.Equal(PagingParams.DefaultPageSize, page.Items.Count);
        Assert.Equal(entityCount, page.TotalCount);
        Assert.True(page.HasNextPage);

        GetAllInvoicesResponse firstInvoice = page.Items[0];
        Assert.NotEqual(Guid.Empty, firstInvoice.Id);
        Assert.Equal(customer.Id.Value, firstInvoice.CustomerId);
        Assert.Equal(InvoiceStatus.Draft, firstInvoice.Status);
        Assert.NotEmpty(firstInvoice.Lines);
    }

    [Fact]
    public async Task Get_ShouldReturnRequestedPage()
    {
        // Arrange
        Customer customer = await AddCustomerAsync();
        await AddRangeAsync(InvoiceFactory.Generate(customer.Id, 10));
        Guid[] expected = await SortedIds(i => i.CreatedAt);

        // Act
        PagedList<GetAllInvoicesResponse> page = await GetPage<GetAllInvoicesResponse>($"{Route}?page=2&pageSize=4");

        // Assert
        Assert.Equal(2, page.Page);
        Assert.Equal(10, page.TotalCount);
        Assert.Equal(expected.Skip(4).Take(4), page.Items.Select(i => i.Id));
    }

    [Fact]
    public async Task Get_ShouldSortDescending()
    {
        // Arrange
        Customer customer = await AddCustomerAsync();
        await AddRangeAsync(InvoiceFactory.Generate(customer.Id, 5));
        decimal[] expected = await GetQueryable<Invoice>()
            .OrderBy(i => i.Total)
            .ThenBy(i => i.Id)
            .Select(i => i.Total)
            .ToArrayAsync(CancellationToken);

        // Act
        PagedList<GetAllInvoicesResponse> page = await GetPage<GetAllInvoicesResponse>($"{Route}?pageSize=5&sortBy=total&sortDirection=desc");

        // Assert
        Assert.Equal(expected.Reverse(), page.Items.Select(i => i.Total));
    }

    // AmountDue isn't mapped, so its sort column is the expression Total - AmountPaid: this pins down that
    // EF Core translates it, and that it differs from sorting by total once some invoices are part-paid.
    [Fact]
    public async Task Get_ShouldSortByAmountDue()
    {
        // Arrange
        Customer customer = await AddCustomerAsync();
        IReadOnlyList<Invoice> invoices = InvoiceFactory.Generate(customer.Id, 4);
        foreach (Invoice invoice in invoices)
        {
            invoice.Issue();
            invoice.RecordPayment(Math.Round(invoice.Total / 2, 2));
        }
        await AddRangeAsync(invoices);
        Guid[] expected = await SortedIds(i => i.Total - i.AmountPaid);

        // Act
        PagedList<GetAllInvoicesResponse> page = await GetPage<GetAllInvoicesResponse>($"{Route}?sortBy=amountDue");

        // Assert
        Assert.Equal(expected, page.Items.Select(i => i.Id));
        Assert.All(page.Items, i => Assert.True(i.AmountDue < i.Total, "Each invoice is part-paid."));
    }

    [Fact]
    public async Task Get_ShouldClampPageSize_WhenAboveTheMaximum()
    {
        // Arrange
        Customer customer = await AddCustomerAsync();
        await AddRangeAsync(InvoiceFactory.Generate(customer.Id, 5));

        // Act
        PagedList<GetAllInvoicesResponse> page = await GetPage<GetAllInvoicesResponse>($"{Route}?pageSize=5000");

        // Assert
        Assert.Equal(PagingParams.MaxPageSize, page.PageSize);
    }

    [Fact]
    public async Task Get_ShouldReturnValidationProblem_WhenSortColumnIsNotAllowed()
    {
        // Arrange
        Customer customer = await AddCustomerAsync();
        await AddRangeAsync(InvoiceFactory.Generate(customer.Id, 3));

        // Act
        HttpResponseMessage response = await Client.GetAsync($"{Route}?sortBy=notAColumn", CancellationToken);

        // Assert
        IDictionary<string, string[]> errors = await response.ShouldBeValidationProblemAsync(CancellationToken);
        string message = Assert.Single(Assert.Single(errors).Value);
        Assert.Contains("not a sortable column", message, StringComparison.Ordinal);
    }

    // Each aggregate has its own allow-list: a customer column is unknown here, not silently ignored.
    [Fact]
    public async Task Get_ShouldReturnValidationProblem_WhenSortColumnBelongsToCustomers()
    {
        // Act
        HttpResponseMessage response = await Client.GetAsync($"{Route}?sortBy=outstandingBalance", CancellationToken);

        // Assert
        IDictionary<string, string[]> errors = await response.ShouldBeValidationProblemAsync(CancellationToken);
        string message = Assert.Single(Assert.Single(errors).Value);
        Assert.Contains("not a sortable column", message, StringComparison.Ordinal);
    }

    private async Task<Customer> AddCustomerAsync()
    {
        Customer customer = CustomerFactory.Generate();
        await AddAsync(customer);
        return customer;
    }

    /// <remarks>
    /// Expected order comes back out of the database rather than a hard-coded list, because the invoices are
    /// randomly generated. Ordering mirrors <c>InvoiceQueryableExtensions.SortedBy</c> — sort column then <c>Id</c> as tie-breaker.
    /// </remarks>
    private async Task<Guid[]> SortedIds<TKey>(System.Linq.Expressions.Expression<Func<Invoice, TKey>> sortColumn) =>
        await GetQueryable<Invoice>()
            .OrderBy(sortColumn)
            .ThenBy(i => i.Id)
            .Select(i => i.Id.Value)
            .ToArrayAsync(CancellationToken);
}
