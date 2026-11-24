namespace MikeAllan.VerticalSliceArchitecture.Domain.DomainEvents;

/// <summary>
/// Handles one type of domain event. Register it with
/// <c>services.AddDomainEventHandler&lt;TEvent, THandler&gt;()</c> in the owning feature's <c>Setup.cs</c>.
/// </summary>
/// <remarks>
/// Handlers run after the change that raised the event was saved. A handler that writes to the database
/// should open its own scope (<c>IServiceScopeFactory</c>) rather than inject the caller's
/// <c>ApplicationDbContext</c>: outside an HTTP request it runs inside that context's
/// <c>SaveChanges</c>, so reusing the context would re-enter it.
/// </remarks>
public interface IDomainEventHandler<in TEvent> : IDomainEventHandler
    where TEvent : IDomainEvent
{
    Task HandleAsync(TEvent domainEvent, CancellationToken ct);

    Task IDomainEventHandler.HandleAsync(IDomainEvent domainEvent, CancellationToken ct) =>
        domainEvent is TEvent typed ? HandleAsync(typed, ct) : Task.CompletedTask;
}

/// <summary>
/// The untyped view of <see cref="IDomainEventHandler{TEvent}"/> that the publisher resolves, so it can
/// dispatch an event known only as <see cref="IDomainEvent"/> without reflection. Implement the generic
/// interface, not this one.
/// </summary>
public interface IDomainEventHandler
{
    Task HandleAsync(IDomainEvent domainEvent, CancellationToken ct);
}
