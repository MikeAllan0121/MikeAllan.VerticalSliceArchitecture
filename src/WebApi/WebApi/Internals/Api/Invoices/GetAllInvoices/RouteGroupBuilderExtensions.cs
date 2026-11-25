using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;

namespace MikeAllan.VerticalSliceArchitecture.Internals.Api.Invoices.GetAllInvoices;

internal static class RouteGroupBuilderExtensions
{
    extension(RouteGroupBuilder group)
    {
        public RouteGroupBuilder MapGetAllInvoices()
        {
            group.MapGet("/", GetAllInvoicesEndpoint.Handle)
                .WithName("GetAllInvoices")
                .WithSummary("Get a page of invoices")
                .WithDescription(
                    "Retrieves a page of invoices with their lines, totals and amounts due, wrapped in the standard " +
                    "paged envelope (items plus page, pageSize, totalCount, totalPages). " +
                    $"page is 1-based and defaults to {PagingParams.FirstPage}. " +
                    $"pageSize defaults to {PagingParams.DefaultPageSize} and is clamped to at most {PagingParams.MaxPageSize}. " +
                    $"sortBy is one of: {string.Join(", ", InvoiceQueries.SortColumns.AllowedColumns)}; anything else returns 400. " +
                    $"sortDirection is one of: {string.Join(", ", SortDirections.Allowed)}; defaults to ascending.")
                .ProducesValidationProblem();

            return group;
        }
    }
}
