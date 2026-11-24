using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace MikeAllan.VerticalSliceArchitecture.Domain.Auditing;

internal sealed class EntitySaveChangesInterceptor(ICurrentUserService currentUserService, TimeProvider timeProvider)
    : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        UpdateEntities(eventData.Context);

        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData,
        InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        UpdateEntities(eventData.Context);

        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateEntities(DbContext? context)
    {
        if (context is null)
            return;

        foreach (EntityEntry<IAuditable> entry in context.ChangeTracker.Entries<IAuditable>())
        {
            if (entry.State is EntityState.Added)
            {
                entry.Entity.SetCreated(timeProvider, currentUserService.UserId);
            }
            else if (entry.State is EntityState.Added or EntityState.Modified ||
                     entry.HasChangedOwnedEntities)
            {
                entry.Entity.SetUpdated(timeProvider, currentUserService.UserId);
            }
        }
    }
}

internal static class EntityEntryExtensions
{
    extension(EntityEntry entry)
    {
        public bool HasChangedOwnedEntities =>
            entry.References.Any(r =>
                r.TargetEntry != null &&
                r.TargetEntry.Metadata.IsOwned() &&
                r.TargetEntry.State is EntityState.Added or EntityState.Modified);
    }
}
