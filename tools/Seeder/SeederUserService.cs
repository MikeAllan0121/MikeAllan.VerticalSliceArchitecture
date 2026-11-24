using MikeAllan.VerticalSliceArchitecture.Domain.Auditing;

namespace Seeder;

public sealed class SeederUserService : ICurrentUserService
{
    public string? UserId => "Seeder";
}
