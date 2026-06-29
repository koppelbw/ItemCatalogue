using Domain.Entities;
using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Persistence.RepositoryAdapters;

public sealed class LocationRepository(ItemCatalogueDbContext dbContext, ILoggerFactory loggerFactory)
    : GenericRepository<Location>(dbContext, loggerFactory), ILocationRepository
{
    protected override IQueryable<Location> ReadQuery()
        => EntitySet.Include(l => l.Floors).AsNoTracking();

    public async Task<Location?> GetMapAsync(int id, CancellationToken cancellationToken = default)
    {
        // Load the building skeleton: Floors -> Rooms, with each room's Doors and its top-level
        // Containers. Tracked (not AsNoTracking) on purpose so the iterative child-container loads
        // below are wired into each Container.Children navigation by EF relationship fixup — which
        // only happens for tracked entities sharing this context.
        var location = await EntitySet
            .Include(l => l.Floors)
                .ThenInclude(f => f.Rooms)
                    .ThenInclude(r => r.Doors)
            .Include(l => l.Floors)
                .ThenInclude(f => f.Rooms)
                    .ThenInclude(r => r.Stairs)
            .Include(l => l.Floors)
                .ThenInclude(f => f.Rooms)
                    .ThenInclude(r => r.Containers)
            .FirstOrDefaultAsync(l => l.Id == id, cancellationToken);

        if (location is null)
        {
            return null;
        }

        // Walk the container forest breadth-first, loading each level's children into the same
        // context. Containers are a tree rooted at rooms; descendants carry ParentContainerId (not
        // RoomId) so they cannot be reached by room and must be fetched level by level.
        var floorContents = location.Floors
            .SelectMany(f => f.Rooms)
            .SelectMany(r => r.Containers)
            .Select(c => c.Id)
            .ToList();

        while (floorContents.Count > 0)
        {
            var children = await DbContext.Containers
                .Where(c => c.ParentContainerId != null && floorContents.Contains(c.ParentContainerId.Value))
                .ToListAsync(cancellationToken);

            floorContents = children.Select(c => c.Id).ToList();
        }

        return location;
    }
}
