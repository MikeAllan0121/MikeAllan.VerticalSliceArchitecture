using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using MikeAllan.VerticalSliceArchitecture.Domain.Database;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests;

/// <summary>
/// One database container and one in-memory app, shared by every integration test. The database is
/// emptied before each test (see <see cref="IntegrationTestBase"/>).
/// </summary>
public sealed class TestingDatabaseFixture : IAsyncLifetime
{
    private readonly TestDatabase _database = new();
    private WebApiTestFactory _factory = null!;

    public async ValueTask InitializeAsync()
    {
        await _database.InitializeAsync();
        _factory = new WebApiTestFactory(_database.ConnectionString);
    }

    public Task ResetDatabaseAsync() => _database.ResetAsync();

    // NOTE: If you need an authenticated client, add a method that authenticates, sets the headers and
    // returns the client. For an example see https://github.com/SSWConsulting/Northwind365
    public HttpClient CreateClient() => _factory.CreateClient();

    /// <summary>A client for the same app with extra test configuration, such as replaced services.</summary>
    public HttpClient CreateClient(Action<IWebHostBuilder> configure) =>
        _factory.WithWebHostBuilder(configure).CreateClient();

    /// <summary>A client for the same app whose <see cref="ApplicationDbContext"/> also runs <paramref name="interceptor"/>.</summary>
    public HttpClient CreateClient(IInterceptor interceptor) =>
        CreateClient(builder => builder.ConfigureTestServices(services =>
            services.ConfigureDbContext<ApplicationDbContext>(options => options.AddInterceptors(interceptor))));

    /// <summary>The test database, for a test that writes to it from outside the app.</summary>
    public string ConnectionString => _database.ConnectionString;

    public IServiceScope CreateScope() => _factory.Services.CreateScope();

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _database.DisposeAsync();
    }
}

[CollectionDefinition]
public sealed class TestingDatabaseFixtureCollection : ICollectionFixture<TestingDatabaseFixture>;
