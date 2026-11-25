namespace MikeAllan.VerticalSliceArchitecture.Domain.Concurrency;

/// <summary>
/// What a save that lost an optimistic-concurrency race (<c>DbUpdateConcurrencyException</c>) becomes. The
/// WebApi answers it with a 409 problem carrying this code. Nothing was written, so the caller can retry.
/// </summary>
// Entries are typed DomainError, not the leaf record, so each one converts straight to Result and Result<T>.
public static class ConcurrencyErrors
{
    public static readonly DomainError Conflict = new ConflictError(
        "Concurrency.Conflict",
        "The resource was changed by another request; nothing was saved, so retry the request");
}
