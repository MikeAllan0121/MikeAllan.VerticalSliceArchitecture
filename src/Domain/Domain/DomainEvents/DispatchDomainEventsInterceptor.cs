using Microsoft.EntityFrameworkCore.Diagnostics;
using MikeAllan.VerticalSliceArchitecture.Domain.Aggregates;

namespace MikeAllan.VerticalSliceArchitecture.Domain.DomainEvents;

internal sealed class DispatchDomainEventsInterceptor(IDomainEventDispatcher dispatcher) : SaveChangesInterceptor
{
    // NOTE: There are two options for dispatching domain events:
    // Option 1. Before changes are saved to the database (SavingChanges)
    // Option 2. After changes are saved to the database (SavedChanges)
    //
    // We are using Option 2, for several reasons:
    // - Event handlers can query the DB and expect the data to be up-to-date
    // - Event handlers that fire integration events that affect other systems, will only do
    //   so if the changes are successfully saved to the database
    //
    // The downside of this is that we may have multiple calls to save changes in a single DB request.
    // This means we no longer have a single write to the DB, so may need to wrap the entire operation
    // in a transaction to ensure consistency.
    //
    // Where the popped events go is the dispatcher's call: published now by default, or deferred
    // until after the HTTP response by the WebApi's eventual-consistency dispatcher.
    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        DispatchDomainEvents(eventData.Context, CancellationToken.None).ConfigureAwait(false).GetAwaiter().GetResult();
        return base.SavedChanges(eventData, result);
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData,
        int result,
        CancellationToken cancellationToken = default)
    {
        await DispatchDomainEvents(eventData.Context, cancellationToken);
        return await base.SavedChangesAsync(eventData, result, cancellationToken);
    }

    private async Task DispatchDomainEvents(DbContext? context, CancellationToken ct)
    {
        if (context is null)
            return;

        List<IDomainEvent> domainEvents = [.. context.ChangeTracker.Entries<IAggregateRoot>()
            .SelectMany(entry => entry.Entity.PopDomainEvents())];

        if (domainEvents.Count is 0)
            return;

        await dispatcher.DispatchAsync(domainEvents, ct);
    }
}
