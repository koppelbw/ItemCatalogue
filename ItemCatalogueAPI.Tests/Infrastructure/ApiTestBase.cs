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

        // Delete children before parents. The join tables (ItemTag, CollectionItem) reference
        // Item/Tag/Collection, so clear them first; then Item -> (Room|Container|Person); Container ->
        // (Room|Container); Door -> Room; Room -> Floor; Floor -> Location. A single DELETE FROM
        // [Container] (and [Door]) clears all rows at once, satisfying the self-referencing /
        // multiple-FK-to-Room checks at statement end.
        await db.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM [ImportChunk];
            DELETE FROM [ImportJob];
            DELETE FROM [Picture];
            DELETE FROM [ItemTag];
            DELETE FROM [CollectionItem];
            DELETE FROM [Item];
            DELETE FROM [Container];
            DELETE FROM [Door];
            DELETE FROM [Stair];
            DELETE FROM [Room];
            DELETE FROM [Floor];
            DELETE FROM [Location];
            DELETE FROM [Person];
            DELETE FROM [Tag];
            DELETE FROM [Collection];
            """);

        Client = _factory.CreateClient();
    }

    public ValueTask DisposeAsync()
    {
        Client.Dispose();
        return ValueTask.CompletedTask;
    }
}
