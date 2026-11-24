using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;

namespace MikeAllan.VerticalSliceArchitecture.Api.Customers.GetCustomer;

public record GetCustomerResponse(
    Guid Id,
    string Name,
    string Email,
    decimal OutstandingBalance,
    IEnumerable<GetCustomerResponse.GetCustomerInvoiceDto> Invoices)
{
    public record GetCustomerInvoiceDto(
        Guid Id,
        InvoiceStatus Status,
        decimal Total,
        decimal AmountDue,
        IEnumerable<GetCustomerInvoiceLineDto> Lines);

    public record GetCustomerInvoiceLineDto(string Description, int Quantity, decimal UnitPrice);
}
