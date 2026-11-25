using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;

namespace MikeAllan.VerticalSliceArchitecture.Internals.Api.Customers.GetAllCustomers;

internal static class RouteGroupBuilderExtensions
{
    extension(RouteGroupBuilder group)
    {
        public RouteGroupBuilder MapGetAllCustomers()
        {
            group.MapGet("/", GetAllCustomersEndpoint.Handle)
                .WithName("GetAllCustomers")
                .WithSummary("Get a page of customers")
                .WithDescription(
                    "Retrieves a page of customers with their outstanding balances, wrapped in the standard paged " +
                    "envelope (items plus page, pageSize, totalCount, totalPages). " +
                    $"page is 1-based and defaults to {PagingParams.FirstPage}. " +
                    $"pageSize defaults to {PagingParams.DefaultPageSize} and is clamped to at most {PagingParams.MaxPageSize}. " +
                    $"sortBy is one of: {string.Join(", ", CustomerQueries.SortColumns.AllowedColumns)}; anything else returns 400. " +
                    $"sortDirection is one of: {string.Join(", ", SortDirections.Allowed)}; defaults to ascending.")
                .ProducesValidationProblem();

            return group;
        }
    }
}
