using Microsoft.Extensions.DependencyInjection;

namespace MikeAllan.VerticalSliceArchitecture.Domain.DomainEvents;

/// <remarks>
/// Resolves handlers at publish time rather than through the constructor: the publisher sits under
/// <c>ApplicationDbContext</c>'s interceptors, so a handler injected eagerly that itself depends on the
/// context would make a dependency cycle.
/// </remarks>
internal sealed class DomainEventPublisher(IServiceProvider serviceProvider) : IDomainEventPublisher
{
    public async Task PublishAsync(IDomainEvent domainEvent, CancellationToken ct)
    {
        foreach (IDomainEventHandler handler in serviceProvider.GetServices<IDomainEventHandler>())
            await handler.HandleAsync(domainEvent, ct);
    }
}
