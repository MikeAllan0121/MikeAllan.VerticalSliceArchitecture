using Microsoft.AspNetCore.Http.HttpResults;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;
using MikeAllan.VerticalSliceArchitecture.ErrorHandling;
using MikeAllan.VerticalSliceArchitecture.Validation;

namespace MikeAllan.VerticalSliceArchitecture.Api.Customers.GetCustomer;

public static class GetCustomerEndpoint
{
    internal static async Task<Results<Ok<GetCustomerResponse>, ProblemHttpResult>> Handle(
        [NotEmptyGuid] Guid customerId,
        ApplicationDbContext dbContext,
        CancellationToken ct)
    {
        CustomerId id = CustomerId.From(customerId);

        // AmountDue is not mapped, so the projection spells it out as Total - AmountPaid.
        GetCustomerResponse? customer = await dbContext.Customers
            .Where(c => c.Id == id)
            .Select(c => new GetCustomerResponse(
                c.Id.Value,
                c.Name,
                c.Email,
                c.OutstandingBalance,
                c.Invoices.Select(
                    i => new GetCustomerResponse.GetCustomerInvoiceDto(
                        i.Id.Value,
                        i.Status,
                        i.Total,
                        i.Total - i.AmountPaid,
                        i.Lines.Select(l => new GetCustomerResponse.GetCustomerInvoiceLineDto(l.Description, l.Quantity, l.UnitPrice))
                    )
                )))
            .FirstOrDefaultAsync(ct);

        if (customer is null)
            return CustomerErrors.NotFound.ToProblem();

        return TypedResults.Ok(customer);
    }
}
