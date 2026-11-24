namespace MikeAllan.VerticalSliceArchitecture.EventualConsistency;

/// <summary>
/// Thrown by a domain event handler that cannot complete for an expected reason, carrying the
/// <see cref="DomainError"/> that says why. <see cref="EventualConsistencyMiddleware"/> logs its code and
/// description and carries on with the remaining events.
/// </summary>
public sealed class EventualConsistencyException(DomainError error) : Exception(error.Description)
{
    public DomainError Error { get; } = error;
}
