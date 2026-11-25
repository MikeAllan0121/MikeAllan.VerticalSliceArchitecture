using Microsoft.AspNetCore.Http.HttpResults;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;

namespace MikeAllan.VerticalSliceArchitecture.Internals.Api.Invoices.GetAllInvoices;

internal static class GetAllInvoicesEndpoint
{
    internal static async Task<Ok<PagedList<GetAllInvoicesResponse>>> Handle(
        [AsParameters] GetAllInvoicesRequest request,
        ApplicationDbContext dbContext,
        CancellationToken ct)
    {
        // AmountDue is not mapped, so the projection spells it out as Total - AmountPaid.
        PagedList<GetAllInvoicesResponse> invoices = await dbContext.Invoices
            .SortedBy(request.SortBy, request.ToSortDirection())
            .ToPagedListAsync(
                request.ToPaging(),
                i => new GetAllInvoicesResponse(
                    i.Id.Value,
                    i.CustomerId.Value,
                    i.Status,
                    i.Total,
                    i.Total - i.AmountPaid,
                    i.Lines.Select(l => new GetAllInvoicesResponse.InvoiceLineDto(l.Description, l.Quantity, l.UnitPrice)).ToList()),
                ct);

        return TypedResults.Ok(invoices);
    }
}
