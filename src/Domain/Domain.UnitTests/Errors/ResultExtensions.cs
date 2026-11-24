using MikeAllan.VerticalSliceArchitecture.Domain.Errors;

namespace MikeAllan.VerticalSliceArchitecture.Domain.UnitTests.Errors;

/// <summary>
/// Asserts which case a <see cref="Result"/> or <see cref="Result{T}"/> holds and returns it.
/// </summary>
/// <remarks>
/// Compare an error with the catalogue entry, both as <see cref="DomainError"/>:
/// <c>Assert.Equal(InvoiceErrors.NotDraft, result.ShouldBeError())</c>. Comparing against the leaf
/// record (<c>ConflictError</c>) is never equal, because the union holds a <see cref="DomainError"/>.
/// </remarks>
internal static class ResultExtensions
{
    extension(Result result)
    {
        public DomainError ShouldBeError() => Assert.IsType<DomainError>(result.Value);

        public void ShouldBeSuccess() => Assert.IsType<Success>(result.Value);
    }

    extension<T>(Result<T> result)
    {
        public DomainError ShouldBeError() => Assert.IsType<DomainError>(result.Value);

        public T ShouldBeSuccess() => Assert.IsType<T>(result.Value);
    }
}
