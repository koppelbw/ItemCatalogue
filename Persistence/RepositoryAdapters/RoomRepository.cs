using Domain.Entities;
using Domain.RepositoryPorts;
using Microsoft.Extensions.Logging;

namespace Persistence.RepositoryAdapters;

public sealed class RoomRepository(ItemCatalogueDbContext dbContext, ILoggerFactory loggerFactory)
    : GenericRepository<Room>(dbContext, loggerFactory), IRoomRepository;
