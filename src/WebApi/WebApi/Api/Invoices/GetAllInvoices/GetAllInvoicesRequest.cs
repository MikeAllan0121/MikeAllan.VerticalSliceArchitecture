using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;
using MikeAllan.VerticalSliceArchitecture.Pagination;

namespace MikeAllan.VerticalSliceArchitecture.Api.Invoices.GetAllInvoices;

public sealed record GetAllInvoicesRequest : PagedRequest<Invoice>
{
    protected override SortColumnMap<Invoice> SortColumns => InvoiceQueries.SortColumns;
}
