using Domain.Entities;
using Domain.RepositoryPorts;

namespace Persistence.RepositoryAdapters;

public sealed class RoomRepository(ItemCatalogueDbContext dbContext)
    : GenericRepository<Room>(dbContext), IRoomRepository;
