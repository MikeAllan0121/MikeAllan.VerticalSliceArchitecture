namespace MikeAllan.VerticalSliceArchitecture.Api.Customers.GetCustomer;

public static class RouteGroupBuilderExtensions
{
    extension(RouteGroupBuilder group)
    {
        public RouteGroupBuilder MapGetCustomer()
        {
            group.MapGet("/{customerId}", GetCustomerEndpoint.Handle)
                .WithName("GetCustomer")
                .WithSummary("Get a specific customer")
                .WithDescription("Retrieves a customer with their outstanding balance and all their invoices, including each invoice's lines.")
                .ProducesValidationProblem();

            return group;
        }
    }
}
