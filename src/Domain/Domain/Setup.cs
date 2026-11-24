using Microsoft.Extensions.Hosting;
using MikeAllan.VerticalSliceArchitecture.Domain.Auditing;
using MikeAllan.VerticalSliceArchitecture.Domain.Database;
using MikeAllan.VerticalSliceArchitecture.Domain.DomainEvents;

namespace MikeAllan.VerticalSliceArchitecture.Domain;

/// <summary>
/// The library's single entry point: registers the database, auditing and domain-event dispatch.
/// </summary>
/// <remarks>
/// The host must also register an <see cref="ICurrentUserService"/>: who the current user is depends on
/// the host (an HTTP request, a seeding tool), so this library cannot decide it.
/// </remarks>
public static class Setup
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddDomain()
        {
            builder.Services.AddAuditing();
            builder.Services.AddDomainEvents();
            builder.AddDatabase();

            return builder;
        }
    }
}
