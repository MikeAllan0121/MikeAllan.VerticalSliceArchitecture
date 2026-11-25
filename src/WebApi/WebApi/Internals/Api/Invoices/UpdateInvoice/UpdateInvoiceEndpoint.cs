using Microsoft.AspNetCore.Http.HttpResults;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Internals.ErrorHandling;
using MikeAllan.VerticalSliceArchitecture.Internals.Validation;

namespace MikeAllan.VerticalSliceArchitecture.Internals.Api.Invoices.UpdateInvoice;

internal static class UpdateInvoiceEndpoint
{
    internal static async Task<Results<NoContent, ProblemHttpResult>> Handle(
        [NotEmptyGuid] Guid invoiceId,
        UpdateInvoiceRequest request,
        ApplicationDbContext dbContext,
        CancellationToken ct)
    {
        Invoice? invoice = await dbContext.Invoices
            .ById(InvoiceId.From(invoiceId))
            .FirstOrDefaultAsync(ct);

        if (invoice is null)
            return InvoiceErrors.NotFound.ToProblem();

        IEnumerable<InvoiceLine> lines = request.Lines.Select(l => new InvoiceLine(l.Description, l.Quantity, l.UnitPrice));

        if (invoice.ReplaceLines(lines) is DomainError error)
            return error.ToProblem();

        await dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
