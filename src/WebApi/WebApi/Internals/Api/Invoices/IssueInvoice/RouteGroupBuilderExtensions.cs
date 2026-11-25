namespace MikeAllan.VerticalSliceArchitecture.Internals.Api.Invoices.IssueInvoice;

internal static class RouteGroupBuilderExtensions
{
    extension(RouteGroupBuilder group)
    {
        public RouteGroupBuilder MapIssueInvoice()
        {
            group.MapPost("/{invoiceId}/issue", IssueInvoiceEndpoint.Handle)
                .WithName("IssueInvoice")
                .WithSummary("Issue a draft invoice")
                .WithDescription("Issues a draft invoice, so its total is owed by the customer. The invoice must be a draft with at least one line.")
                .ProducesValidationProblem();

            return group;
        }
    }
}
