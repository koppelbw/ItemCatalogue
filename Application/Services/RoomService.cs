using Application.DTOs;
using Application.Mapping;
using Application.ServicePorts;
using Domain.RepositoryPorts;

namespace Application.Services;

public sealed class RoomService(IRoomRepository roomRepository) : IRoomService
{
    public async Task<RoomResponse> GetByIdAsync(int id)
    {
        var room = await roomRepository.GetByIdAsync(id)
            ?? throw new InvalidOperationException($"Room with id {id} not found.");

        return room.ToResponse();
    }

    public async Task<IReadOnlyList<RoomResponse>> GetAllAsync()
    {
        var rooms = await roomRepository.GetAllAsync();
        return rooms.Select(r => r.ToResponse()).ToList();
    }

    public async Task<RoomResponse> CreateAsync(CreateRoomRequest request)
    {
        var room = request.ToEntity();
        await roomRepository.InsertAsync(room);
        return room.ToResponse();
    }

    public async Task<RoomResponse> UpdateAsync(UpdateRoomRequest request)
    {
        var room = await roomRepository.GetByIdAsync(request.Id)
            ?? throw new InvalidOperationException($"Room with id {request.Id} not found.");

        request.ApplyTo(room);
        await roomRepository.UpdateAsync(room);
        return room.ToResponse();
    }

    public async Task<int> DeleteAsync(int id)
    {
        return await roomRepository.DeleteAsync(id);
    }
}
