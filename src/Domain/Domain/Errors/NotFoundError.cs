namespace MikeAllan.VerticalSliceArchitecture.Domain.Errors;

/// <summary>
/// The thing an operation needs does not exist. Mapped to 404 by the WebApi.
/// </summary>
/// <remarks>
/// Suffixed <c>Error</c> because ASP.NET Core's <c>Microsoft.AspNetCore.Http.HttpResults</c> already has a
/// <c>NotFound</c> type.
/// </remarks>
/// <param name="Code">Stable, machine-readable identifier, e.g. <c>Customer.NotFound</c>.</param>
/// <param name="Description">Human-readable message; becomes the problem's <c>title</c>.</param>
public sealed record NotFoundError(string Code, string Description);
