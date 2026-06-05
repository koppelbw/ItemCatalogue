using Application.DTOs;
using Domain.Pagination;

namespace Application.Mapping;

public static class PagedResultMappings
{
    // Project a page of entities to a page of response DTOs, carrying the paging metadata
    // across unchanged. Keeps each service's GetAllAsync a single expression.
    public static PagedResponse<TResponse> ToResponse<TEntity, TResponse>(
        this PagedResult<TEntity> page, Func<TEntity, TResponse> toResponse)
        => new(
            page.Items.Select(toResponse).ToList(),
            page.TotalCount,
            page.Page,
            page.PageSize,
            page.TotalPages,
            page.HasNext,
            page.HasPrevious);
}
