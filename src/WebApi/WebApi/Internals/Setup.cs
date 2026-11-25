using MikeAllan.VerticalSliceArchitecture.Domain;
using MikeAllan.VerticalSliceArchitecture.Internals.Api;
using MikeAllan.VerticalSliceArchitecture.Internals.CurrentUser;
using MikeAllan.VerticalSliceArchitecture.Internals.ErrorHandling;
using MikeAllan.VerticalSliceArchitecture.Internals.EventualConsistency;
using MikeAllan.VerticalSliceArchitecture.Internals.JsonSerialization;
using MikeAllan.VerticalSliceArchitecture.Internals.OpenApi;
using MikeAllan.VerticalSliceArchitecture.Internals.Validation;

namespace MikeAllan.VerticalSliceArchitecture.Internals;

/// <summary>
/// The top of the wiring tree: calls the <c>Setup</c> of each concern and each feature (its direct
/// children), and owns the middleware order. Program.cs calls only this.
/// </summary>
/// <remarks>
/// Everything under <c>Internals/</c> is <c>internal</c>: an executable has no consumers, so nothing in it
/// is public API. The test projects reach it through <c>InternalsVisibleTo</c> in the csproj.
/// </remarks>
internal static class Setup
{
    extension(IHostApplicationBuilder builder)
    {
        public IHostApplicationBuilder AddWebApi()
        {
            builder.AddServiceDefaults();
            builder.AddDomain();

            builder.Services
                .AddCurrentUser()
                .AddErrorHandling()
                .AddEventualConsistency()
                .AddJsonSerialization()
                .AddRequestValidation()
                .AddApiDocs()
                .AddApi();

            return builder;
        }
    }

    extension(WebApplication app)
    {
        /// <summary>
        /// Middleware in pipeline order, then endpoints. The only place middleware is added: order is
        /// app-wide, so no feature or concern adds its own.
        /// </summary>
        public WebApplication UseWebApi()
        {
            // First, so it catches exceptions from everything after it.
            app.UseErrorHandling();

            if (!app.Environment.IsDevelopment())
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // Publishes the domain events queued during the request, after the response is sent.
            app.UseEventualConsistency();

            app.MapDefaultEndpoints();
            app.MapApiDocs();

            app.MapApi();

            return app;
        }
    }
}
