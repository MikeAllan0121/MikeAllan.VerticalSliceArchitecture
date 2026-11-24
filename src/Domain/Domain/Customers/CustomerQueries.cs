using System.Linq.Expressions;
using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;

namespace MikeAllan.VerticalSliceArchitecture.Domain.Customers;

/// <summary>
/// What a caller may sort customers by. The queries themselves are the extension members in
/// <see cref="CustomerQueryableExtensions"/>: <c>dbContext.Customers.ById(id)</c>.
/// </summary>
public static class CustomerQueries
{
    /// <summary>
    /// The columns a caller may sort customers by. Lives with the aggregate because mapping a query-string
    /// name to an ordering expression is knowledge of the entity, not of any one slice.
    /// </summary>
    public static SortColumnMap<Customer> SortColumns { get; } = new(
        defaultColumn: "name",
        columns: new Dictionary<string, Expression<Func<Customer, object?>>>
        {
            ["name"] = c => c.Name,
            ["email"] = c => c.Email,
            ["outstandingBalance"] = c => c.OutstandingBalance
        });
}
