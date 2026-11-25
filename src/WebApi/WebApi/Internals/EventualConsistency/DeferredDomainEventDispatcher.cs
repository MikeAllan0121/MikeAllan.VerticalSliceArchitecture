using MikeAllan.VerticalSliceArchitecture.Domain.DomainEvents;

namespace MikeAllan.VerticalSliceArchitecture.Internals.EventualConsistency;

/// <summary>
/// The WebApi's replacement for the domain library's immediate dispatcher. During an HTTP request the
/// user is waiting online, so events are queued on the request and published by
/// <see cref="EventualConsistencyMiddleware"/> after the response. With no request in flight they are
/// published immediately.
/// </summary>
internal sealed class DeferredDomainEventDispatcher(
    IHttpContextAccessor httpContextAccessor,
    IDomainEventPublisher publisher) : IDomainEventDispatcher
{
    public async Task DispatchAsync(IReadOnlyList<IDomainEvent> domainEvents, CancellationToken ct)
    {
        HttpContext? httpContext = httpContextAccessor.HttpContext;

        if (httpContext is null)
        {
            foreach (IDomainEvent domainEvent in domainEvents)
                await publisher.PublishAsync(domainEvent, ct);

            return;
        }

        Queue<IDomainEvent> queue =
            httpContext.Items.TryGetValue(EventualConsistencyMiddleware.DomainEventsKey, out object? value) &&
            value is Queue<IDomainEvent> existing
                ? existing
                : new Queue<IDomainEvent>();

        foreach (IDomainEvent domainEvent in domainEvents)
            queue.Enqueue(domainEvent);

        httpContext.Items[EventualConsistencyMiddleware.DomainEventsKey] = queue;
    }
}
