using Domain.Entities;
using Domain.RepositoryPorts;
using Microsoft.Extensions.Logging;

namespace Persistence.RepositoryAdapters;

public sealed class PersonRepository(ItemCatalogueDbContext dbContext, ILoggerFactory loggerFactory)
    : GenericRepository<Person>(dbContext, loggerFactory), IPersonRepository;
