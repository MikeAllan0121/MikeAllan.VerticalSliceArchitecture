using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;

namespace MikeAllan.VerticalSliceArchitecture.Domain.Invoices;

/// <summary>
/// The queries over invoices, as extension members on <see cref="IQueryable{T}"/>:
/// <c>dbContext.Invoices.ById(id)</c>.
/// </summary>
/// <remarks>
/// Each returns a query rather than results, so the caller composes it with what it needs next and EF Core
/// translates the whole chain as one statement. See <c>CustomerQueryableExtensions</c> for the shape.
/// </remarks>
public static class InvoiceQueryableExtensions
{
    extension(IQueryable<Invoice> invoices)
    {
        /// <summary>
        /// The invoice with <paramref name="invoiceId"/>, with its payments: the whole aggregate, ready to
        /// change. Its lines are an owned JSON collection, so they come with it. Empty when there is none.
        /// </summary>
        public IQueryable<Invoice> ById(InvoiceId invoiceId) =>
            invoices
                .Where(i => i.Id == invoiceId)
                .Include(i => i.Payments);

        /// <summary>
        /// Invoices in the order the caller asked for, then by <c>Id</c>, the tie-breaker that keeps paging stable.
        /// </summary>
        /// <remarks>
        /// No <c>Include</c>, unlike <see cref="ById"/>: the list endpoint projects into a DTO, and EF Core
        /// discards includes under a projection, so they would cost a join per page and buy nothing.
        /// </remarks>
        /// <exception cref="ArgumentOutOfRangeException">
        /// <paramref name="sortBy"/> is not one of <see cref="InvoiceQueries.SortColumns"/>.
        /// </exception>
        public IOrderedQueryable<Invoice> SortedBy(string? sortBy, SortDirection direction) =>
            InvoiceQueries.SortColumns.Apply(invoices, sortBy, direction, i => i.Id);
    }
}
