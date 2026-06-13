using Domain.Entities;

namespace Domain.RepositoryPorts;

// Marker port for Container. Inherits the standard CRUD surface from IGenericRepository<Container>;
// add Container-specific methods here if the need arises.
public interface IContainerRepository : IGenericRepository<Container>;
