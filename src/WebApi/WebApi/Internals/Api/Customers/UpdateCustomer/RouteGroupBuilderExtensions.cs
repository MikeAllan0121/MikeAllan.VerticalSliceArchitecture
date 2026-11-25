namespace MikeAllan.VerticalSliceArchitecture.Internals.Api.Customers.UpdateCustomer;

internal static class RouteGroupBuilderExtensions
{
    extension(RouteGroupBuilder group)
    {
        public RouteGroupBuilder MapUpdateCustomer()
        {
            group.MapPut("/{customerId}", UpdateCustomerEndpoint.Handle)
                .WithName("UpdateCustomer")
                .WithSummary("Update an existing customer")
                .WithDescription("Updates a customer's name and email address. The customer must exist.")
                .ProducesValidationProblem();

            return group;
        }
    }
}
