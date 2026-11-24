namespace MikeAllan.VerticalSliceArchitecture.Domain.DomainEvents;

/// <summary>
/// Marker for something that happened in the domain that other parts of the system may react to.
/// Raised by an aggregate (<c>AddDomainEvent</c>) and dispatched after the aggregate is saved.
/// </summary>
public interface IDomainEvent;
