namespace BlazorUI.Contracts
{
    public sealed record PagedResponse<T>(
        IReadOnlyList<T> Items,
        int TotalCount,
        int Page,
        int PageSize,
        int TotalPages,
        bool HasNext,
        bool HasPrevious);
}