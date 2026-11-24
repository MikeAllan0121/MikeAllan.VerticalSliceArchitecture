using System.ComponentModel.DataAnnotations;

namespace MikeAllan.VerticalSliceArchitecture.UnitTests;

/// <summary>
/// Runs a request type's DataAnnotations and <see cref="IValidatableObject"/> rules the way
/// <c>AddValidation()</c> does before a handler, without an HTTP host.
/// </summary>
/// <remarks>
/// <see cref="Validator.TryValidateObject(object, ValidationContext, ICollection{ValidationResult}?, bool)"/>
/// does not recurse into collection items, so validate a nested DTO (e.g. an <c>InvoiceLineDto</c>) on its own.
/// Nesting itself (<c>Lines[0].Description</c>) is covered by the integration tests.
/// </remarks>
internal static class RequestValidation
{
    public static IReadOnlyList<ValidationResult> Validate(object instance)
    {
        List<ValidationResult> results = [];
        Validator.TryValidateObject(instance, new ValidationContext(instance), results, validateAllProperties: true);
        return results;
    }

    /// <summary>The members named by every failed rule, e.g. <c>["CustomerId", "Lines"]</c>.</summary>
    public static IReadOnlyList<string> InvalidMembers(object instance) =>
        [.. Validate(instance).SelectMany(r => r.MemberNames)];
}
