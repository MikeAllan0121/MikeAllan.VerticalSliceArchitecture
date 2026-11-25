using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace MikeAllan.VerticalSliceArchitecture.Seeder.Internals.Seeding;

/// <summary>Seeds the database once, then stops the host so the AppHost sees the resource complete.</summary>
internal sealed class Worker(
    IServiceProvider serviceProvider,
    IHostApplicationLifetime hostApplicationLifetime,
    ILogger<Worker> logger) : BackgroundService
{
    public const string ActivitySourceName = "Seeding";
    private static readonly ActivitySource ActivitySource = new(ActivitySourceName);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using Activity? activity = ActivitySource.StartActivity("Seeding database", ActivityKind.Client);

        try
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            using IServiceScope scope = serviceProvider.CreateScope();

            // No environment check: the AppHost only adds this resource in run mode, so it
            // can never reach a deployed environment. The schema is already in place — the
            // "migrations" resource runs to completion before this project starts.
            DatabaseSeeder seeder = scope.ServiceProvider.GetRequiredService<DatabaseSeeder>();
            await seeder.SeedAsync(stoppingToken);

            stopwatch.Stop();
            logger.DatabaseSeeded(stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            activity?.AddException(ex);
            throw;
        }

        hostApplicationLifetime.StopApplication();
    }
}

// Compile-time logging: the source generator emits a level-checked, allocation-free
// method, which also satisfies CA1873 (the generated call is not an ILogger.Log* shape).
internal static partial class WorkerLog
{
    [LoggerMessage(LogLevel.Information, "DB seeding took {ElapsedTime}")]
    public static partial void DatabaseSeeded(this ILogger logger, TimeSpan elapsedTime);
}
