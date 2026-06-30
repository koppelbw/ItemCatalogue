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

public sealed class StairService(
    IStairRepository stairRepository,
    IValidator<CreateStairRequest> createValidator,
    IValidator<UpdateStairRequest> updateValidator,
    ILogger<StairService> logger) : IStairService
{
    public async Task<StairResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var stair = await stairRepository.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Stair", id);

        return stair.ToResponse();
    }

    public async Task<PagedResponse<StairResponse>> GetAllAsync(PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        var page = await stairRepository.GetAllAsync(PageRequest.Create(pagination.Page, pagination.PageSize), cancellationToken);
        return page.ToResponse(s => s.ToResponse());
    }

    public async Task<StairResponse> CreateAsync(CreateStairRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var stair = request.ToEntity();
        await stairRepository.InsertAsync(stair, cancellationToken);
        logger.EntityCreated("Stair", stair.Id);
        return stair.ToResponse();
    }

    public async Task<StairResponse> UpdateAsync(UpdateStairRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var stair = await stairRepository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Stair", request.Id);

        request.ApplyTo(stair);
        await stairRepository.UpdateAsync(stair, cancellationToken);
        logger.EntityUpdated("Stair", stair.Id);
        return stair.ToResponse();
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var rowsAffected = await stairRepository.DeleteAsync(id, cancellationToken);
        logger.EntityDeleted("Stair", id, rowsAffected);
        return rowsAffected;
    }
}
