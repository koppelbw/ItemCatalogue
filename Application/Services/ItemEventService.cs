using Application.DTOs;
using Application.Mapping;
using Application.ServicePorts;
using Domain.RepositoryPorts;

namespace Application.Services;

public sealed class ItemEventService(IItemEventRepository itemEventRepository) : IItemEventService
{
    public async Task<IReadOnlyList<ItemEventResponse>> GetByItemIdAsync(int itemId, CancellationToken cancellationToken = default)
    {
        var events = await itemEventRepository.GetByItemIdAsync(itemId, cancellationToken);
        return events.Select(e => e.ToResponse()).ToList();
    }
}
