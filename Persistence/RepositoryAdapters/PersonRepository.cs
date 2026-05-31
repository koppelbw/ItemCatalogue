using Domain.Entities;
using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;

namespace Persistence.RepositoryAdapters;

public sealed class PersonRepository(ItemCatalogueDbContext dbContext) : IPersonRepository
{
    public async Task<Person?> GetByIdAsync(int id)
    {
        return await dbContext.People.FindAsync(id);
    }

    public async Task<IReadOnlyList<Person>> GetAllAsync()
    {
        return await dbContext.People
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> InsertAsync(Person person)
    {
        dbContext.People.Add(person);
        await dbContext.SaveChangesAsync();
        return person.Id;
    }

    public async Task UpdateAsync(Person person)
    {
        dbContext.People.Update(person);
        await dbContext.SaveChangesAsync();
    }

    public async Task<int> DeleteAsync(int id)
    {
        var rowsAffected = await dbContext.People
            .Where(p => p.Id == id)
            .ExecuteDeleteAsync();

        if (rowsAffected == 0)
        {
            throw new InvalidOperationException($"Person with id {id} not found.");
        }

        return rowsAffected;
    }
}
