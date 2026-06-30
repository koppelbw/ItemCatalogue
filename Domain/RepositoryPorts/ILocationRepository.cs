using Domain.Entities;

namespace Domain.RepositoryPorts;

public interface ILocationRepository : IGenericRepository<Location>
{
    // Loads the full spatial graph for one Location — Floors, their Rooms (with geometry/colours),
    // each room's Containers (the complete nested tree) and Doors — for a consumer to reconstruct the
    // whole building in a single call. Read-only; returns null when the Location does not exist.
    Task<Location?> GetMapAsync(int id, CancellationToken cancellationToken = default);
}
