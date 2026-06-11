using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using Testcontainers.MsSql;

namespace ItemCatalogueAPI.Tests.Infrastructure;

// Boots the real API in-process via WebApplicationFactory and points its DbContext at a real SQL
// Server in Docker. A real engine is required (not EF in-memory) because the repositories rely on
// SQL Server-only features the API exercises end to end: rowversion concurrency, the JSON ItemTypes
// column, ExecuteUpdate/ExecuteDelete, and FK-restrict (error 547) translation to 409.
public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string SqlServerImage = "mcr.microsoft.com/mssql/server:2022-CU14-ubuntu-22.04";

    private readonly MsSqlContainer _container = new MsSqlBuilder(SqlServerImage).Build();

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();

        // Touching Services builds the host (running ConfigureWebHost below with the container's
        // connection string), then we create the schema from the EF model a single time.
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ItemCatalogueDbContext>();
        await db.Database.EnsureCreatedAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        // Override the "local" connection string the Persistence layer reads, so AddPersistence wires
        // the DbContext to the test container instead of a developer's local SQL Server.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:local"] = _container.GetConnectionString(),
            });
        });
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
