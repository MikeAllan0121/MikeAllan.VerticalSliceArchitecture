namespace MikeAllan.VerticalSliceArchitecture.Internals.Api.Invoices.RecordPayment;

internal static class RouteGroupBuilderExtensions
{
    extension(RouteGroupBuilder group)
    {
        public RouteGroupBuilder MapRecordPayment()
        {
            group.MapPost("/{invoiceId}/payments", RecordPaymentEndpoint.Handle)
                .WithName("RecordPayment")
                .WithSummary("Record a payment against an invoice")
                .WithDescription(
                    "Records a full or partial payment against an issued invoice. The payment can't be more than " +
                    "the amount due; once nothing is due, the invoice is paid.")
                .ProducesValidationProblem();

            return group;
        }
    }
}
