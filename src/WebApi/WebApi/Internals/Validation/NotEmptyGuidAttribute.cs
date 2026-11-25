using System.ComponentModel.DataAnnotations;

namespace MikeAllan.VerticalSliceArchitecture.Internals.Validation;

/// <summary>
/// Rejects <see cref="Guid.Empty"/>. <see cref="RequiredAttribute"/> can't: a <see cref="Guid"/> is never null.
/// </summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter)]
internal sealed class NotEmptyGuidAttribute : ValidationAttribute
{
    public NotEmptyGuidAttribute()
        : base("The {0} field must not be an empty GUID.")
    {
    }

    public override bool IsValid(object? value) => value is not Guid guid || guid != Guid.Empty;
}
