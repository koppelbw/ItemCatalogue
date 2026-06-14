using Domain.Entities;

namespace Domain.RepositoryPorts;

public interface ICollectionRepository : IGenericRepository<Collection>
{
    Task<Collection?> GetForUpdateWithItemsAsync(int id, CancellationToken cancellationToken = default);

    Task SaveAsync(CancellationToken cancellationToken = default);
}
