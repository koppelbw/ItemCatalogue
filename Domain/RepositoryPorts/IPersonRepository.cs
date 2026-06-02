using Domain.Entities;

namespace Domain.RepositoryPorts;

public interface IPersonRepository
{
    Task<Person?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Person>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<int> InsertAsync(Person person, CancellationToken cancellationToken = default);

    Task UpdateAsync(Person person, CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default);
}
