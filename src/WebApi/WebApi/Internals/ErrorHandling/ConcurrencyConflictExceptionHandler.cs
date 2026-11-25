using Microsoft.AspNetCore.Diagnostics;
using MikeAllan.VerticalSliceArchitecture.Domain.Concurrency;

namespace MikeAllan.VerticalSliceArchitecture.Internals.ErrorHandling;

/// <summary>
/// Answers a save that lost an optimistic-concurrency race with
/// <see cref="ConcurrencyErrors.Conflict"/>: a 409 problem whose <c>code</c> is <c>Concurrency.Conflict</c>.
/// </summary>
/// <remarks>
/// Any handler's <c>SaveChangesAsync</c> can throw <see cref="DbUpdateConcurrencyException"/> when another
/// request changed the same aggregate after it was loaded (see the Domain's <c>HasRowVersion</c>). Mapping it
/// here, once, covers every write endpoint and every aggregate without a try/catch in each handler. The
/// failed save committed nothing and raised no domain events, so the client can safely send the request again;
/// the retry re-runs the domain rules against the current state.
/// </remarks>
internal sealed class ConcurrencyConflictExceptionHandler(ILogger<ConcurrencyConflictExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not DbUpdateConcurrencyException)
            return false;

        logger.ConcurrencyConflict(httpContext.Request.Method, httpContext.Request.Path);

        await ConcurrencyErrors.Conflict.ToProblem().ExecuteAsync(httpContext);

        return true;
    }
}

internal static partial class ConcurrencyConflictExceptionHandlerLog
{
    [LoggerMessage(LogLevel.Warning, "Concurrency conflict on {Method} {Path}: another request changed the aggregate first")]
    public static partial void ConcurrencyConflict(this ILogger logger, string method, string path);
}
