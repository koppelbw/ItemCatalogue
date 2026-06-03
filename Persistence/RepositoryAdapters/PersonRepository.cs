using Domain.Entities;
using Domain.RepositoryPorts;

namespace Persistence.RepositoryAdapters;

public sealed class PersonRepository(ItemCatalogueDbContext dbContext)
    : GenericRepository<Person>(dbContext), IPersonRepository;
