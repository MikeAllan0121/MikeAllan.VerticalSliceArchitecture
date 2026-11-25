using Microsoft.EntityFrameworkCore;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;
using MikeAllan.VerticalSliceArchitecture.Internals.Api.Customers.GetAllCustomers;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests.Api.Customers.GetAllCustomers;

/// <remarks>
/// Requests are raw URLs because what these tests pin down is the query-string contract itself: the
/// parameter names and how out-of-range values are handled.
/// </remarks>
public sealed class GetAllCustomersEndpointTests(TestingDatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    private const string Route = "/api/customers";

    [Fact]
    public async Task Get_ShouldReturnFirstPage_WhenPagingIsNotSpecified()
    {
        // Arrange
        const int entityCount = 25;
        await AddRangeAsync(CustomerFactory.Generate(entityCount));

        // Act
        PagedList<GetAllCustomersResponse> page = await GetPage<GetAllCustomersResponse>(Route);

        // Assert
        Assert.Equal(PagingParams.DefaultPageSize, page.Items.Count);
        Assert.Equal(PagingParams.FirstPage, page.Page);
        Assert.Equal(PagingParams.DefaultPageSize, page.PageSize);
        Assert.Equal(entityCount, page.TotalCount);
        Assert.Equal(3, page.TotalPages);
        Assert.False(page.HasPreviousPage);
        Assert.True(page.HasNextPage);
    }

    [Fact]
    public async Task Get_ShouldReturnRequestedPage()
    {
        // Arrange
        await AddRangeAsync(CustomerFactory.Generate(10));
        string[] expected = await SortedEmails();

        // Act
        PagedList<GetAllCustomersResponse> page = await GetPage<GetAllCustomersResponse>($"{Route}?page=2&pageSize=4&sortBy=email");

        // Assert
        Assert.Equal(2, page.Page);
        Assert.Equal(4, page.PageSize);
        Assert.Equal(10, page.TotalCount);
        Assert.Equal(expected.Skip(4).Take(4), page.Items.Select(c => c.Email));
    }

    [Fact]
    public async Task Get_ShouldReturnPartialLastPage()
    {
        // Arrange
        await AddRangeAsync(CustomerFactory.Generate(10));

        // Act
        PagedList<GetAllCustomersResponse> page = await GetPage<GetAllCustomersResponse>($"{Route}?page=3&pageSize=4");

        // Assert
        Assert.Equal(2, page.Items.Count);
        Assert.Equal(10, page.TotalCount);
        Assert.False(page.HasNextPage);
    }

    [Fact]
    public async Task Get_ShouldReturnNoItemsButTheTotalCount_WhenPageIsPastTheEnd()
    {
        // Arrange
        await AddRangeAsync(CustomerFactory.Generate(10));

        // Act
        PagedList<GetAllCustomersResponse> page = await GetPage<GetAllCustomersResponse>($"{Route}?page=99&pageSize=10");

        // Assert
        Assert.Empty(page.Items);
        Assert.Equal(10, page.TotalCount);
        Assert.Equal(99, page.Page);
    }

    [Fact]
    public async Task Get_ShouldClampPageSize_WhenAboveTheMaximum()
    {
        // Arrange — one more row than the cap, so a caller asking for everything still can't have it
        const int entityCount = PagingParams.MaxPageSize + 1;
        await AddRangeAsync(CustomerFactory.Generate(entityCount));

        // Act
        PagedList<GetAllCustomersResponse> page = await GetPage<GetAllCustomersResponse>($"{Route}?pageSize=5000");

        // Assert
        Assert.Equal(PagingParams.MaxPageSize, page.PageSize);
        Assert.Equal(PagingParams.MaxPageSize, page.Items.Count);
        Assert.Equal(entityCount, page.TotalCount);
    }

    [Fact]
    public async Task Get_ShouldClampPage_WhenBelowTheFirstPage()
    {
        // Arrange
        await AddRangeAsync(CustomerFactory.Generate(3));

        // Act
        PagedList<GetAllCustomersResponse> page = await GetPage<GetAllCustomersResponse>($"{Route}?page=0");

        // Assert
        Assert.Equal(PagingParams.FirstPage, page.Page);
        Assert.Equal(3, page.Items.Count);
    }

    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    public async Task Get_ShouldSortByAnAllowedColumn(string sortDirection)
    {
        // Arrange
        await AddRangeAsync(CustomerFactory.Generate(10));
        string[] expected = await SortedEmails();
        if (sortDirection == "desc")
            expected = [.. expected.Reverse()];

        // Act
        PagedList<GetAllCustomersResponse> page =
            await GetPage<GetAllCustomersResponse>($"{Route}?pageSize=10&sortBy=email&sortDirection={sortDirection}");

        // Assert
        Assert.Equal(expected, page.Items.Select(c => c.Email));
    }

    // A numeric column's ordering lambda compiles to Convert(c.OutstandingBalance, Object), a different
    // expression shape from a string column's — so a string-only sorting suite says nothing about whether EF
    // Core can translate the numeric one, and a numeric column is the first thing a new map gains.
    [Theory]
    [InlineData("asc")]
    [InlineData("desc")]
    public async Task Get_ShouldSortByANumericColumn(string sortDirection)
    {
        // Arrange
        await AddCustomersWithBalancesAsync(10);
        decimal[] expected = await SortedOutstandingBalances();
        if (sortDirection == "desc")
            expected = [.. expected.Reverse()];

        // Act
        PagedList<GetAllCustomersResponse> page =
            await GetPage<GetAllCustomersResponse>($"{Route}?pageSize=10&sortBy=outstandingBalance&sortDirection={sortDirection}");

        // Assert
        Assert.Equal(expected, page.Items.Select(c => c.OutstandingBalance));
    }

    [Fact]
    public async Task Get_ShouldReturnNoItems_WhenPageIsFarEnoughPastTheEndToOverflow()
    {
        // Arrange — (page - 1) * pageSize overflows int, so this is the request that used to produce a
        // negative OFFSET rather than an empty page
        await AddRangeAsync(CustomerFactory.Generate(3));

        // Act
        PagedList<GetAllCustomersResponse> page = await GetPage<GetAllCustomersResponse>($"{Route}?page={int.MaxValue}&pageSize=100");

        // Assert
        Assert.Empty(page.Items);
        Assert.Equal(3, page.TotalCount);
    }

    [Fact]
    public async Task Get_ShouldReturnValidationProblem_WhenSortColumnIsNotAllowed()
    {
        // Arrange
        await AddRangeAsync(CustomerFactory.Generate(3));

        // Act
        HttpResponseMessage response = await Client.GetAsync($"{Route}?sortBy=notAColumn", CancellationToken);

        // Assert
        IDictionary<string, string[]> errors = await response.ShouldBeValidationProblemAsync(CancellationToken);
        string message = Assert.Single(Assert.Single(errors).Value);
        Assert.Contains("not a sortable column", message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_ShouldReturnValidationProblem_WhenSortDirectionIsNotAllowed()
    {
        // Arrange
        await AddRangeAsync(CustomerFactory.Generate(3));

        // Act
        HttpResponseMessage response = await Client.GetAsync($"{Route}?sortBy=name&sortDirection=sideways", CancellationToken);

        // Assert
        IDictionary<string, string[]> errors = await response.ShouldBeValidationProblemAsync(CancellationToken);
        string message = Assert.Single(Assert.Single(errors).Value);
        Assert.Contains("not a valid sort direction", message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Get_ShouldReturnCustomerDetails()
    {
        // Arrange
        Customer seeded = CustomerFactory.Generate();
        await AddAsync(seeded);

        // Act
        PagedList<GetAllCustomersResponse> page = await GetPage<GetAllCustomersResponse>(Route);

        // Assert
        GetAllCustomersResponse customer = Assert.Single(page.Items);
        Assert.Equal(seeded.Id.Value, customer.Id);
        Assert.Equal(seeded.Name, customer.Name);
        Assert.Equal(seeded.Email, customer.Email);
        Assert.Equal(0m, customer.OutstandingBalance);
    }

    /// <summary>
    /// Customers with differing balances, each from one issued invoice. Outside an HTTP request the issue
    /// event is handled during the seeding <c>SaveChanges</c>, so the balances are in place on return.
    /// </summary>
    private async Task AddCustomersWithBalancesAsync(int count)
    {
        IReadOnlyList<Customer> customers = CustomerFactory.Generate(count);
        await AddRangeAsync(customers);

        List<Invoice> invoices = [.. customers.Select(c => InvoiceFactory.Generate(c.Id))];
        foreach (Invoice invoice in invoices)
            invoice.Issue();
        await AddRangeAsync(invoices);

        Assert.True(
            await GetQueryable<Customer>().Select(c => c.OutstandingBalance).Distinct().CountAsync(CancellationToken) > 1,
            "The seeded customers' balances were not recalculated.");
    }

    /// <remarks>
    /// The expected order comes back out of the database rather than a hard-coded list, because the customers
    /// are randomly generated. Ordering here mirrors <c>CustomerQueryableExtensions.SortedBy</c> — sort column then <c>Id</c> as tie-breaker.
    /// </remarks>
    private async Task<string[]> SortedEmails() =>
        await GetQueryable<Customer>()
            .OrderBy(c => c.Email)
            .ThenBy(c => c.Id)
            .Select(c => c.Email)
            .ToArrayAsync(CancellationToken);

    private async Task<decimal[]> SortedOutstandingBalances() =>
        await GetQueryable<Customer>()
            .OrderBy(c => c.OutstandingBalance)
            .ThenBy(c => c.Id)
            .Select(c => c.OutstandingBalance)
            .ToArrayAsync(CancellationToken);
}
