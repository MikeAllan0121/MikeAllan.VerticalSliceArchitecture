using Scalar.AspNetCore;

namespace MikeAllan.VerticalSliceArchitecture.OpenApi;

/// <summary>
/// The OpenAPI document (<c>/openapi/v1.json</c>) and the Scalar UI that renders it (<c>/scalar</c>).
/// </summary>
/// <remarks>
/// Mapped in every environment, the same exposure the previous Swagger UI had. Restrict it to
/// Development here if the API should not describe itself in production.
/// </remarks>
public static class Setup
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddApiDocs()
        {
            services.AddOpenApi();

            return services;
        }
    }

    extension(IEndpointRouteBuilder app)
    {
        public IEndpointRouteBuilder MapApiDocs()
        {
            app.MapOpenApi();
            app.MapScalarApiReference();

            return app;
        }
    }
}
