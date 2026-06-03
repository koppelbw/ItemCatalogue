using Domain.Entities;
using Domain.Enums;

namespace Domain.RepositoryPorts;

// Item does not extend IGenericRepository<Item>: it exposes a soft delete (SoftDeleteItemByIdAsync)
// rather than the generic hard DeleteAsync, but its read/insert/update surface mirrors the
// generic naming for consistency with the other ports.
public interface IItemRepository
{
    Task<Item?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<Item?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Item>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<int> InsertAsync(Item item, CancellationToken cancellationToken = default);

    Task UpdateAsync(Item item, CancellationToken cancellationToken = default);

    Task<int> SoftDeleteItemByIdAsync(int id, DeletedReason reason, CancellationToken cancellationToken = default);
}
