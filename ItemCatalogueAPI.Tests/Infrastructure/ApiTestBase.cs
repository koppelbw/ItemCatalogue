using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Persistence;

namespace ItemCatalogueAPI.Tests.Infrastructure;

// Base for the API functional tests. Each test starts from an empty database (the tables are wiped
// in FK-safe order before the test runs) and gets a fresh HttpClient against the in-process server.
// Tests within the collection run sequentially, so wiping up front gives deterministic isolation
// without per-test schema recreation.
[Collection(ApiCollection.Name)]
public abstract class ApiTestBase : IAsyncLifetime
{
    private readonly ApiFactory _factory;
    protected HttpClient Client { get; private set; } = null!;

    protected ApiTestBase(ApiFactory factory) => _factory = factory;

    public async ValueTask InitializeAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ItemCatalogueDbContext>();

        // Delete children before parents: Item -> (Room, Person), Room -> Location.
        await db.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM [Item];
            DELETE FROM [Room];
            DELETE FROM [Location];
            DELETE FROM [Person];
            """);

        Client = _factory.CreateClient();
    }

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        return ValueTask.CompletedTask;
    }
}
