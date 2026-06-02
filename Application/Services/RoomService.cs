using Application.DTOs;
using Application.Mapping;
using Application.ServicePorts;
using Domain.RepositoryPorts;

namespace Application.Services;

public sealed class RoomService(IRoomRepository roomRepository) : IRoomService
{
    public async Task<RoomResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var room = await roomRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new InvalidOperationException($"Room with id {id} not found.");

        return room.ToResponse();
    }

    public async Task<IReadOnlyList<RoomResponse>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rooms = await roomRepository.GetAllAsync(cancellationToken);
        return rooms.Select(r => r.ToResponse()).ToList();
    }

    public async Task<RoomResponse> CreateAsync(CreateRoomRequest request, CancellationToken cancellationToken = default)
    {
        var room = request.ToEntity();
        await roomRepository.InsertAsync(room, cancellationToken);
        return room.ToResponse();
    }

    public async Task<RoomResponse> UpdateAsync(UpdateRoomRequest request, CancellationToken cancellationToken = default)
    {
        var room = await roomRepository.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Room with id {request.Id} not found.");

        request.ApplyTo(room);
        await roomRepository.UpdateAsync(room, cancellationToken);
        return room.ToResponse();
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        return await roomRepository.DeleteAsync(id, cancellationToken);
    }
}
