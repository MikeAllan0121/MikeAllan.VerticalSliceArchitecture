using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;
using MikeAllan.VerticalSliceArchitecture.Internals.Pagination;

namespace MikeAllan.VerticalSliceArchitecture.Internals.Api.Customers.GetAllCustomers;

internal sealed record GetAllCustomersRequest : PagedRequest<Customer>
{
    protected override SortColumnMap<Customer> SortColumns => CustomerQueries.SortColumns;
}
