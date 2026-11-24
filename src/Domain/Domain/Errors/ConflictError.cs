namespace MikeAllan.VerticalSliceArchitecture.Domain.Errors;

/// <summary>
/// The operation clashes with the current state, e.g. an invoice that has already been issued. Mapped to 409
/// by the WebApi.
/// </summary>
/// <remarks>
/// Suffixed <c>Error</c> because ASP.NET Core's <c>Microsoft.AspNetCore.Http.HttpResults</c> already has a
/// <c>Conflict</c> type.
/// </remarks>
/// <param name="Code">Stable, machine-readable identifier, e.g. <c>Invoice.NotDraft</c>.</param>
/// <param name="Description">Human-readable message; becomes the problem's <c>title</c>.</param>
public sealed record ConflictError(string Code, string Description);
