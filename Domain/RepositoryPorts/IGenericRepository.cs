using Domain.Entities;
using Domain.Pagination;

namespace Domain.RepositoryPorts;

// Generic persistence port shared by entities with uniform CRUD semantics.
// Entity-specific ports extend this and may add their own methods.
public interface IGenericRepository<TEntity> where TEntity : class, IEntity
{
    Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<TEntity?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResult<TEntity>> GetAllAsync(PageRequest page, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
