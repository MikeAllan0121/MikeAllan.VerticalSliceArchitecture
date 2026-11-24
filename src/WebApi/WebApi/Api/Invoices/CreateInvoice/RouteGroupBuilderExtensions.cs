namespace MikeAllan.VerticalSliceArchitecture.Api.Invoices.CreateInvoice;

public static class RouteGroupBuilderExtensions
{
    extension(RouteGroupBuilder group)
    {
        public RouteGroupBuilder MapCreateInvoice()
        {
            group.MapPost("/", CreateInvoiceEndpoint.Handle)
                .WithName("CreateInvoice")
                .WithSummary("Create a draft invoice")
                .WithDescription("Creates a draft invoice for an existing customer with the specified lines. Returns the ID of the created invoice.")
                .ProducesValidationProblem();

            return group;
        }
    }
}
