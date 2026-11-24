namespace MikeAllan.VerticalSliceArchitecture.Domain.DomainEvents;

/// <summary>
/// Decides when the events popped off saved aggregates are published.
/// </summary>
/// <remarks>
/// The default publishes immediately, which is right for a tool like the Seeder. A host can replace it:
/// the WebApi defers events raised during an HTTP request until after the response (eventual consistency).
/// Two real implementations, which is why this is an interface.
/// </remarks>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken ct);
}
