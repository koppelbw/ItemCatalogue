using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Time.Testing;
using Persistence;

namespace Persistence.Tests.Infrastructure;

// Base for integration tests that share the SQL Server container. Each test runs inside a
// transaction that is rolled back on dispose, so tests are fully isolated and the table starts
// empty every time — without paying to recreate the schema per test. A FakeTimeProvider gives a
// fixed, advanceable clock so CreatedDate/LastModifiedDate assertions are exact.
[Collection(SqlServerCollection.Name)]
public abstract class PersistenceTestBase : IAsyncLifetime
{
    private readonly SqlServerFixture _fixture;
    private IDbContextTransaction _transaction = null!;

    protected ItemCatalogueDbContext Db { get; private set; } = null!;
    protected FakeTimeProvider Clock { get; } = new(DateTimeOffset.Parse("2026-06-10T12:00:00Z"));

    protected PersistenceTestBase(SqlServerFixture fixture) => _fixture = fixture;

    public async ValueTask InitializeAsync()
    {
        Db = _fixture.CreateContext(Clock);
        _transaction = await Db.Database.BeginTransactionAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _transaction.RollbackAsync();
        await _transaction.DisposeAsync();
        await Db.DisposeAsync();
    }
}
