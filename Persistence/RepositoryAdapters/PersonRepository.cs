using Domain.Entities;
using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;

namespace Persistence.RepositoryAdapters;

public sealed class PersonRepository(ItemCatalogueDbContext dbContext) : IPersonRepository
{
    public async Task<Person?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await dbContext.People.FindAsync([id], cancellationToken);
    }

    public async Task<IReadOnlyList<Person>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.People
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<int> InsertAsync(Person person, CancellationToken cancellationToken = default)
    {
        dbContext.People.Add(person);
        await dbContext.SaveChangesAsync(cancellationToken);
        return person.Id;
    }

    public async Task UpdateAsync(Person person, CancellationToken cancellationToken = default)
    {
        dbContext.People.Update(person);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var rowsAffected = await dbContext.People
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"Person with id {id} not found.");
        }

        return rowsAffected;
    }
}
