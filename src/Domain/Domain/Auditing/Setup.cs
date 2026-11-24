using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MikeAllan.VerticalSliceArchitecture.Domain.Auditing;

internal static class Setup
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddAuditing()
        {
            services.TryAddSingleton(TimeProvider.System);
            services.AddScoped<EntitySaveChangesInterceptor>();

            return services;
        }
    }
}
