using MikeAllan.VerticalSliceArchitecture.Domain.DomainEvents;

namespace MikeAllan.VerticalSliceArchitecture.EventualConsistency;

/// <summary>
/// Publishes the domain events <see cref="DeferredDomainEventDispatcher"/> queued during the request,
/// once the response has been sent, so the caller does not wait on side effects.
/// </summary>
/// <remarks>
/// A handler that fails does not stop the events behind it: the failure is logged (see
/// <see cref="EventualConsistencyMiddlewareLog"/>) and publishing carries on. There is no wrapping
/// transaction: handlers write through their own scope and <c>DbContext</c> (see
/// <see cref="IDomainEventHandler{TEvent}"/>), so each handler's changes commit on their own and a
/// transaction on the request's context would cover nothing. Failing fast would only drop the remaining
/// events, which are independent of the one that failed, while rolling nothing back. The response has
/// already been sent, so there is no caller to report to: the log is the only place a failure surfaces.
/// </remarks>
public sealed class EventualConsistencyMiddleware(RequestDelegate next, ILogger<EventualConsistencyMiddleware> logger)
{
    internal const string DomainEventsKey = "DomainEventsKey";

    public async Task InvokeAsync(HttpContext context, IDomainEventPublisher publisher)
    {
        context.Response.OnCompleted(async () => await PublishEvents(context, publisher));

        await next(context);
    }

    private async Task PublishEvents(HttpContext context, IDomainEventPublisher publisher)
    {
        if (!context.Items.TryGetValue(DomainEventsKey, out object? value) ||
            value is not Queue<IDomainEvent> domainEvents)
            return;

        while (domainEvents.TryDequeue(out IDomainEvent? nextEvent))
        {
            try
            {
                await publisher.PublishAsync(nextEvent, CancellationToken.None);
            }
            catch (EventualConsistencyException ex)
            {
                logger.EventualConsistencyFailed(
                    nextEvent.GetType().Name,
                    ex.Error.Code,
                    ex.Error.Description);
            }
#pragma warning disable CA1031 // Nothing can handle it: the response is sent, so log it and publish the rest.
            catch (Exception ex)
#pragma warning restore CA1031
            {
                logger.DomainEventFailed(ex, nextEvent.GetType().Name);
            }
        }
    }
}

internal static partial class EventualConsistencyMiddlewareLog
{
    [LoggerMessage(LogLevel.Warning,
        "Domain event {DomainEventType} could not be fully handled: {ErrorCode}: {ErrorDescription}")]
    public static partial void EventualConsistencyFailed(
        this ILogger logger, string domainEventType, string errorCode, string errorDescription);

    [LoggerMessage(LogLevel.Error, "Domain event {DomainEventType} failed with an unexpected error")]
    public static partial void DomainEventFailed(this ILogger logger, Exception exception, string domainEventType);
}
