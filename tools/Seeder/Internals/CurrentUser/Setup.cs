using Microsoft.Extensions.DependencyInjection;
using MikeAllan.VerticalSliceArchitecture.Domain.Auditing;

namespace MikeAllan.VerticalSliceArchitecture.Seeder.Internals.CurrentUser;

internal static class Setup
{
    extension(IServiceCollection services)
    {
        // The domain library audits every save with the current user; for the seeder that's a fixed identity.
        public IServiceCollection AddCurrentUser() =>
            services.AddSingleton<ICurrentUserService, SeederUserService>();
    }
}
