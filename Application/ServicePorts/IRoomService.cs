using Application.DTOs;

namespace Application.ServicePorts;

public interface IRoomService
{
    Task<RoomResponse> GetByIdAsync(int id);

    Task<IReadOnlyList<RoomResponse>> GetAllAsync();

    Task<RoomResponse> CreateAsync(CreateRoomRequest request);

    Task<RoomResponse> UpdateAsync(UpdateRoomRequest request);

    Task<int> DeleteAsync(int id);
}
