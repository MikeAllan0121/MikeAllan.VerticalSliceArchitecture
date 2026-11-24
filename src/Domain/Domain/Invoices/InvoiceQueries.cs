using System.Linq.Expressions;
using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;

namespace MikeAllan.VerticalSliceArchitecture.Domain.Invoices;

/// <summary>
/// What a caller may sort invoices by. The queries themselves are the extension members in
/// <see cref="InvoiceQueryableExtensions"/>: <c>dbContext.Invoices.ById(id)</c>.
/// </summary>
public static class InvoiceQueries
{
    /// <summary>
    /// The columns a caller may sort invoices by. Lives with the aggregate because mapping a query-string
    /// name to an ordering expression is knowledge of the entity, not of any one slice.
    /// </summary>
    /// <remarks>
    /// <c>amountDue</c> is written out as <c>Total - AmountPaid</c>: <see cref="Invoice.AmountDue"/> is not
    /// mapped, so EF Core can't translate it.
    /// </remarks>
    public static SortColumnMap<Invoice> SortColumns { get; } = new(
        defaultColumn: "createdAt",
        columns: new Dictionary<string, Expression<Func<Invoice, object?>>>
        {
            ["createdAt"] = i => i.CreatedAt,
            ["total"] = i => i.Total,
            ["amountDue"] = i => i.Total - i.AmountPaid
        });
}
