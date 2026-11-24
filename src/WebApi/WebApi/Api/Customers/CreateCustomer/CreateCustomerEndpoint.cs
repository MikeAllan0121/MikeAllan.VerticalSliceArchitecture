using Microsoft.AspNetCore.Http.HttpResults;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;

namespace MikeAllan.VerticalSliceArchitecture.Api.Customers.CreateCustomer;

public static class CreateCustomerEndpoint
{
    internal static async Task<Ok<CreateCustomerResponse>> Handle(
        CreateCustomerRequest request,
        ApplicationDbContext dbContext,
        CancellationToken ct)
    {
        Customer customer = Customer.Create(request.Name, request.Email);

        dbContext.Customers.Add(customer);
        await dbContext.SaveChangesAsync(ct);

        return TypedResults.Ok(new CreateCustomerResponse(customer.Id.Value));
    }
}
