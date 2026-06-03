namespace Domain.Entities;

// Shared shape for all persisted entities: an integer key and a SQL Server
// rowversion used as an optimistic-concurrency token. Lets GenericRepository<TEntity>
// operate generically over any entity without per-type plumbing.
public interface IEntity
{
    int Id { get; }

    byte[] RowVersion { get; set; }
}
