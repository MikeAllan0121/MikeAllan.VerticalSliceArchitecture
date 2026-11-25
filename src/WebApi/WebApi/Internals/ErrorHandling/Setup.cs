using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;

namespace MikeAllan.VerticalSliceArchitecture.Internals.ErrorHandling;

/// <summary>
/// Every error the API returns is RFC 9457 problem details: unhandled exceptions, request validation
/// failures and domain errors (see <see cref="DomainErrorExtensions"/>) all go through the same enrichment.
/// </summary>
internal static class Setup
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddErrorHandling()
        {
            services.AddProblemDetails(options =>
            {
                options.CustomizeProblemDetails = context =>
                {
                    context.ProblemDetails.Instance = $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";

                    context.ProblemDetails.Extensions.TryAdd("requestId", context.HttpContext.TraceIdentifier);

                    Activity? activity = context.HttpContext.Features.Get<IHttpActivityFeature>()?.Activity;
                    context.ProblemDetails.Extensions.TryAdd("traceId", activity?.Id);
                };
            });

            // A lost optimistic-concurrency race is a 409 Concurrency.Conflict problem, not a 500.
            services.AddExceptionHandler<ConcurrencyConflictExceptionHandler>();

            // A request that fails to bind (malformed JSON, a route value that isn't a GUID) throws
            // BadHttpRequestException in Development; keep its 400 rather than reporting a 500.
            services.Configure<ExceptionHandlerOptions>(options =>
                options.StatusCodeSelector = exception =>
                    exception is BadHttpRequestException badRequest
                        ? badRequest.StatusCode
                        : StatusCodes.Status500InternalServerError);

            return services;
        }
    }

    extension(IApplicationBuilder app)
    {
        /// <summary>Called only by the top-level <c>Setup</c>, which owns middleware order.</summary>
        public IApplicationBuilder UseErrorHandling() => app.UseExceptionHandler();
    }
}
