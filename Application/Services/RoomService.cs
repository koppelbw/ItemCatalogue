using Application.DTOs;
using Application.Mapping;
using Application.ServicePorts;
using Domain.Exceptions;
using Domain.Pagination;
using Domain.RepositoryPorts;

namespace Application.Services;

public sealed class RoomService(IRoomRepository roomRepository) : IRoomService
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
        var room = request.ToEntity();
        await roomRepository.InsertAsync(room, cancellationToken);
        return room.ToResponse();
    }

    public async Task<RoomResponse> UpdateAsync(UpdateRoomRequest request, CancellationToken cancellationToken = default)
    {
        var room = await roomRepository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Room", request.Id);

        request.ApplyTo(room);
        await roomRepository.UpdateAsync(room, cancellationToken);
        return room.ToResponse();
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await roomRepository.DeleteAsync(id, cancellationToken);
    }
}
