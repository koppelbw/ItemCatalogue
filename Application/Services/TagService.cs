using Application.DTOs;
using Application.Logging;
using Application.Mapping;
using Application.ServicePorts;
using Domain.Exceptions;
using Domain.Pagination;
using Domain.RepositoryPorts;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public sealed class TagService(
    ITagRepository tagRepository,
    IValidator<CreateTagRequest> createValidator,
    IValidator<UpdateTagRequest> updateValidator,
    ILogger<TagService> logger) : ITagService
{
    public async Task<TagResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var tag = await tagRepository.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Tag", id);

        return tag.ToResponse();
    }

    public async Task<PagedResponse<TagResponse>> GetAllAsync(PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        var page = await tagRepository.GetAllAsync(PageRequest.Create(pagination.Page, pagination.PageSize), cancellationToken);
        return page.ToResponse(t => t.ToResponse());
    }

    public async Task<TagResponse> CreateAsync(CreateTagRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var tag = request.ToEntity();
        await tagRepository.InsertAsync(tag, cancellationToken);
        logger.EntityCreated("Tag", tag.Id);
        return tag.ToResponse();
    }

    public async Task<TagResponse> UpdateAsync(UpdateTagRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var tag = await tagRepository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Tag", request.Id);

        request.ApplyTo(tag);
        await tagRepository.UpdateAsync(tag, cancellationToken);
        logger.EntityUpdated("Tag", tag.Id);
        return tag.ToResponse();
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var rowsAffected = await tagRepository.DeleteAsync(id, cancellationToken);
        logger.EntityDeleted("Tag", id, rowsAffected);
        return rowsAffected;
    }
}
