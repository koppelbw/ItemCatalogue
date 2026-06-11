using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Persistence;
using Persistence.Interceptors;
using Testcontainers.MsSql;

namespace Persistence.Tests.Infrastructure;

// Spins up one real SQL Server in Docker for the whole integration-test run (shared via the
// [Collection] below) and creates the EF schema once. A real engine is required — not the EF
// in-memory or SQLite providers — because these tests exercise SQL Server-specific behaviour:
// the rowversion concurrency token, the nvarchar(max) JSON conversion for ItemTypes, ExecuteUpdate/
// ExecuteDelete, and FK-restrict (error 547) translation. Individual tests run inside a transaction
// that is rolled back, so the schema is built only once and tables stay empty between tests.
public sealed class SqlServerFixture : IAsyncLifetime
{
    // Pin the image so the test run is reproducible and independent of the local Docker cache.
    private const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04";

    private readonly MsSqlContainer _container = new MsSqlBuilder(SqlServerImage).Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();

        // Build the schema from the EF model a single time. EnsureCreated (rather than migrations)
        // is appropriate here: the test database is disposable and we want it to match the mappings
        // in ItemCatalogueDbContext exactly.
        await using var context = CreateContext(new FakeTimeProvider());
        await context.Database.EnsureCreatedAsync();
    }

    // Each test gets its own context wired with the same auditing interceptor used in production,
    // driven by the supplied (fake) clock so audit-stamp assertions are deterministic.
    public ItemCatalogueDbContext CreateContext(FakeTimeProvider clock)
    {
        var options = new DbContextOptionsBuilder<ItemCatalogueDbContext>()
            .UseSqlServer(ConnectionString)
            .AddInterceptors(new AuditingSaveChangesInterceptor(clock))
            .Options;

        return new ItemCatalogueDbContext(options);
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}

// Single shared collection so the container starts once for all integration tests.
[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "sqlserver";
}
