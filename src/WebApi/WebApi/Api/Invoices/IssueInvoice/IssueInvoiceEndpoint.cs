using Microsoft.AspNetCore.Http.HttpResults;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.ErrorHandling;
using MikeAllan.VerticalSliceArchitecture.Validation;

namespace MikeAllan.VerticalSliceArchitecture.Api.Invoices.IssueInvoice;

public static class IssueInvoiceEndpoint
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
