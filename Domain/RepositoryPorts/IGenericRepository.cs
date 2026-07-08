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

    // Inserts every entity in one transaction (single SaveChanges). All-or-nothing at the
    // persistence level: callers wanting partial success filter invalid rows out first.
    Task InsertRangeAsync(IReadOnlyCollection<TEntity> entities, CancellationToken cancellationToken = default);

    // Returns the subset of ids that exist, in one query. Used by bulk operations to pre-check
    // foreign-key references so a dangling id becomes a per-row error instead of a provider
    // exception that would fail the whole batch.
    Task<IReadOnlyList<int>> FilterExistingIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default);

    Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
