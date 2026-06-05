using System.ComponentModel.DataAnnotations;
using Domain.Pagination;

namespace Application.DTOs;

// Query-string binding model for paginated list endpoints (e.g. ?page=2&pageSize=50).
// The Range attributes surface obviously-invalid input as 400s at the API boundary;
// PageRequest.Create still clamps server-side so the repository can never be handed
// out-of-range values regardless of how it is called.
public sealed record PaginationQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, PageRequest.MaxPageSize)]
    public int PageSize { get; init; } = PageRequest.DefaultPageSize;
}

// Transport shape for a single page of response DTOs. Mirrors Domain.PagedResult but
// flattens the computed metadata so clients get it without recomputation.
public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    bool HasNext,
    bool HasPrevious);
