using System.ComponentModel.DataAnnotations;
using MikeAllan.VerticalSliceArchitecture.Domain.Invoices;
using MikeAllan.VerticalSliceArchitecture.Validation;

namespace MikeAllan.VerticalSliceArchitecture.Api.Invoices.RecordPayment;

/// <summary>
/// The body of <c>POST /api/invoices/{invoiceId}/payments</c>. The invoice's ID comes from the route.
/// </summary>
/// <remarks>
/// The upper limit is the largest value a <c>numeric(18, 2)</c> column holds; a payment above the amount
/// due is the domain's call (a 409), not validation's.
/// </remarks>
public sealed record RecordPaymentRequest(
    [property: Range(typeof(decimal), "0.01", "9999999999999999.99",
        ParseLimitsInInvariantCulture = true, ConvertValueInInvariantCulture = true),
        MaxDecimalPlaces(Payment.AmountDecimalPlaces)] decimal Amount);
