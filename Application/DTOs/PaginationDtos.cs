using System.ComponentModel.DataAnnotations;
using Domain.Pagination;

namespace Application.DTOs;

public record PaginationQuery
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, PageRequest.MaxPageSize)]
    public int PageSize { get; init; } = PageRequest.DefaultPageSize;
}

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages,
    bool HasNext,
    bool HasPrevious);
