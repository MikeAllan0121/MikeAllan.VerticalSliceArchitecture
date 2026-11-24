using Microsoft.Extensions.DependencyInjection.Extensions;
using MikeAllan.VerticalSliceArchitecture.Domain.DomainEvents;

namespace MikeAllan.VerticalSliceArchitecture.EventualConsistency;

public static class Setup
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddEventualConsistency()
        {
            services.AddHttpContextAccessor();

            // Replace, not Add: the domain library registers an immediate dispatcher by default.
            services.Replace(ServiceDescriptor.Scoped<IDomainEventDispatcher, DeferredDomainEventDispatcher>());

            return services;
        }
    }

    extension(IApplicationBuilder app)
    {
        /// <summary>Called only by the top-level <c>Setup</c>, which owns middleware order.</summary>
        public IApplicationBuilder UseEventualConsistency() => app.UseMiddleware<EventualConsistencyMiddleware>();
    }
}
