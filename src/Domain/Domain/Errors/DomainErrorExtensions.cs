namespace MikeAllan.VerticalSliceArchitecture.Domain.Errors;

/// <summary>The fields every <see cref="DomainError"/> kind carries, for code that doesn't care about the kind.</summary>
public static class DomainErrorExtensions
{
    extension(DomainError error)
    {
        /// <summary>The kind's <c>Code</c>, e.g. <c>Invoice.NotDraft</c>.</summary>
        /// <exception cref="InvalidOperationException">The error is <c>default</c> and holds no kind.</exception>
        public string Code => error switch
        {
            NotFoundError e => e.Code,
            ConflictError e => e.Code,
            null => throw DefaultError(),
        };

        /// <summary>The kind's human-readable <c>Description</c>.</summary>
        /// <exception cref="InvalidOperationException">The error is <c>default</c> and holds no kind.</exception>
        public string Description => error switch
        {
            NotFoundError e => e.Description,
            ConflictError e => e.Description,
            null => throw DefaultError(),
        };
    }

    private static InvalidOperationException DefaultError() =>
        new($"A default {nameof(DomainError)} holds no error; build one from a catalogue entry.");
}
