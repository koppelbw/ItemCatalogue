using Domain.Entities;

namespace Domain.RepositoryPorts;

public interface IPersonRepository
{
    Task<Person?> GetByIdAsync(int id);

    Task<IReadOnlyList<Person>> GetAllAsync();

    Task<int> InsertAsync(Person person);

    Task UpdateAsync(Person person);

    Task<int> DeleteAsync(int id);
}
