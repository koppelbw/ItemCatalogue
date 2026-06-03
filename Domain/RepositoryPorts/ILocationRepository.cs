using Domain.Entities;

namespace Domain.RepositoryPorts;

// Marker port for Location. Inherits the standard CRUD surface from IGenericRepository<Location>;
// add Location-specific methods here if the need arises.
public interface ILocationRepository : IGenericRepository<Location>;
