using Microsoft.AspNetCore.Http.HttpResults;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Internals.ErrorHandling;
using MikeAllan.VerticalSliceArchitecture.Internals.Validation;

namespace MikeAllan.VerticalSliceArchitecture.Internals.Api.Invoices.IssueInvoice;

internal static class IssueInvoiceEndpoint
{
    internal static async Task<Results<NoContent, ProblemHttpResult>> Handle(
        [NotEmptyGuid] Guid invoiceId,
        ApplicationDbContext dbContext,
        CancellationToken ct)
    {
        Invoice? invoice = await dbContext.Invoices
            .ById(InvoiceId.From(invoiceId))
            .FirstOrDefaultAsync(ct);

        if (invoice is null)
            return InvoiceErrors.NotFound.ToProblem();

        if (invoice.Issue() is DomainError error)
            return error.ToProblem();

        await dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
