using AppHost;
using AppHost.Commands;
using Azure.Provisioning;
using Azure.Provisioning.AppService;

var builder = DistributedApplication.CreateBuilder(args);

// Configure the Azure App Service environment
builder.AddAzureAppServiceEnvironment("plan").ConfigureInfrastructure(infra =>
{
    var plan = infra.GetProvisionableResources()
        .OfType<AppServicePlan>()
        .Single();

    plan.Sku = new AppServiceSkuDescription
    {
        Name = "B1", // Basic tier, 1 core
    };
});

// Azure Database for PostgreSQL Flexible Server on Azure, with Microsoft Entra ID (managed identity)
// authentication and no passwords. Locally it runs as a PostgreSQL container instead.
// Versions differ: the container is Aspire's default postgres image (18.x), while Azure gets PostgreSQL 16,
// the newest version Azure.Provisioning.PostgreSql offers (PostgreSqlFlexibleServerVersion). Raise it
// with ConfigureInfrastructure once a newer value ships.
var postgres = builder
    .AddAzurePostgresFlexibleServer("postgres")
    .RunAsContainer(container =>
    {
        // Keep the container between runs. There is no data volume: the data is seed data, so a
        // removed container loses nothing that the next run doesn't recreate.
        container.WithLifetime(ContainerLifetime.Persistent);

        // Group under one "mikeallan-vsa" project in Docker Desktop / OrbStack (cosmetic only)
        container.InDockerProject();

        // If desired, set the PostgreSQL port to a constant value
        //container.WithHostPort(5432);
    });

var db = postgres
    .AddDatabase("AppDb", "app-db")
    .WithDropDatabaseCommand();

// Projects are registered by path, relative to this AppHost's folder. AddDotnetProject replaced
// AddProject<Projects.T>() in Aspire 17, so there are no generated Projects.* types.
var api = builder
    .AddDotnetProject("api", "../../src/WebApi/WebApi/WebApi.csproj")
    .WithExternalHttpEndpoints()
    .WithReference(db);

// The DbContext and its migrations live in the Domain class library (Database/Migrations);
// the WebApi stays the startup project, since its host is what configures the context.
// On publish this emits a migration bundle under efmigrations/ rather than running anything —
// applying it is a deployment step (see the README).
var migrationsProject = Path.GetFullPath(
    Path.Combine(builder.AppHostDirectory, "..", "..", "src", "Domain", "Domain", "Domain.csproj"));

var migrations = api.AddEFMigrations("migrations")
    .WithMigrationsProject(migrationsProject)
    .WithReference(db)
    .WaitFor(postgres)
    .RunDatabaseUpdateOnStart()
    .PublishAsMigrationBundle();

if (builder.ExecutionContext.IsRunMode)
{
    // Seeding is dev-only, and this guard is what enforces it — the resource is never
    // added in publish mode, so Bogus data has no path to a deployed environment.
    var seeder = builder.AddDotnetProject("seeder", "../Seeder/Seeder.csproj")
        .WithReference(db)
        .WaitForCompletion(migrations);

    api.WaitForCompletion(seeder);
}
else
{
    api.WaitForCompletion(migrations);
}

// Configure Application Insights and Log Analytics only if in publish mode
// When running locally, use Aspire Dashboard instead
if (builder.ExecutionContext.IsPublishMode)
{
    var logAnalytics = builder.AddAzureLogAnalyticsWorkspace("log-analytics");
    var insights = builder.AddAzureApplicationInsights("insights", logAnalytics);
    api.WithReference(insights);
}

builder.Build().Run();