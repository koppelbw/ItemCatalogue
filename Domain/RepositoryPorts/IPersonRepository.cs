using Domain.Entities;

namespace Domain.RepositoryPorts;

// Marker port for Person. Inherits the standard CRUD surface from IGenericRepository<Person>;
// add Person-specific methods here if the need arises.
public interface IPersonRepository : IGenericRepository<Person>;
