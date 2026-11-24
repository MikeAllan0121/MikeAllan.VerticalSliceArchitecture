using MikeAllan.VerticalSliceArchitecture.Domain.DomainEvents;

namespace MikeAllan.VerticalSliceArchitecture.Domain.Aggregates;

public interface IAggregateRoot
{
    void AddDomainEvent(IDomainEvent domainEvent);

    IReadOnlyList<IDomainEvent> PopDomainEvents();
}
