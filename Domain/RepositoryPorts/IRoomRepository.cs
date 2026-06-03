using Domain.Entities;

namespace Domain.RepositoryPorts;

// Marker port for Room. Inherits the standard CRUD surface from IGenericRepository<Room>;
// add Room-specific methods here if the need arises.
public interface IRoomRepository : IGenericRepository<Room>;
