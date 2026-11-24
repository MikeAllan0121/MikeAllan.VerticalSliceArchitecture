using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MikeAllan.VerticalSliceArchitecture.Domain.Auditing;
using MikeAllan.VerticalSliceArchitecture.Domain.DomainEvents;

namespace MikeAllan.VerticalSliceArchitecture.Domain.Database;

internal static class Setup
{
    /// <summary>The Aspire connection name: <c>ConnectionStrings:AppDb</c>.</summary>
    private const string ConnectionName = "AppDb";

    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddDatabase()
        {
            // Aspire's documented pattern for a context that needs services from DI: register it with
            // AddDbContext, whose callback is handed the scoped provider, then let Aspire add its
            // retries, health check and telemetry. AddAzureNpgsqlDbContext gives no provider, which is
            // what would push this into BuildServiceProvider() (ASP0000).
            builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
            {
                // A missing connection string is left for EF to report on first use, so design-time
                // tools (dotnet ef) can build the model without a database.
                options.UseNpgsql(builder.Configuration.GetConnectionString(ConnectionName));

                options.AddInterceptors(
                    serviceProvider.GetRequiredService<EntitySaveChangesInterceptor>(),
                    serviceProvider.GetRequiredService<DispatchDomainEventsInterceptor>());
            });

            // The Azure variant also adds the Entra ID token provider: on Azure the connection string has
            // no username or password, so it reads the username from a token for the app's managed
            // identity and gives each new connection a short-lived token as its password. Locally the
            // container's connection string carries both, and the token provider stays out of the way.
            //
            // Skipped under the design-time tools (dotnet ef): there is no connection string there, so
            // the enricher would ask Azure for a token just to build the model, and fail without an
            // Azure sign-in. The tools need none of what it adds.
            if (!EF.IsDesignTime)
                builder.EnrichAzureNpgsqlDbContext<ApplicationDbContext>();

            return builder;
        }
    }
}
