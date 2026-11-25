using MikeAllan.VerticalSliceArchitecture.Domain.Auditing;

namespace MikeAllan.VerticalSliceArchitecture.Seeder.Internals.CurrentUser;

/// <summary>Who the domain library's auditing records as the author of seeded rows.</summary>
internal sealed class SeederUserService : ICurrentUserService
{
    public string? UserId => "Seeder";
}
