using Microsoft.AspNetCore.Http.HttpResults;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.Internals.ErrorHandling;
using MikeAllan.VerticalSliceArchitecture.Internals.Validation;

namespace MikeAllan.VerticalSliceArchitecture.Internals.Api.Customers.UpdateCustomer;

internal static class UpdateCustomerEndpoint
{
    internal static async Task<Results<NoContent, ProblemHttpResult>> Handle(
        [NotEmptyGuid] Guid customerId,
        UpdateCustomerRequest request,
        ApplicationDbContext dbContext,
        CancellationToken ct)
    {
        Customer? customer = await dbContext.Customers
            .ById(CustomerId.From(customerId))
            .FirstOrDefaultAsync(ct);

        if (customer is null)
            return CustomerErrors.NotFound.ToProblem();

        customer.Name = request.Name;
        customer.Email = request.Email;

        await dbContext.SaveChangesAsync(ct);

        return TypedResults.NoContent();
    }
}
