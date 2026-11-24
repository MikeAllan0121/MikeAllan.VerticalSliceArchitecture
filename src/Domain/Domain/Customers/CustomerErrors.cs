namespace MikeAllan.VerticalSliceArchitecture.Domain.Customers;

// Entries are typed DomainError, not the leaf record, so each one converts straight to Result and Result<T>.
public static class CustomerErrors
{
    public static readonly DomainError NotFound = new NotFoundError(
        "Customer.NotFound",
        "Customer is not found");
}
