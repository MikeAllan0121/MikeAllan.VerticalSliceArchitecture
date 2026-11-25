using Microsoft.AspNetCore.Http.HttpResults;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;

namespace MikeAllan.VerticalSliceArchitecture.Internals.Api.Customers.GetAllCustomers;

internal static class GetAllCustomersEndpoint
{
    internal static async Task<Ok<PagedList<GetAllCustomersResponse>>> Handle(
        [AsParameters] GetAllCustomersRequest request,
        ApplicationDbContext dbContext,
        CancellationToken ct)
    {
        PagedList<GetAllCustomersResponse> customers = await dbContext.Customers
            .SortedBy(request.SortBy, request.ToSortDirection())
            .ToPagedListAsync(
                request.ToPaging(),
                c => new GetAllCustomersResponse(c.Id.Value, c.Name, c.Email, c.OutstandingBalance),
                ct);

        return TypedResults.Ok(customers);
    }
}
