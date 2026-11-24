using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;

namespace MikeAllan.VerticalSliceArchitecture.Domain.Customers;

/// <summary>
/// The queries over customers, as extension members on <see cref="IQueryable{T}"/>:
/// <c>dbContext.Customers.ById(id)</c>.
/// </summary>
/// <remarks>
/// Each returns a query rather than results, so the caller composes it with what it needs next
/// (<c>FirstOrDefaultAsync</c>, a projection, <c>ToPagedListAsync</c>) and EF Core translates the whole
/// chain as one statement. Every query over the aggregate lives here, beside it, so there is one place to
/// look before writing another.
/// </remarks>
public static class CustomerQueryableExtensions
{
    extension(IQueryable<Customer> customers)
    {
        /// <summary>
        /// The customer with <paramref name="customerId"/>, with their invoices, which
        /// <see cref="Customer.RecalculateOutstandingBalance"/> needs. Empty when there is none.
        /// </summary>
        public IQueryable<Customer> ById(CustomerId customerId) =>
            customers
                .Where(c => c.Id == customerId)
                .Include(c => c.Invoices);

        /// <summary>
        /// Customers in the order the caller asked for, then by <c>Id</c>, the tie-breaker that keeps paging stable.
        /// </summary>
        /// <remarks>
        /// No <c>Include</c>, unlike <see cref="ById"/>: the list endpoint projects into a DTO, and EF Core
        /// discards includes under a projection, so they would cost a join per page and buy nothing.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="sortBy"/> is not one of <see cref="CustomerQueries.SortColumns"/>.
        /// </exception>
        public IOrderedQueryable<Customer> SortedBy(string? sortBy, SortDirection direction) =>
            CustomerQueries.SortColumns.Apply(customers, sortBy, direction, c => c.Id);
    }
}
