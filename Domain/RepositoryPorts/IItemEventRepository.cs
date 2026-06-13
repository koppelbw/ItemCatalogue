using Domain.Entities;

namespace Domain.RepositoryPorts;

public interface IItemEventRepository
{
    Task InsertAsync(ItemEvent itemEvent, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ItemEvent>> GetByItemIdAsync(int itemId, CancellationToken cancellationToken = default);
}
