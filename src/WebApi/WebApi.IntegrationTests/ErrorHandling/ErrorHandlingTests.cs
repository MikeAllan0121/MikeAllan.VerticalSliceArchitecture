using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests.ErrorHandling;

/// <summary>
/// Requests that never reach a handler still answer with problem details, not a 500 or an empty body.
/// Validation and domain errors are covered in each slice's tests.
/// </summary>
public sealed class ErrorHandlingTests(TestingDatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    [Fact]
    public async Task MalformedJson_ShouldReturnBadRequestProblem()
    {
        // Arrange
        using StringContent content = new("{ \"name\": ", Encoding.UTF8, "application/json");

        // Act
        HttpResponseMessage response = await Client.PostAsync("/api/customers", content, CancellationToken);

        // Assert
        ProblemDetails problem = await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, CancellationToken);
        Assert.Equal("POST /api/customers", problem.Instance);
    }

    [Fact]
    public async Task NonGuidRouteValue_ShouldReturnBadRequestProblem()
    {
        // Act
        HttpResponseMessage response = await Client.GetAsync("/api/customers/not-a-guid", CancellationToken);

        // Assert
        await response.ShouldBeProblemAsync(HttpStatusCode.BadRequest, CancellationToken);
    }

    [Fact]
    public async Task Problems_ShouldCarryRequestAndTraceIds()
    {
        // Act
        HttpResponseMessage response = await Client.GetAsync($"/api/customers/{Guid.CreateVersion7()}", CancellationToken);

        // Assert
        ProblemDetails problem = await response.ShouldBeProblemAsync(HttpStatusCode.NotFound, CancellationToken);
        Assert.Equal($"GET {response.RequestMessage!.RequestUri!.AbsolutePath}", problem.Instance);
        Assert.True(problem.Extensions.ContainsKey("requestId"), "The problem has no 'requestId'.");
        Assert.True(problem.Extensions.ContainsKey("traceId"), "The problem has no 'traceId'.");
    }
}
