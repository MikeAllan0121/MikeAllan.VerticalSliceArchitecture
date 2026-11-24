using Npgsql;
using Testcontainers.PostgreSql;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests;

/// <summary>
/// The throwaway PostgreSQL container the integration tests run against.
/// </summary>
internal sealed class PostgresContainer : IAsyncDisposable
{
    private const int MaxRetries = 5;

    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(5);

    // The image Aspire runs locally (Aspire.Hosting.PostgreSQL 17.0.0), so the tests and the app agree.
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18.6")
        .WithName($"WebApi-IntegrationTests-{Guid.NewGuid()}")
        .WithPortBinding(5432, true)
        .WithAutoRemove(true)
        .Build();

    public string ConnectionString => _container.GetConnectionString();

    public async Task InitializeAsync()
    {
        // The container sometimes fails to start first time; retry rather than fail the whole run. The last
        // attempt's exception escapes, so a container that never starts still fails the fixture.
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                await _container.StartAsync();
                return;
            }
            catch (InvalidOperationException) when (attempt < MaxRetries)
            {
                await Task.Delay(RetryDelay);
            }
        }
    }

    public NpgsqlConnectionStringBuilder CreateConnectionStringBuilder() => new(ConnectionString);

    public async ValueTask DisposeAsync()
    {
        await _container.StopAsync();
        await _container.DisposeAsync();
    }
}
