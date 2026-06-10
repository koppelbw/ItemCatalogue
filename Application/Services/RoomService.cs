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

public sealed class RoomService(
    IRoomRepository roomRepository,
    IValidator<CreateRoomRequest> createValidator,
    IValidator<UpdateRoomRequest> updateValidator,
    ILogger<RoomService> logger) : IRoomService
{
    public async Task<RoomResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var room = await roomRepository.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Room", id);

        return room.ToResponse();
    }

    public async Task<PagedResponse<RoomResponse>> GetAllAsync(PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        var page = await roomRepository.GetAllAsync(PageRequest.Create(pagination.Page, pagination.PageSize), cancellationToken);
        return page.ToResponse(r => r.ToResponse());
    }

    public async Task<RoomResponse> CreateAsync(CreateRoomRequest request, CancellationToken cancellationToken = default)
    {
        await createValidator.ValidateAndThrowAsync(request, cancellationToken);

        var room = request.ToEntity();
        await roomRepository.InsertAsync(room, cancellationToken);
        logger.EntityCreated("Room", room.Id);
        return room.ToResponse();
    }

    public async Task<RoomResponse> UpdateAsync(UpdateRoomRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var room = await roomRepository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Room", request.Id);

        request.ApplyTo(room);
        await roomRepository.UpdateAsync(room, cancellationToken);
        logger.EntityUpdated("Room", room.Id);
        return room.ToResponse();
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var rowsAffected = await roomRepository.DeleteAsync(id, cancellationToken);
        logger.EntityDeleted("Room", id, rowsAffected);
        return rowsAffected;
    }
}
