using Domain.Entities;
using Domain.Enums;
using Domain.Pagination;
using Domain.Querying;

namespace Domain.RepositoryPorts;

// Item does not extend IGenericRepository<Item>: it exposes a soft delete (SoftDeleteItemByIdAsync)
// rather than the generic hard DeleteAsync, but its read/insert/update surface mirrors the
// generic naming for consistency with the other ports.
public interface IItemRepository
{
    Task<Item?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    // Loads the item with its full location chain eager-loaded (Room/Container → Floor → Location).
    // Handles up to 3 levels of container nesting. Used by the location-path endpoint.
    Task<Item?> GetWithLocationAsync(int id, CancellationToken cancellationToken = default);

    Task<Item?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResult<Item>> GetAllAsync(PageRequest page, CancellationToken cancellationToken = default);

    Task<PagedResult<Item>> SearchAsync(ItemFilter filter, PageRequest page, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(Item item, CancellationToken cancellationToken = default);

    // Inserts every item in one transaction (single SaveChanges); the bulk-import chunk processor
    // relies on this atomicity. Mirrors IGenericRepository<T>.InsertRangeAsync.
    Task InsertRangeAsync(IReadOnlyCollection<Item> items, CancellationToken cancellationToken = default);

    Task UpdateAsync(Item item, CancellationToken cancellationToken = default);

    Task<int> SoftDeleteItemByIdAsync(int id, DeletedReason reason, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Tag>> GetTagsAsync(int itemId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Tag>> SetTagsAsync(int itemId, IReadOnlyCollection<int> tagIds, CancellationToken cancellationToken = default);
}
