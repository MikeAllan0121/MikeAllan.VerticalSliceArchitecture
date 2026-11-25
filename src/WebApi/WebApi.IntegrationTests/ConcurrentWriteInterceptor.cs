using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using MikeAllan.VerticalSliceArchitecture.Domain.Database;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests;

/// <summary>
/// Loses a concurrency race on purpose: just before the app's first save that modifies a
/// <typeparamref name="TEntity"/>, commits <paramref name="concurrentWrite"/> to the same row from a separate
/// connection, as another request would have between this one's read and its write.
/// </summary>
/// <remarks>
/// Firing at a chosen moment makes the lost race deterministic, where racing real requests only makes it
/// likely. It fires once, so the next save (a retry) goes through. Add it with
/// <see cref="TestingDatabaseFixture.CreateClient(Microsoft.EntityFrameworkCore.Diagnostics.IInterceptor)"/>.
/// </remarks>
internal sealed class ConcurrentWriteInterceptor<TEntity>(
    string connectionString,
    Func<ApplicationDbContext, TEntity, Task> concurrentWrite)
    : SaveChangesInterceptor
    where TEntity : class
{
    private int _fired;

    /// <summary>Whether the concurrent write has happened.</summary>
    public bool Fired => Volatile.Read(ref _fired) == 1;

    public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        TEntity? modified = eventData.Context?.ChangeTracker.Entries<TEntity>()
            .Where(entry => entry.State == EntityState.Modified)
            .Select(entry => entry.Entity)
            .FirstOrDefault();

        if (modified is not null && Interlocked.Exchange(ref _fired, 1) == 0)
        {
            DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseNpgsql(connectionString)
                .Options;

            await using ApplicationDbContext otherRequest = new(options);
            await concurrentWrite(otherRequest, modified);
        }

        return await base.SavingChangesAsync(eventData, result, cancellationToken);
    }
}
