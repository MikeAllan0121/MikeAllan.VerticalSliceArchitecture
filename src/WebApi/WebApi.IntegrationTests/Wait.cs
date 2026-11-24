namespace MikeAllan.VerticalSliceArchitecture.IntegrationTests;

/// <summary>
/// Polls for the effect of a deferred domain event. The WebApi publishes events after the response has
/// been sent, so the caller sees the 2xx before the handler has run.
/// </summary>
internal static class Wait
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan Interval = TimeSpan.FromMilliseconds(100);

    /// <returns>Whether <paramref name="condition"/> became true before the timeout.</returns>
    public static async Task<bool> UntilAsync(Func<Task<bool>> condition, CancellationToken ct)
    {
        DateTimeOffset deadline = DateTimeOffset.UtcNow + Timeout;

        while (DateTimeOffset.UtcNow < deadline)
        {
            if (await condition())
                return true;

            await Task.Delay(Interval, ct);
        }

        return await condition();
    }
}
