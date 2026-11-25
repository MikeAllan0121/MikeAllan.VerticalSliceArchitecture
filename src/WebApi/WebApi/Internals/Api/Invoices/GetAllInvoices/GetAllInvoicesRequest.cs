using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;
using MikeAllan.VerticalSliceArchitecture.Internals.Pagination;

namespace MikeAllan.VerticalSliceArchitecture.Internals.Api.Invoices.GetAllInvoices;

internal sealed record GetAllInvoicesRequest : PagedRequest<Invoice>
{
    protected override SortColumnMap<Invoice> SortColumns => InvoiceQueries.SortColumns;
}
