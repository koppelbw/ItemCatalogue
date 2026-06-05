using Application.DTOs;

namespace Application.ServicePorts;

public interface IRoomService
{
    Task<RoomResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResponse<RoomResponse>> GetAllAsync(PaginationQuery pagination, CancellationToken cancellationToken = default);

    Task<RoomResponse> CreateAsync(CreateRoomRequest request, CancellationToken cancellationToken = default);

    Task<RoomResponse> UpdateAsync(UpdateRoomRequest request, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
