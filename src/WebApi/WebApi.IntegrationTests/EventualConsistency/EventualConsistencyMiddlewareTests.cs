using System.Net;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.DomainEvents;
using MikeAllan.VerticalSliceArchitecture.Domain.Errors;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.EventualConsistency;
using static MikeAllan.VerticalSliceArchitecture.IntegrationTests.EventualConsistency.CapturingLoggerProvider;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests.EventualConsistency;

/// <summary>
/// A domain event handler runs after the response is sent, so when it fails nobody is waiting to be told.
/// The middleware has to log it.
/// </summary>
public sealed class EventualConsistencyMiddlewareTests(TestingDatabaseFixture fixture) : IntegrationTestBase(fixture)
{
    private readonly TestingDatabaseFixture _fixture = fixture;

    [Fact]
    public async Task HandlerThrowingEventualConsistencyException_ShouldLogEventTypeAndErrorCode()
    {
        // Arrange
        DomainError error = new ConflictError("Test.Failed", "The test handler failed");
        CapturingLoggerProvider logs = new();
        using HttpClient client = CreateClientWithHandler(logs, new ThrowingHandler(new EventualConsistencyException(error)));
        Invoice invoice = await AddDraftInvoiceAsync();

        // Act
        HttpResponseMessage response = await IssueInvoiceAsync(client, invoice);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        LogEntry entry = await WaitForEntryAsync(logs, LogLevel.Warning);
        Assert.Equal(nameof(InvoiceAmountDueChangedEvent), entry.Values["DomainEventType"]);
        Assert.Equal("Test.Failed", entry.Values["ErrorCode"]);
        Assert.Equal("The test handler failed", entry.Values["ErrorDescription"]);
    }

    [Fact]
    public async Task HandlerThrowingAnyOtherException_ShouldLogAnErrorWithTheException()
    {
        // Arrange
        InvalidOperationException failure = new("boom");
        CapturingLoggerProvider logs = new();
        using HttpClient client = CreateClientWithHandler(logs, new ThrowingHandler(failure));
        Invoice invoice = await AddDraftInvoiceAsync();

        // Act
        HttpResponseMessage response = await IssueInvoiceAsync(client, invoice);

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        LogEntry entry = await WaitForEntryAsync(logs, LogLevel.Error);
        Assert.Equal(nameof(InvoiceAmountDueChangedEvent), entry.Values["DomainEventType"]);
        Assert.Same(failure, entry.Exception);
    }

    private HttpClient CreateClientWithHandler(CapturingLoggerProvider logs, ThrowingHandler handler) =>
        _fixture.CreateClient(builder => builder.ConfigureServices(services =>
        {
            services.AddLogging(logging => logging.AddProvider(logs));
            services.AddScoped<IDomainEventHandler>(_ => handler);
        }));

    // A draft, so seeding raises no event: the only one is the issue's, inside the request under test.
    private async Task<Invoice> AddDraftInvoiceAsync()
    {
        Customer customer = CustomerFactory.Generate();
        await AddAsync(customer);
        Invoice invoice = InvoiceFactory.Generate(customer.Id);
        await AddAsync(invoice);
        return invoice;
    }

    private static Task<HttpResponseMessage> IssueInvoiceAsync(HttpClient client, Invoice invoice) =>
        client.PostAsync($"/api/invoices/{invoice.Id.Value}/issue", null, CancellationToken);

    private static async Task<LogEntry> WaitForEntryAsync(CapturingLoggerProvider logs, LogLevel level)
    {
        bool logged = await Wait.UntilAsync(
            () => Task.FromResult(logs.Entries.Any(e => IsFromMiddleware(e, level))),
            CancellationToken);
        Assert.True(logged, $"No {level} entry was logged by {nameof(EventualConsistencyMiddleware)}.");
        return logs.Entries.First(e => IsFromMiddleware(e, level));
    }

    private static bool IsFromMiddleware(LogEntry entry, LogLevel level) =>
        entry.Level == level && entry.Category == typeof(EventualConsistencyMiddleware).FullName;

    private sealed class ThrowingHandler(Exception failure) : IDomainEventHandler
    {
        public Task HandleAsync(IDomainEvent domainEvent, CancellationToken ct) => throw failure;
    }
}
