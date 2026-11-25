using System.ComponentModel.DataAnnotations;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Internals.Validation;

namespace MikeAllan.VerticalSliceArchitecture.Internals.Api.Invoices.CreateInvoice;

/// <remarks>
/// <c>Lines</c> is <see cref="RequiredAttribute"/> because the endpoint enumerates it unconditionally:
/// without it a body omitting <c>lines</c> is a 500, not a 400. It may be empty: a draft can start with no
/// lines. Each line is validated too.
/// </remarks>
internal sealed record CreateInvoiceRequest(
    [property: NotEmptyGuid] Guid CustomerId,
    [property: Required] IEnumerable<CreateInvoiceRequest.InvoiceLineDto> Lines)
{
    // Decimal limits are strings parsed in the invariant culture, so "0.01" means the same on every server.
    public sealed record InvoiceLineDto(
        [property: Required, MaxLength(InvoiceLine.DescriptionMaxLength)] string Description,
        [property: Range(InvoiceLine.QuantityMin, InvoiceLine.QuantityMax)] int Quantity,
        [property: Range(typeof(decimal), "0.01", "1000000",
            ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true),
            MaxDecimalPlaces(InvoiceLine.UnitPriceDecimalPlaces)] decimal UnitPrice);
}
