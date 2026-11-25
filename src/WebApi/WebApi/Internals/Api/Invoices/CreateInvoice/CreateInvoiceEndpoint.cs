using Microsoft.AspNetCore.Http.HttpResults;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Internals.ErrorHandling;

namespace MikeAllan.VerticalSliceArchitecture.Internals.Api.Invoices.CreateInvoice;

internal static class CreateInvoiceEndpoint
{
    internal static async Task<Results<Ok<CreateInvoiceResponse>, ProblemHttpResult>> Handle(
        CreateInvoiceRequest request,
        ApplicationDbContext dbContext,
        CancellationToken ct)
    {
        CustomerId customerId = CustomerId.From(request.CustomerId);

        if (!await dbContext.Customers.AnyAsync(c => c.Id == customerId, ct))
            return CustomerErrors.NotFound.ToProblem();

        Invoice invoice = Invoice.Create(
            customerId,
            request.Lines.Select(l => new InvoiceLine(l.Description, l.Quantity, l.UnitPrice)));

        dbContext.Invoices.Add(invoice);
        await dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new CreateInvoiceResponse(invoice.Id.Value));
    }
}
