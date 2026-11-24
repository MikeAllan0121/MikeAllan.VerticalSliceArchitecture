using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Api.Customers.CreateCustomer;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests.Api.Customers.CreateCustomer;

public sealed class CreateCustomerEndpointTests(TestingDatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    private const string Route = "/api/customers";

    [Fact]
    public async Task Post_ShouldCreateCustomerAndReturnItsId()
    {
        // Arrange
        CreateCustomerRequest request = new("Contoso", "accounts@contoso.com");

        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(Route, request, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        CreateCustomerResponse body = await ReadJsonAsync<CreateCustomerResponse>(response);

        Customer item = await GetQueryable<Customer>().SingleAsync(CancellationToken);
        Assert.Equal(body.Id, item.Id.Value);
        Assert.Equal(request.Name, item.Name);
        Assert.Equal(request.Email, item.Email);
        Assert.Equal(0m, item.OutstandingBalance);
        Assert.InRange(item.CreatedAt, DateTimeOffset.UtcNow.AddSeconds(-10), DateTimeOffset.UtcNow.AddSeconds(10));
    }

    // The domain guards only blank and length, so an address with no '@' is the request's to reject.
    [Fact]
    public async Task Post_WithInvalidEmail_ShouldReturnValidationProblem()
    {
        // Arrange
        CreateCustomerRequest request = new("Contoso", "not-an-email");

        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(Route, request, CancellationToken);

        // Assert
        IDictionary<string, string[]> errors = await response.ShouldBeValidationProblemAsync(CancellationToken);
        Assert.Equal([nameof(CreateCustomerRequest.Email)], errors.Keys);
        Assert.False(await GetQueryable<Customer>().AnyAsync(CancellationToken));
    }

    [Fact]
    public async Task Post_WithNameAboveTheDomainLimit_ShouldReturnValidationProblem()
    {
        // Arrange
        CreateCustomerRequest request = new(new string('a', Customer.NameMaxLength + 1), "accounts@contoso.com");

        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(Route, request, CancellationToken);

        // Assert
        IDictionary<string, string[]> errors = await response.ShouldBeValidationProblemAsync(CancellationToken);
        Assert.Equal([nameof(CreateCustomerRequest.Name)], errors.Keys);
    }

    // The domain setter throws on a null email: without [Required] this would be a 500.
    [Fact]
    public async Task Post_WithoutEmail_ShouldReturnValidationProblem()
    {
        // Act
        HttpResponseMessage response = await Client.PostAsJsonAsync(Route, new { name = "Contoso" }, CancellationToken);

        // Assert
        IDictionary<string, string[]> errors = await response.ShouldBeValidationProblemAsync(CancellationToken);
        Assert.Equal([nameof(CreateCustomerRequest.Email)], errors.Keys);
    }
}
