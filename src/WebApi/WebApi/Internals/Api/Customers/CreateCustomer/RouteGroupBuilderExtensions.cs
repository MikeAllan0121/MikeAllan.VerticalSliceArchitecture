namespace MikeAllan.VerticalSliceArchitecture.Internals.Api.Customers.CreateCustomer;

internal static class RouteGroupBuilderExtensions
{
    extension(RouteGroupBuilder group)
    {
        public RouteGroupBuilder MapCreateCustomer()
        {
            group.MapPost("/", CreateCustomerEndpoint.Handle)
                .WithName("CreateCustomer")
                .WithSummary("Create a new customer")
                .WithDescription("Creates a new customer with the specified name and email address. Returns the ID of the created customer.")
                .ProducesValidationProblem();

            return group;
        }
    }
}
