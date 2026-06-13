using Application.DTOs;

namespace Application.ServicePorts;

public interface IItemEventService
{
    Task<IReadOnlyList<ItemEventResponse>> GetByItemIdAsync(int itemId, CancellationToken cancellationToken = default);
}
