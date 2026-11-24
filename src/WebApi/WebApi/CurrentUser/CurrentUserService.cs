using System.Security.Claims;
using MikeAllan.VerticalSliceArchitecture.Domain.Auditing;

namespace MikeAllan.VerticalSliceArchitecture.CurrentUser;

/// <summary>
/// The user of the current HTTP request, as the domain library's auditing sees them.
/// </summary>
public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string? UserId => httpContextAccessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
}
