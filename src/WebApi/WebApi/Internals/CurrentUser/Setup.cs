using MikeAllan.VerticalSliceArchitecture.Domain.Auditing;

namespace MikeAllan.VerticalSliceArchitecture.Internals.CurrentUser;

internal static class Setup
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCurrentUser()
        {
            services.AddHttpContextAccessor();
            services.AddScoped<ICurrentUserService, CurrentUserService>();

            return services;
        }
    }
}
