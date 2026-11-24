using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;
using MikeAllan.VerticalSliceArchitecture.Pagination;

namespace MikeAllan.VerticalSliceArchitecture.Api.Customers.GetAllCustomers;

public sealed record GetAllCustomersRequest : PagedRequest<Customer>
{
    protected override SortColumnMap<Customer> SortColumns => CustomerQueries.SortColumns;
}
