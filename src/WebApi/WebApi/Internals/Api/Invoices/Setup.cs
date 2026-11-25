using MikeAllan.VerticalSliceArchitecture.Internals.Api.Invoices.CreateInvoice;
using MikeAllan.VerticalSliceArchitecture.Internals.Api.Invoices.GetAllInvoices;
using MikeAllan.VerticalSliceArchitecture.Internals.Api.Invoices.IssueInvoice;
using MikeAllan.VerticalSliceArchitecture.Internals.Api.Invoices.RecordPayment;
using MikeAllan.VerticalSliceArchitecture.Internals.Api.Invoices.UpdateInvoice;

namespace MikeAllan.VerticalSliceArchitecture.Internals.Api.Invoices;

internal static class Setup
{
    extension(IEndpointRouteBuilder app)
    {
        public IEndpointRouteBuilder MapInvoices()
        {
            RouteGroupBuilder group = app.MapGroup("invoices")
                .WithTags("Invoices")
                .ProducesProblem(StatusCodes.Status500InternalServerError);

            group.MapCreateInvoice();
            group.MapGetAllInvoices();
            group.MapIssueInvoice();
            group.MapRecordPayment();
            group.MapUpdateInvoice();

            return app;
        }
    }
}
