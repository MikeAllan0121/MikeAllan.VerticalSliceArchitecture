using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;

namespace MikeAllan.VerticalSliceArchitecture.Internals.Api.Customers.GetCustomer;

internal sealed record GetCustomerResponse(
    Guid Id,
    string Name,
    string Email,
    decimal OutstandingBalance,
    IEnumerable<GetCustomerResponse.GetCustomerInvoiceDto> Invoices)
{
    public sealed record GetCustomerInvoiceDto(
        Guid Id,
        InvoiceStatus Status,
        decimal Total,
        decimal AmountDue,
        IEnumerable<GetCustomerInvoiceLineDto> Lines);

    public sealed record GetCustomerInvoiceLineDto(string Description, int Quantity, decimal UnitPrice);
}
