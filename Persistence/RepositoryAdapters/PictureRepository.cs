using Domain.Entities;
using Domain.Enums;
using Domain.Pagination;
using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Persistence.RepositoryAdapters;

public sealed class PictureRepository(ItemCatalogueDbContext dbContext, ILoggerFactory loggerFactory)
    : GenericRepository<Picture>(dbContext, loggerFactory), IPictureRepository
{
    public Task<PagedResult<Picture>> GetForOwnerAsync(
        PictureOwnerType ownerType, int ownerId, PageRequest page, CancellationToken cancellationToken = default)
        => PaginateAsync(
            OwnerFilter(ReadQuery(), ownerType, ownerId).OrderBy(p => p.SortOrder).ThenBy(p => p.Id),
            page, cancellationToken);

    public Task ClearPrimaryForOwnerAsync(
        PictureOwnerType ownerType, int ownerId, int exceptPictureId, CancellationToken cancellationToken = default)
        => OwnerFilter(EntitySet, ownerType, ownerId)
            .Where(p => p.Id != exceptPictureId && p.IsPrimary)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.IsPrimary, false), cancellationToken);

    private static IQueryable<Picture> OwnerFilter(IQueryable<Picture> query, PictureOwnerType ownerType, int ownerId)
        => ownerType switch
        {
            PictureOwnerType.Location => query.Where(p => p.LocationId == ownerId),
            PictureOwnerType.Room => query.Where(p => p.RoomId == ownerId),
            PictureOwnerType.Container => query.Where(p => p.ContainerId == ownerId),
            PictureOwnerType.Item => query.Where(p => p.ItemId == ownerId),
            _ => throw new ArgumentOutOfRangeException(nameof(ownerType), ownerType, null),
        };
}
