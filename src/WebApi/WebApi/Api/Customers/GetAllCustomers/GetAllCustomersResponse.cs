namespace MikeAllan.VerticalSliceArchitecture.Api.Customers.GetAllCustomers;

/// <remarks>
/// One customer, not the whole list: the endpoint returns <c>PagedList&lt;GetAllCustomersResponse&gt;</c>, so the
/// paging envelope is the same shape on every list endpoint.
/// </remarks>
public record GetAllCustomersResponse(Guid Id, string Name, string Email, decimal OutstandingBalance);
