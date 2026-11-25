using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using MikeAllan.VerticalSliceArchitecture.Domain.Pagination;

namespace MikeAllan.VerticalSliceArchitecture.Internals.Pagination;

/// <summary>
/// The query-string contract shared by every paged list endpoint:
/// <c>?page=1&amp;pageSize=10&amp;sortBy=name&amp;sortDirection=asc</c>.
/// </summary>
/// <remarks>
/// Slice requests inherit this (bound with <c>[AsParameters]</c>) so the parameter names stay identical
/// across features. Values are the caller's raw input — run them through <see cref="ToPaging"/> and
/// <see cref="ToSortDirection"/> before building a query.
/// <para>
/// Every property is nullable, including the numbers: a non-nullable <c>int</c> with an initialiser looks
/// like a required parameter to OpenAPI, and the defaults belong in <see cref="PagingParams"/> anyway.
/// </para>
/// <para>
/// No page or page-size rules: those are clamped by <see cref="PagingParams.From"/>, so there is nothing
/// left to reject. Sort inputs are the opposite — an unknown column has no sensible interpretation, and
/// answering it with a 400 that lists the allowed columns beats guessing. <see cref="Validate"/> runs
/// before the handler, so <see cref="SortColumnMap{T}"/> never sees a column it would throw on.
/// </para>
/// </remarks>
/// <typeparam name="TEntity">The aggregate being paged; its queries class supplies <see cref="SortColumns"/>.</typeparam>
internal abstract record PagedRequest<TEntity> : IValidatableObject
{
    // Names pinned so the OpenAPI document (and any generated client) uses the camelCase contract.
    [FromQuery(Name = "page")]
    public int? Page { get; init; }

    [FromQuery(Name = "pageSize")]
    public int? PageSize { get; init; }

    [FromQuery(Name = "sortBy")]
    public string? SortBy { get; init; }

    [FromQuery(Name = "sortDirection")]
    public string? SortDirection { get; init; }

    /// <summary>The aggregate's sort allow-list, e.g. <c>CustomerQueries.SortColumns</c>.</summary>
    protected abstract SortColumnMap<TEntity> SortColumns { get; }

    public PagingParams ToPaging() => PagingParams.From(Page, PageSize);

    public SortDirection ToSortDirection() => SortDirections.From(SortDirection);

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!SortColumns.IsAllowed(SortBy))
        {
            yield return new ValidationResult(
                $"'{SortBy}' is not a sortable column. Allowed values: {string.Join(", ", SortColumns.AllowedColumns)}.",
                [nameof(SortBy)]);
        }

        if (!SortDirections.IsAllowed(SortDirection))
        {
            yield return new ValidationResult(
                $"'{SortDirection}' is not a valid sort direction. Allowed values: {string.Join(", ", SortDirections.Allowed)}.",
                [nameof(SortDirection)]);
        }
    }
}
