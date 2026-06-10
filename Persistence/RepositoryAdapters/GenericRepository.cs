using Domain.Entities;
using Domain.Exceptions;
using Domain.Pagination;
using Domain.RepositoryPorts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Persistence.Logging;

namespace Persistence.RepositoryAdapters;

// Shared CRUD implementation for entities with uniform persistence semantics:
// AsNoTracking reads, tracked load-for-update, rowversion optimistic concurrency,
// and translation of provider errors into domain exceptions. Concrete repositories
// override ReadQuery() to eager-load related data, and may override individual
// methods for entity-specific behaviour.
public abstract class GenericRepository<TEntity>(ItemCatalogueDbContext dbContext, ILoggerFactory loggerFactory) : IGenericRepository<TEntity>
    where TEntity : class, IEntity
{
    protected ItemCatalogueDbContext DbContext { get; } = dbContext;
    protected DbSet<TEntity> EntitySet => DbContext.Set<TEntity>();

    // Lazily built so the category reflects the concrete adapter (e.g. RoomRepository) rather than
    // the generic base, while still being injected once at the base. GetType() needs an instance,
    // so it can't run in a field initializer — hence the lazy property.
    private ILogger? _logger;
    protected ILogger Logger => _logger ??= loggerFactory.CreateLogger(GetType());




    // Read-only query used by GetByIdAsync/GetAllAsync. Override to add Includes.
    // Not tracked, since results are only mapped to responses.
    protected virtual IQueryable<TEntity> ReadQuery()
    {
        return EntitySet.AsNoTracking();
    }

    // async was intentionally left off because the method is a thin pass-through, and adding it would only cost a state-machine allocation for no benefit.
    public virtual Task<TEntity?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return ReadQuery().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public virtual Task<TEntity?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default)
    {
        // Tracked (no AsNoTracking) so SaveChangesAsync emits a minimal, diff-based UPDATE.
        // No Includes: updates only touch the entity's own columns.
        return EntitySet.FirstOrDefaultAsync(e => e.Id == id, cancellationToken);
    }

    public virtual async Task<PagedResult<TEntity>> GetAllAsync(PageRequest page, CancellationToken cancellationToken = default)
    {
        // OFFSET/FETCH requires a deterministic order; Id (the clustered PK) is stable.
        var query = ReadQuery().OrderBy(e => e.Id);

        // Count over the full filtered set, then fetch only the requested window. The COUNT
        // is a separate round trip but keeps the page payload bounded regardless of table size.
        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip(page.Skip)
            .Take(page.PageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<TEntity>(items, totalCount, page.Page, page.PageSize);
    }

    public virtual async Task<int> InsertAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        EntitySet.Add(entity);
        await DbContext.SaveChangesAsync(cancellationToken);
        return entity.Id;
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken cancellationToken = default)
    {
        // entity is already tracked (loaded via GetForUpdateAsync), so no Update() call is needed.
        // Drive the concurrency check off the client's token carried on the entity. EF emits the
        // check as "AND RowVersion = @original" in the UPDATE's WHERE clause.
        DbContext.Entry(entity).Property(nameof(IEntity.RowVersion)).OriginalValue = entity.RowVersion;

        try
        {
            await DbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            Logger.ConcurrencyConflict(typeof(TEntity).Name, entity.Id);
            throw new ConcurrencyConflictException(
                $"{typeof(TEntity).Name} with id {entity.Id} was modified by another process. Reload and try again.", ex);
        }
    }

    public virtual async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
        {
            var rowsAffected = await EntitySet
                .Where(e => e.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            if (rowsAffected == 0)
            {
                throw new InvalidOperationException($"{typeof(TEntity).Name} with id {id} not found.");
            }

            return rowsAffected;
        }
        catch (SqlException ex) when (ex.Number == 547)
        {
            // 547 = FK reference constraint violation. The entity is still referenced by another
            // record under a restricted FK. Translate the provider error into a domain exception
            // so upper layers can map it to HTTP 409 without referencing EF.
            Logger.EntityInUse(typeof(TEntity).Name, id);
            throw new EntityInUseException(
                $"{typeof(TEntity).Name} with id {id} cannot be deleted because it is still referenced by another record.", ex);
        }
    }
}
