using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;

namespace MikeAllan.VerticalSliceArchitecture.Api.Invoices.GetAllInvoices;

/// <remarks>
/// One invoice, not the whole list: the endpoint returns <c>PagedList&lt;GetAllInvoicesResponse&gt;</c>, so the
/// paging envelope is the same shape on every list endpoint.
/// </remarks>
public record GetAllInvoicesResponse(
    Guid Id,
    Guid CustomerId,
    InvoiceStatus Status,
    decimal Total,
    decimal AmountDue,
    IReadOnlyList<GetAllInvoicesResponse.InvoiceLineDto> Lines)
{
    public record InvoiceLineDto(string Description, int Quantity, decimal UnitPrice);
}
