namespace MikeAllan.VerticalSliceArchitecture.Api.Invoices.UpdateInvoice;

public static class RouteGroupBuilderExtensions
{
    extension(RouteGroupBuilder group)
    {
        public RouteGroupBuilder MapUpdateInvoice()
        {
            group.MapPut("/{invoiceId}", UpdateInvoiceEndpoint.Handle)
                .WithName("UpdateInvoice")
                .WithSummary("Replace a draft invoice's lines")
                .WithDescription("Replaces every line on an invoice. The invoice must exist and still be a draft.")
                .ProducesValidationProblem();

            return group;
        }
    }
}
