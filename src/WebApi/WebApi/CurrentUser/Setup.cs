using MikeAllan.VerticalSliceArchitecture.Domain.Auditing;

namespace MikeAllan.VerticalSliceArchitecture.CurrentUser;

public static class Setup
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
