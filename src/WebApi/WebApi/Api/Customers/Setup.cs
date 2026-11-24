using MikeAllan.VerticalSliceArchitecture.Domain.DomainEvents;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Api.Customers.CreateCustomer;
using MikeAllan.VerticalSliceArchitecture.Api.Customers.GetAllCustomers;
using MikeAllan.VerticalSliceArchitecture.Api.Customers.GetCustomer;
using MikeAllan.VerticalSliceArchitecture.Api.Customers.InvoiceAmountDueChanged;
using MikeAllan.VerticalSliceArchitecture.Api.Customers.UpdateCustomer;

namespace MikeAllan.VerticalSliceArchitecture.Api.Customers;

public static class Setup
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
