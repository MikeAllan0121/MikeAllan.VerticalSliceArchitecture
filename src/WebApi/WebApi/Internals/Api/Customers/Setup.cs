using MikeAllan.VerticalSliceArchitecture.Domain.DomainEvents;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Internals.Api.Customers.CreateCustomer;
using MikeAllan.VerticalSliceArchitecture.Internals.Api.Customers.GetAllCustomers;
using MikeAllan.VerticalSliceArchitecture.Internals.Api.Customers.GetCustomer;
using MikeAllan.VerticalSliceArchitecture.Internals.Api.Customers.InvoiceAmountDueChanged;
using MikeAllan.VerticalSliceArchitecture.Internals.Api.Customers.UpdateCustomer;

namespace MikeAllan.VerticalSliceArchitecture.Internals.Api.Customers;

internal static class Setup
{
    extension(IServiceCollection services)
    {
        public IServiceCollection AddCustomers() =>
            services.AddDomainEventHandler<InvoiceAmountDueChangedEvent, InvoiceAmountDueChangedEventHandler>();
    }

    extension(IEndpointRouteBuilder app)
    {
        public IEndpointRouteBuilder MapCustomers()
        {
            RouteGroupBuilder group = app.MapGroup("customers")
                .WithTags("Customers")
                .ProducesProblem(StatusCodes.Status500InternalServerError);

            group.MapCreateCustomer();
            group.MapGetAllCustomers();
            group.MapGetCustomer();
            group.MapUpdateCustomer();

            return app;
        }
    }
}
