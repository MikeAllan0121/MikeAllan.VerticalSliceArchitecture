using MikeAllan.VerticalSliceArchitecture.Api.Invoices.CreateInvoice;
using MikeAllan.VerticalSliceArchitecture.Api.Invoices.GetAllInvoices;
using MikeAllan.VerticalSliceArchitecture.Api.Invoices.IssueInvoice;
using MikeAllan.VerticalSliceArchitecture.Api.Invoices.RecordPayment;
using MikeAllan.VerticalSliceArchitecture.Api.Invoices.UpdateInvoice;

namespace MikeAllan.VerticalSliceArchitecture.Api.Invoices;

public static class Setup
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
