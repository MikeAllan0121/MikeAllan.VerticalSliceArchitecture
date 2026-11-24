using MikeAllan.VerticalSliceArchitecture.Api.Customers;
using MikeAllan.VerticalSliceArchitecture.Api.Invoices;

namespace MikeAllan.VerticalSliceArchitecture.Api;

/// <summary>
/// Wires the API Features (Customers, Invoices). Owns the <c>/api</c> route group, so every Feature's routes
/// get their prefix here and the top-level <c>Setup</c> only calls <c>MapApi()</c>.
/// </summary>
public static class Setup
{
    extension(IServiceCollection services)
    {
        // Invoices registers no services, so only Customers appears here.
        public IServiceCollection AddApi() => services.AddCustomers();
    }

    extension(IEndpointRouteBuilder app)
    {
        public IEndpointRouteBuilder MapApi()
        {
            RouteGroupBuilder api = app.MapGroup("api");
            api.MapCustomers();
            api.MapInvoices();

            return app;
        }
    }
}
