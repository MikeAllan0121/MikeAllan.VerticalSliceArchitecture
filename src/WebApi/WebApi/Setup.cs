using MikeAllan.VerticalSliceArchitecture.Api;
using MikeAllan.VerticalSliceArchitecture.CurrentUser;
using MikeAllan.VerticalSliceArchitecture.Domain;
using MikeAllan.VerticalSliceArchitecture.ErrorHandling;
using MikeAllan.VerticalSliceArchitecture.EventualConsistency;
using MikeAllan.VerticalSliceArchitecture.JsonSerialization;
using MikeAllan.VerticalSliceArchitecture.OpenApi;
using MikeAllan.VerticalSliceArchitecture.Validation;

namespace MikeAllan.VerticalSliceArchitecture;

/// <summary>
/// The top of the wiring tree: calls the <c>Setup</c> of each concern and each feature (its direct
/// children), and owns the middleware order. Program.cs calls only this.
/// </summary>
public static class Setup
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
