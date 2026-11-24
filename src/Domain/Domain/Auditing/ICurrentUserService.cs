namespace MikeAllan.VerticalSliceArchitecture.Domain.Auditing;

/// <summary>
/// Who is making the change, stamped onto <see cref="IAuditable"/> entities when they are saved.
/// </summary>
/// <remarks>
/// An interface because each host answers it differently: the WebApi reads the HTTP request's user,
/// the Seeder uses a fixed identity. The host registers its implementation.
/// </remarks>
public interface ICurrentUserService
{
    string? UserId { get; }
}
