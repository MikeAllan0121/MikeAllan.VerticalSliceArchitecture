namespace MikeAllan.VerticalSliceArchitecture.Domain.DomainEvents;

internal sealed class ImmediateDomainEventDispatcher(IDomainEventPublisher publisher) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken ct)
    {
        foreach (IDomainEvent domainEvent in domainEvents)
            await publisher.PublishAsync(domainEvent, ct);
    }
}
