using System.Linq.Expressions;

namespace MikeAllan.VerticalSliceArchitecture.Domain.Pagination;

public static class OrderedQueryableExtensions
{
    extension<TEntity>(IOrderedQueryable<TEntity> source)
    {
        /// <summary>
        /// Runs one page of an ordered query and the query's total count, and projects the page into
        /// <typeparamref name="TResult"/>.
        /// </summary>
        /// <remarks>
        /// The receiver is an <see cref="IOrderedQueryable{T}"/> so an unordered query can't be paged at all:
        /// <c>OFFSET</c>/<c>FETCH</c> over unordered rows has no stable order, and the same row could land on two
        /// pages. Order it with the aggregate's <c>SortedBy(...)</c>, which adds the <c>Id</c> tie-breaker.
        /// <para>
        /// Two queries over one source. The count runs before <c>Skip</c>/<c>Take</c>, so
        /// <see cref="PagedList{T}.TotalCount"/> describes the whole result set, and a filter added to the
        /// source applies to both. The window is applied here, from the same <paramref name="paging"/> the
        /// envelope reports, so the body can't describe a page the query didn't fetch.
        /// </para>
        /// </remarks>
        public async Task<PagedList<TResult>> ToPagedListAsync<TResult>(
            PagingParams paging,
            Expression<Func<TEntity, TResult>> projection,
            CancellationToken ct)
        {
            int totalCount = await source.CountAsync(ct);

            List<TResult> items = await source
                .Skip(paging.Skip)
                .Take(paging.PageSize)
                .Select(projection)
                .ToListAsync(ct);

            return new PagedList<TResult>(items, paging.Page, paging.PageSize, totalCount);
        }
    }
}
