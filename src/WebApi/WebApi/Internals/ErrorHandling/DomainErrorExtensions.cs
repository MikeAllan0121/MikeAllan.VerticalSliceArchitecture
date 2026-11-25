using Microsoft.AspNetCore.Http.HttpResults;

namespace MikeAllan.VerticalSliceArchitecture.Internals.ErrorHandling;

/// <summary>
/// Turns a <see cref="DomainError"/> into a problem-details response with the matching status code. The
/// one place an error kind is mapped to HTTP.
/// </summary>
/// <remarks>
/// The switch is exhaustive with no <c>_</c> arm on purpose: a new <see cref="DomainError"/> kind fails
/// the build here (CS8509) until it is given a status, instead of silently becoming a 500.
/// </remarks>
internal static class DomainErrorExtensions
{
    extension(DomainError error)
    {
        /// <summary>
        /// A problem whose <c>title</c> is the error's description and whose <c>code</c> extension is its
        /// code, e.g. <c>Invoice.NotDraft</c>.
        /// </summary>
        public ProblemHttpResult ToProblem() => error switch
        {
            NotFoundError e => Problem(StatusCodes.Status404NotFound, e.Code, e.Description),
            ConflictError e => Problem(StatusCodes.Status409Conflict, e.Code, e.Description),
            null => TypedResults.Problem(statusCode: StatusCodes.Status500InternalServerError), // default(DomainError)
        };
    }

    private static ProblemHttpResult Problem(int statusCode, string code, string description) =>
        TypedResults.Problem(
            statusCode: statusCode,
            title: description,
            extensions: new Dictionary<string, object?> { ["code"] = code });
}
