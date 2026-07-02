using Domain.Entities;
using Domain.Enums;
using Domain.Pagination;

namespace Domain.RepositoryPorts;

public interface IPictureRepository : IGenericRepository<Picture>
{
    Task<PagedResult<Picture>> GetForOwnerAsync(
        PictureOwnerType ownerType, int ownerId, PageRequest page, CancellationToken cancellationToken = default);

    // Clears IsPrimary on every other picture owned by the same owner, so at most one picture per
    // owner is ever primary. exceptPictureId is the picture that should keep/become primary.
    Task ClearPrimaryForOwnerAsync(
        PictureOwnerType ownerType, int ownerId, int exceptPictureId, CancellationToken cancellationToken = default);
}
