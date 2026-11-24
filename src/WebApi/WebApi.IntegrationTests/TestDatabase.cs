using Microsoft.EntityFrameworkCore;
using Npgsql;
using Respawn;
using MikeAllan.VerticalSliceArchitecture.Domain.Database;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests;

/// <summary>
/// Owns the schema and data of the test database: migrated once, emptied by Respawn before each test.
/// </summary>
internal sealed class TestDatabase : IAsyncDisposable
{
    private const string DatabaseName = "WebApi-IntegrationTests";

    private readonly PostgresContainer _postgres = new();
    private Respawner _respawner = null!;

    public string ConnectionString { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.InitializeAsync();

        NpgsqlConnectionStringBuilder builder = _postgres.CreateConnectionStringBuilder();
        builder.Database = DatabaseName;
        ConnectionString = builder.ConnectionString;

        // The real migrations, not EnsureCreated: a migration that doesn't match the model fails here.
        // MigrateAsync creates the database first, since it doesn't exist yet.
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        await using (ApplicationDbContext dbContext = new(options))
            await dbContext.Database.MigrateAsync();

        await using NpgsqlConnection connection = new(ConnectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = ["public"],
                TablesToIgnore = ["__EFMigrationsHistory"],
            });
    }

    public async Task ResetAsync()
    {
        await using NpgsqlConnection connection = new(ConnectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    public ValueTask DisposeAsync() => _postgres.DisposeAsync();
}
