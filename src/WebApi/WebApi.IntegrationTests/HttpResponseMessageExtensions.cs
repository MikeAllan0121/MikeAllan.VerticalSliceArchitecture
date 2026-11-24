using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests;

/// <summary>
/// Asserts the error contract: every error is RFC 9457 problem details (<c>application/problem+json</c>).
/// </summary>
internal static class HttpResponseMessageExtensions
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    extension(HttpResponseMessage response)
    {
        /// <summary>
        /// Asserts a problem-details response with <paramref name="expectedStatus"/> and returns it.
        /// </summary>
        public async Task<ProblemDetails> ShouldBeProblemAsync(HttpStatusCode expectedStatus, CancellationToken ct)
        {
            string body = await response.Content.ReadAsStringAsync(ct);

            Assert.True(expectedStatus == response.StatusCode,
                $"Expected {(int)expectedStatus} but got {(int)response.StatusCode}: {body}");
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

            ProblemDetails? problem = JsonSerializer.Deserialize<ProblemDetails>(body, WebJson);
            Assert.NotNull(problem);
            Assert.Equal((int)expectedStatus, problem.Status);
            return problem;
        }

        /// <summary>
        /// Asserts a domain-error problem (404, 409, …) carrying the <c>DomainError</c>'s
        /// <paramref name="expectedCode"/>, e.g. <c>Customer.NotFound</c>.
        /// </summary>
        public async Task<ProblemDetails> ShouldBeProblemAsync(
            HttpStatusCode expectedStatus,
            string expectedCode,
            CancellationToken ct)
        {
            ProblemDetails problem = await response.ShouldBeProblemAsync(expectedStatus, ct);

            Assert.True(problem.Extensions.TryGetValue("code", out object? code), "The problem has no 'code' extension.");
            Assert.Equal(expectedCode, code?.ToString());
            return problem;
        }

        /// <summary>
        /// Asserts a 400 validation problem and returns its <c>errors</c> map, keyed by property path
        /// (<c>Name</c>, <c>Lines[0].Quantity</c>) or route parameter (<c>customerId</c>).
        /// </summary>
        public async Task<IDictionary<string, string[]>> ShouldBeValidationProblemAsync(CancellationToken ct)
        {
            string body = await response.Content.ReadAsStringAsync(ct);

            Assert.True(response.StatusCode == HttpStatusCode.BadRequest,
                $"Expected 400 but got {(int)response.StatusCode}: {body}");
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

            HttpValidationProblemDetails? problem = JsonSerializer.Deserialize<HttpValidationProblemDetails>(body, WebJson);
            Assert.NotNull(problem);
            Assert.NotEmpty(problem.Errors);
            return problem.Errors;
        }
    }
}
