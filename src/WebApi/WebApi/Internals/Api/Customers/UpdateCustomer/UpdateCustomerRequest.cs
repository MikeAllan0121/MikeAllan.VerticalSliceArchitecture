using System.ComponentModel.DataAnnotations;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;

namespace MikeAllan.VerticalSliceArchitecture.Internals.Api.Customers.UpdateCustomer;

/// <summary>
/// The body of <c>PUT /api/customers/{customerId}</c>. The customer's ID comes from the route, not the body.
/// </summary>
/// <remarks>
/// The domain guards only blank and length, so the address format is checked here, at the boundary.
/// </remarks>
internal sealed record UpdateCustomerRequest(
    [property: Required, MaxLength(Customer.NameMaxLength)] string Name,
    [property: Required, MaxLength(Customer.EmailMaxLength), EmailAddress] string Email);
