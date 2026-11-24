using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace MikeAllan.VerticalSliceArchitecture.Domain.DomainEvents;

public static class Setup
{
    extension(IServiceCollection services)
    {
        /// <summary>
        /// Registers one handler for one event type. Call it from the <c>Setup.cs</c> of the feature that
        /// owns the handler; nothing scans for handlers.
        /// </summary>
        public IServiceCollection AddDomainEventHandler<TEvent, THandler>()
            where TEvent : IDomainEvent
            where THandler : class, IDomainEventHandler<TEvent> =>
            services.AddScoped<IDomainEventHandler, THandler>();

        internal IServiceCollection AddDomainEvents()
        {
            services.AddScoped<IDomainEventPublisher, DomainEventPublisher>();

            // TryAdd: a host that replaced the dispatcher before this ran keeps its own.
            services.TryAddScoped<IDomainEventDispatcher, ImmediateDomainEventDispatcher>();
            services.AddScoped<DispatchDomainEventsInterceptor>();

            return services;
        }
    }
}
