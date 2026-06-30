using ItemCatalogue.TestSupport;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.MsSql;

namespace ItemCatalogueAPI.Tests.Infrastructure;

// Boots the real API in-process via WebApplicationFactory and points its DbContext at a real SQL
// Server in Docker. A real engine is required (not EF in-memory) because the repositories rely on
// SQL Server-only features the API exercises end to end: rowversion concurrency, the JSON ItemTypes
// column, ExecuteUpdate/ExecuteDelete, and FK-restrict (error 547) translation to 409. The schema is
// published from the SSDT dacpac (the schema source of truth) rather than EF's EnsureCreated, so the
// API runs against the exact production schema and any EF<->schema drift surfaces here.
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04";

    // The dacpac publishes into a named database; the API then connects to this same database.
    private const string TargetDatabase = "ItemCatalogue";

    private readonly MsSqlContainer _container = new MsSqlBuilder(SqlServerImage).Build();

    private string _connectionString = string.Empty;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        // The container's connection string targets master; use it to publish the dacpac, which
        // creates the ItemCatalogue database and all of its schema objects.
        var adminConnectionString = _container.GetConnectionString();
        DacpacDeployer.Publish(adminConnectionString, TargetDatabase);

        // The API must talk to the freshly published database, not master. Set this before the host is
        // built (below) so ConfigureWebHost picks it up. The dacpac's post-deployment seed rows are
        // removed per test by ApiTestBase, which wipes the tables before each test runs.
        _connectionString = new SqlConnectionStringBuilder(adminConnectionString)
        {
            InitialCatalog = TargetDatabase,
        }.ConnectionString;

        // Touch Services to build the host now (running ConfigureWebHost with the connection string
        // above) so startup failures surface here rather than in the first test.
        _ = Services;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Override the "local" connection string the Persistence layer reads, so AddPersistence wires
        // the DbContext to the test container's ItemCatalogue database.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:local"] = _connectionString,
            });
        });

        // AddApiRateLimiting captures PermitLimit eagerly at service-registration time (before
        // ConfigureAppConfiguration overrides are applied), so a config key override arrives too late.
        // PostConfigure runs after all Configure calls and sets GlobalLimiter to null, which disables
        // global rate limiting entirely for the test host.
        builder.ConfigureTestServices(services =>
            services.PostConfigure<RateLimiterOptions>(o => o.GlobalLimiter = null));
    }

    public override async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
        await base.DisposeAsync();
    }
}

// Single shared collection so the container + host start once for the whole API test run.
[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<ApiFactory>
{
    public const string Name = "api";
}
