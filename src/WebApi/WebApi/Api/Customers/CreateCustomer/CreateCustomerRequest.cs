using System.ComponentModel.DataAnnotations;
using MikeAllan.VerticalSliceArchitecture.Domain.Customers;

namespace MikeAllan.VerticalSliceArchitecture.Api.Customers.CreateCustomer;

/// <remarks>
/// The domain guards only blank and length, so the address format is checked here, at the boundary.
/// </remarks>
public sealed record CreateCustomerRequest(
    [property: Required, MaxLength(Customer.NameMaxLength)] string Name,
    [property: Required, MaxLength(Customer.EmailMaxLength), EmailAddress] string Email);
