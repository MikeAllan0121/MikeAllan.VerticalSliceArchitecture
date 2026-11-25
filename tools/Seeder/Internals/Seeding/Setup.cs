using Microsoft.Extensions.DependencyInjection;

namespace MikeAllan.VerticalSliceArchitecture.Seeder.Internals.Seeding;

internal static class Setup
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddSeeding()
        {
            services.AddHostedService<Worker>();
            services.AddScoped<DatabaseSeeder>();

            services
                .AddOpenTelemetry()
                .WithTracing(tracing => tracing.AddSource(Worker.ActivitySourceName));

            return services;
        }
    }
}
