using Domain.Entities;
using Domain.RepositoryPorts;
using Microsoft.Extensions.Logging;

namespace Persistence.RepositoryAdapters;

public sealed class TagRepository(ItemCatalogueDbContext dbContext, ILoggerFactory loggerFactory)
    : GenericRepository<Tag>(dbContext, loggerFactory), ITagRepository;
