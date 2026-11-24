namespace MikeAllan.VerticalSliceArchitecture.Domain.Errors;

/// <summary>
/// The outcome of a domain operation that can fail on a business rule and returns nothing on success:
/// either <see cref="Success"/> or a <see cref="DomainError"/>.
/// </summary>
/// <remarks>
/// Return <c>Success.Instance</c> or a catalogue error (e.g. <c>InvoiceErrors.NotDraft</c>); both convert
/// implicitly. Callers branch with <c>if (result is DomainError error) ...</c>. Invalid input is not a
/// result: guard clauses throw for that.
/// </remarks>
public union Result(Success, DomainError);

/// <summary>
/// The outcome of a domain operation that can fail on a business rule and produces a <typeparamref name="T"/>
/// on success: either the value or a <see cref="DomainError"/>.
/// </summary>
/// <remarks>
/// Callers take the value with <c>if (result is not T value) return ...;</c> after handling the error.
/// A value-type <typeparamref name="T"/> is boxed when stored; see <c>.claude/rules/domain.md</c> before
/// returning a struct on a hot path.
/// </remarks>
public union Result<T>(T, DomainError);
