namespace Domain.Pagination;

// A single page of results plus the total matching row count, so callers can render
// paging UI ("page X of Y") without issuing a separate count request.
public sealed record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize)
{
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling(TotalCount / (double)PageSize) : 0;

    public bool HasPrevious => Page > 1;

    public bool HasNext => Page < TotalPages;
}
