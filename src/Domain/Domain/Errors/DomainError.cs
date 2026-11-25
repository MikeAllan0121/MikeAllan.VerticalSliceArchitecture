namespace MikeAllan.VerticalSliceArchitecture.Domain.Errors;

/// <summary>
/// An expected business-rule failure: one of a closed set of kinds, each mapped to an HTTP status in one
/// place (the WebApi's <c>Internals/ErrorHandling/DomainErrorExtensions</c>).
/// </summary>
/// <remarks>
/// To add a kind (e.g. <c>ForbiddenError</c>): declare a <c>sealed record ForbiddenError(string Code, string Description)</c>
/// beside the others and add it here. Every switch over <see cref="DomainError"/> then fails with CS8509
/// until it maps the new kind, which is why those switches never have a <c>_</c> or <c>var</c> arm.
/// </remarks>
public union DomainError(NotFoundError, ConflictError);
