using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.RepositoryAdapters;
using Persistence.Tests.Infrastructure;
using Shouldly;

namespace Persistence.Tests;

// Item carries behaviour the generic adapters do not: a JSON-serialized ItemTypes column, eager
// loading of its Location/Owner graph, and a soft delete (ExecuteUpdate) in place of a hard delete.
public class ItemRepositoryTests(SqlServerFixture fixture) : PersistenceTestBase(fixture)
{
    private ItemRepository Items() => new(Db, Clock, NullLoggerFactory.Instance);
    private RoomRepository Rooms() => new(Db, NullLoggerFactory.Instance);
    private LocationRepository Locations() => new(Db, NullLoggerFactory.Instance);
    private PersonRepository People() => new(Db, NullLoggerFactory.Instance);

    private async Task<(int locationId, int ownerId)> SeedLocationAndOwnerAsync()
    {
        var roomId = await Rooms().InsertAsync(new Room { Name = "Garage" });
        var locationId = await Locations().InsertAsync(new Location { Name = "Top shelf", RoomId = roomId });
        var ownerId = await People().InsertAsync(new Person { Name = "Alex" });
        return (locationId, ownerId);
    }

    [Fact]
    public async Task Insert_thenGetById_RoundTripsItemTypesJsonAndEagerLoadsGraph()
    {
        var items = Items();
        var (locationId, ownerId) = await SeedLocationAndOwnerAsync();

        var id = await items.InsertAsync(new Item
        {
            Name = "Desk Lamp",
            ItemTypes = [ItemType.Electronics, ItemType.Books],
            Price = 19.99m,
            LocationId = locationId,
            OwnerId = ownerId,
        });

        var found = await items.GetByIdAsync(id);

        found.ShouldNotBeNull();
        // The list column survives serialize -> store as nvarchar(max) -> deserialize, order intact.
        found.ItemTypes.ShouldBe([ItemType.Electronics, ItemType.Books]);
        found.Price.ShouldBe(19.99m);
        // ReadQuery eager-loads Location and Owner.
        found.Location.ShouldNotBeNull();
        found.Location.Name.ShouldBe("Top shelf");
        found.Owner.ShouldNotBeNull();
        found.Owner.Name.ShouldBe("Alex");
    }

    [Fact]
    public async Task SoftDeleteItemByIdAsync_MarksDeletedWithReasonAndModifiedDate()
    {
        var items = Items();
        var id = await items.InsertAsync(new Item { Name = "Desk Lamp", ItemTypes = [ItemType.Electronics] });

        // Advance so LastModifiedDate (set by the soft delete) differs from CreatedDate.
        Clock.Advance(TimeSpan.FromMinutes(10));

        var affected = await items.SoftDeleteItemByIdAsync(id, DeletedReason.Broken);

        affected.ShouldBe(1);
        var reloaded = await items.GetByIdAsync(id);
        reloaded!.IsDeleted.ShouldBeTrue();
        reloaded.ReasonForDeletion.ShouldBe(DeletedReason.Broken);
        reloaded.LastModifiedDate.ShouldBe(Clock.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public async Task SoftDeleteItemByIdAsync_WhenMissing_Throws()
    {
        await Should.ThrowAsync<InvalidOperationException>(
            () => Items().SoftDeleteItemByIdAsync(999_999, DeletedReason.Lost));
    }

    [Fact]
    public async Task DeletingReferencedLocation_NullsItemLocationIdViaSetNull()
    {
        var items = Items();
        var (locationId, _) = await SeedLocationAndOwnerAsync();
        var id = await items.InsertAsync(new Item
        {
            Name = "Desk Lamp",
            ItemTypes = [ItemType.Electronics],
            LocationId = locationId,
        });

        // Item -> Location is configured OnDelete(SetNull), so removing the Location must not be
        // blocked and must clear the item's LocationId rather than delete the item.
        await Locations().DeleteAsync(locationId);

        var reloaded = await items.GetByIdAsync(id);
        reloaded.ShouldNotBeNull();
        reloaded.LocationId.ShouldBeNull();
    }
}
