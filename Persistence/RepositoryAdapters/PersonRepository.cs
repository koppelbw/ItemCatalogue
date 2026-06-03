using Domain.Entities;
using Domain.Exceptions;
using Domain.RepositoryPorts;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;

namespace Persistence.RepositoryAdapters;

public sealed class PersonRepository(ItemCatalogueDbContext dbContext) : IPersonRepository
{
    public async Task<Person?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        // Read-only path: not tracked, since the result is only mapped to a response.
        return await dbContext.People
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    public async Task<Person?> GetForUpdateAsync(int id, CancellationToken cancellationToken = default)
    {
        // Tracked (no AsNoTracking) so SaveChangesAsync emits a minimal, diff-based UPDATE.
        return await dbContext.People
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
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
        // person is already tracked (loaded via GetForUpdateAsync), so no Update() call is needed.
        // Drive the concurrency check off the client's token carried on the entity.
        dbContext.Entry(person).Property(p => p.RowVersion).OriginalValue = person.RowVersion;

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException(
                $"Person with id {person.Id} was modified by another process. Reload and try again.", ex);
        }
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        try
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
        catch (SqlException ex) when (ex.Number == 547)
        {
            // 547 = FK reference constraint violation. Defensive: today nothing restricts a
            // Person delete (Item.OwnerId is SetNull), but translate it consistently so a
            // future restricted FK surfaces as a domain exception rather than a raw SQL error.
            throw new EntityInUseException(
                $"Person with id {id} cannot be deleted because it is still referenced by another record.", ex);
        }
    }
}
