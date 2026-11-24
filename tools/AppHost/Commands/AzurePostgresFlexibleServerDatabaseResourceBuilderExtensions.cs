using Aspire.Hosting.Azure;
using Microsoft.EntityFrameworkCore;

namespace AppHost.Commands;

/// <summary>
/// A "Drop Database" command on the database resource in the Aspire dashboard.
/// </summary>
public static class AzurePostgresFlexibleServerDatabaseResourceBuilderExtensions
{
    extension(IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource> builder)
    {
        public IResourceBuilder<AzurePostgresFlexibleServerDatabaseResource> WithDropDatabaseCommand()
        {
            builder.WithCommand(
                "drop-database",
                "Drop Database",
                async _ =>
                {
                    string? connectionString = await builder.Resource.ConnectionStringExpression.GetValueAsync(CancellationToken.None);
                    ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

                    // Npgsql drops the database WITH (FORCE) on PostgreSQL 13+, so open connections
                    // (the running API's pool) don't block it.
                    DbContextOptionsBuilder<DbContext> optionsBuilder = new();
                    optionsBuilder.UseNpgsql(connectionString);
                    await using DbContext db = new(optionsBuilder.Options);
                    await db.Database.EnsureDeletedAsync();

                    return CommandResults.Success();
                },
                null); // Intentionally using 'null' for the command state resolver as this command does not require health status checks. Downstream code is expected to handle 'null' appropriately.

            return builder;
        }
    }
}
