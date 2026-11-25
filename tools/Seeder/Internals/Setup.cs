using Microsoft.Extensions.Hosting;
using MikeAllan.VerticalSliceArchitecture.Domain;
using MikeAllan.VerticalSliceArchitecture.Seeder.Internals.CurrentUser;
using MikeAllan.VerticalSliceArchitecture.Seeder.Internals.Seeding;
using MikeAllan.VerticalSliceArchitecture.ServiceDefaults;

namespace MikeAllan.VerticalSliceArchitecture.Seeder.Internals;

/// <summary>
/// The top of the Seeder's wiring tree: calls the <c>Setup</c> of each child. Program.cs calls only this.
/// </summary>
internal static class Setup
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddSeeder()
        {
            builder.AddServiceDefaults();

            // The DbContext, its interceptors and domain-event dispatch. No HTTP request here, so events raised
            // while seeding are published immediately (and nothing in the seeder handles them).
            builder.AddDomain();

            builder.Services
                .AddCurrentUser()
                .AddSeeding();

            return builder;
        }
    }
}
