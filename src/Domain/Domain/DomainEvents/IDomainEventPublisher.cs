namespace MikeAllan.VerticalSliceArchitecture.Domain.DomainEvents;

/// <summary>
/// Runs every registered handler for an event, now.
/// </summary>
public interface IDomainEventPublisher
{
    Task PublishAsync(IDomainEvent domainEvent, CancellationToken ct);
}
