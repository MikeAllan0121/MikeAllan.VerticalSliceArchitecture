using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests.EventualConsistency;

/// <summary>Records what the app logs, with each message's structured values, so a test can assert on them.</summary>
internal sealed class CapturingLoggerProvider : ILoggerProvider
{
    public ConcurrentQueue<LogEntry> Entries { get; } = [];

    public ILogger CreateLogger(string categoryName) => new CapturingLogger(categoryName, Entries);

    public void Dispose()
    {
    }

    internal sealed record LogEntry(
        string Category,
        LogLevel Level,
        Exception? Exception,
        IReadOnlyDictionary<string, object?> Values);

    private sealed class CapturingLogger(string category, ConcurrentQueue<LogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            Dictionary<string, object?> values = state is IEnumerable<KeyValuePair<string, object?>> pairs
                ? pairs.ToDictionary(p => p.Key, p => p.Value)
                : [];

            entries.Enqueue(new LogEntry(category, logLevel, exception, values));
        }
    }
}
