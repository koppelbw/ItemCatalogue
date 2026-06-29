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

    Task<Item?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    Task<PagedResult<Item>> GetAllAsync(PageRequest page, CancellationToken cancellationToken = default);

    Task<PagedResult<Item>> SearchAsync(ItemFilter filter, PageRequest page, CancellationToken cancellationToken = default);

    Task<int> InsertAsync(Item item, CancellationToken cancellationToken = default);

    Task UpdateAsync(Item item, CancellationToken cancellationToken = default);

    Task<int> SoftDeleteItemByIdAsync(int id, DeletedReason reason, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Tag>> GetTagsAsync(int itemId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Tag>> SetTagsAsync(int itemId, IReadOnlyCollection<int> tagIds, CancellationToken cancellationToken = default);
}
