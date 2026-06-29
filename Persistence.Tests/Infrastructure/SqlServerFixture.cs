using ItemCatalogue.TestSupport;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Time.Testing;
using Persistence;
using Persistence.Interceptors;
using Testcontainers.MsSql;

namespace Persistence.Tests.Infrastructure;

// Spins up one real SQL Server in Docker for the whole integration-test run (shared via the
// [Collection] below) and provisions the schema once by publishing the SSDT dacpac — the same
// artifact deployed to production. Publishing the dacpac (rather than EF's EnsureCreated) means the
// tests run against the exact production schema, so any drift between the EF model and the real
// database surfaces here. A real engine is required — not the EF in-memory or SQLite providers —
// because these tests exercise SQL Server-specific behaviour: the rowversion concurrency token, the
// nvarchar(max) JSON conversion for ItemTypes, ExecuteUpdate/ExecuteDelete, and FK-restrict (error
// 547) translation. Individual tests run inside a transaction that is rolled back, so the schema is
// built only once and tables stay empty between tests.
public sealed class SqlServerFixture : IAsyncLifetime
{
    // Pin the image so the test run is reproducible and independent of the local Docker cache.
    private const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04";

    // The dacpac publishes into a named database; EF then connects to this same database.
    private const string TargetDatabase = "ItemCatalogue";

    private readonly MsSqlContainer _container = new MsSqlBuilder(SqlServerImage).Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        // The container's connection string targets the master database; use it to publish the dacpac,
        // which creates the ItemCatalogue database and all of its schema objects.
        var adminConnectionString = _container.GetConnectionString();
        DacpacDeployer.Publish(adminConnectionString, TargetDatabase);

        // Point EF at the freshly published database rather than master.
        ConnectionString = new SqlConnectionStringBuilder(adminConnectionString)
        {
            InitialCatalog = TargetDatabase,
        }.ConnectionString;

        // The dacpac's post-deployment scripts seed reference/demo rows. Clear them so every test
        // starts from empty tables: PersistenceTestBase isolates each test in a rolled-back
        // transaction, and the assertions (row counts, paging) assume a clean slate.
        await ClearSeedDataAsync();
    }

    // Each test gets its own context wired with the same auditing interceptor used in production,
    // driven by the supplied (fake) clock so audit-stamp assertions are deterministic.
    public ItemCatalogueDbContext CreateContext(FakeTimeProvider clock)
    {
        var options = new DbContextOptionsBuilder<ItemCatalogueDbContext>()
            .UseSqlServer(ConnectionString)
            .AddInterceptors(
                new AuditingSaveChangesInterceptor(clock),
                new ItemEventInterceptor(clock))
            .Options;

        return new ItemCatalogueDbContext(options);
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();

    private async Task ClearSeedDataAsync()
    {
        await using var context = CreateContext(new FakeTimeProvider());
        // FK-safe order: children before parents. The join tables (ItemTag, CollectionItem) reference
        // Item/Tag/Collection, so clear them first; then Item -> (Room|Container); Container ->
        // (Room|Container); Door -> Room; Room -> Floor; Floor -> Location; Person, Tag, Collection are
        // independent once their join rows are gone. A single DELETE FROM [Container] (and [Door])
        // clears all rows at once, satisfying the self-referencing / multiple-FK-to-Room checks.
        await context.Database.ExecuteSqlRawAsync(
            "DELETE FROM [ItemTag]; DELETE FROM [CollectionItem]; DELETE FROM [Item]; DELETE FROM [Container]; " +
            "DELETE FROM [Door]; DELETE FROM [Room]; DELETE FROM [Floor]; DELETE FROM [Location]; " +
            "DELETE FROM [Person]; DELETE FROM [Tag]; DELETE FROM [Collection];");
    }
}

// Single shared collection so the container starts once for all integration tests.
[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "sqlserver";
}
