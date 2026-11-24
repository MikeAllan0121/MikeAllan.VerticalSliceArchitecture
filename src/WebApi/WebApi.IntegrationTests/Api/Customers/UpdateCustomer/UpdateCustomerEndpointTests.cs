using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.Errors;
using MikeAllan.VerticalSliceArchitecture.Api.Customers.UpdateCustomer;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests.Api.Customers.UpdateCustomer;

public sealed class UpdateCustomerEndpointTests(TestingDatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    private static string Route(Guid customerId) => $"/api/customers/{customerId}";

    [Fact]
    public async Task Put_ShouldUpdateCustomer()
    {
        // Arrange
        Customer customer = CustomerFactory.Generate();
        await AddAsync(customer);
        UpdateCustomerRequest request = new("Fabrikam", "billing@fabrikam.com");

        // Act
        HttpResponseMessage response = await Client.PutAsJsonAsync(Route(customer.Id.Value), request, CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        Customer item = await GetQueryable<Customer>().SingleAsync(c => c.Id == customer.Id, CancellationToken);
        Assert.Equal(request.Name, item.Name);
        Assert.Equal(request.Email, item.Email);
        Assert.NotNull(item.UpdatedAt);
        Assert.InRange(item.UpdatedAt.Value, DateTimeOffset.UtcNow.AddSeconds(-10), DateTimeOffset.UtcNow.AddSeconds(10));
    }

    [Fact]
    public async Task Put_WhenCustomerDoesNotExist_ShouldReturnNotFoundProblem()
    {
        // Arrange
        CustomerId customerId = CustomerId.From(Guid.CreateVersion7());
        UpdateCustomerRequest request = new("Fabrikam", "billing@fabrikam.com");

        // Act
        HttpResponseMessage response = await Client.PutAsJsonAsync(Route(customerId.Value), request, CancellationToken);

        // Assert
        ProblemDetails problem = await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, "Customer.NotFound", CancellationToken);
        Assert.Equal(CustomerErrors.NotFound.Description, problem.Title);
        Assert.False(await GetQueryable<Customer>().AnyAsync(c => c.Id == customerId, CancellationToken));
    }

    [Fact]
    public async Task Put_WithEmptyCustomerId_ShouldReturnValidationProblemForTheRouteParameter()
    {
        // Arrange
        UpdateCustomerRequest request = new("Fabrikam", "billing@fabrikam.com");

        // Act
        HttpResponseMessage response = await Client.PutAsJsonAsync(Route(Guid.Empty), request, CancellationToken);

        // Assert
        IDictionary<string, string[]> errors = await response.ShouldBeValidationProblemAsync(CancellationToken);
        Assert.Equal(["customerId"], errors.Keys);
    }

    [Fact]
    public async Task Put_WithInvalidEmail_ShouldReturnValidationProblemAndLeaveCustomerUnchanged()
    {
        // Arrange
        Customer customer = CustomerFactory.Generate();
        await AddAsync(customer);
        UpdateCustomerRequest request = new(customer.Name, "not-an-email");

        // Act
        HttpResponseMessage response = await Client.PutAsJsonAsync(Route(customer.Id.Value), request, CancellationToken);

        // Assert
        IDictionary<string, string[]> errors = await response.ShouldBeValidationProblemAsync(CancellationToken);
        Assert.Equal([nameof(UpdateCustomerRequest.Email)], errors.Keys);

        Customer item = await GetQueryable<Customer>().SingleAsync(c => c.Id == customer.Id, CancellationToken);
        Assert.Equal(customer.Email, item.Email);
    }
}
