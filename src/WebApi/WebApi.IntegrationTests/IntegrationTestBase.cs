using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using MikeAllan.VerticalSliceArchitecture.Domain.Database;
using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests;

/// <summary>
/// Integration tests inherit this: a plain <see cref="HttpClient"/> against the real routes, and an
/// <see cref="ApplicationDbContext"/> to seed data and read back what was persisted.
/// </summary>
[Collection<TestingDatabaseFixtureCollection>]
public abstract class IntegrationTestBase : IAsyncLifetime
{
    /// <summary>How the API serialises: camelCase, case-insensitive on read, enums as their names.</summary>
    protected static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly TestingDatabaseFixture _fixture;
    private readonly IServiceScope _scope;
    private readonly ApplicationDbContext _dbContext;

    protected IntegrationTestBase(TestingDatabaseFixture fixture)
    {
        _fixture = fixture;
        _scope = fixture.CreateScope();
        _dbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        Client = fixture.CreateClient();
    }

    /// <summary>An anonymous client for the in-memory app.</summary>
    protected HttpClient Client { get; }

    protected static CancellationToken CancellationToken => TestContext.Current.CancellationToken;

    public async ValueTask InitializeAsync() => await _fixture.ResetDatabaseAsync();

    /// <summary>An untracked read straight from the database: what was persisted, not what is cached.</summary>
    protected IQueryable<T> GetQueryable<T>() where T : class => _dbContext.Set<T>().AsNoTracking();

    protected async Task AddAsync<TEntity>(TEntity entity)
        where TEntity : class
    {
        await _dbContext.AddAsync(entity, CancellationToken);
        await _dbContext.SaveChangesAsync(CancellationToken);
    }

    protected async Task AddRangeAsync<TEntity>(IEnumerable<TEntity> entities)
        where TEntity : class
    {
        await _dbContext.AddRangeAsync(entities, CancellationToken);
        await _dbContext.SaveChangesAsync(CancellationToken);
    }

    /// <summary>
    /// GETs a paged list endpoint by raw URL and deserialises the standard paged envelope.
    /// </summary>
    /// <remarks>
    /// Takes a URL rather than a request object on purpose: what a paged endpoint's tests pin down is the
    /// query-string contract (the parameter names and how out-of-range values are treated).
    /// </remarks>
    protected async Task<PagedList<T>> GetPage<T>(string url)
    {
        HttpResponseMessage response = await Client.GetAsync(url, CancellationToken);
        string body = await response.Content.ReadAsStringAsync(CancellationToken);

        // Body in the failure message: a paged endpoint's failures are 400s carrying the reason.
        Assert.True(response.IsSuccessStatusCode, $"GET {url} returned {(int)response.StatusCode}: {body}");

        PagedList<T>? page = JsonSerializer.Deserialize<PagedList<T>>(body, WebJson);
        Assert.NotNull(page);
        return page;
    }

    protected async Task<T> ReadJsonAsync<T>(HttpResponseMessage response)
    {
        T? body = await response.Content.ReadFromJsonAsync<T>(WebJson, CancellationToken);
        Assert.NotNull(body);
        return body;
    }

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        _scope.Dispose();
        GC.SuppressFinalize(this);
        return ValueTask.CompletedTask;
    }
}
