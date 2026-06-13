using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.RepositoryAdapters;
using Persistence.Tests.Infrastructure;
using Shouldly;

namespace Persistence.Tests;

// Item carries behaviour the generic adapters do not: a JSON-serialized ItemTypes column, eager
// loading of its Room/Owner graph, and a soft delete (ExecuteUpdate) in place of a hard delete.
public class ItemRepositoryTests(SqlServerFixture fixture) : PersistenceTestBase(fixture)
{
    private ItemRepository Items() => new(Db, Clock, NullLoggerFactory.Instance);
    private RoomRepository Rooms() => new(Db, NullLoggerFactory.Instance);
    private LocationRepository Locations() => new(Db, NullLoggerFactory.Instance);
    private PersonRepository People() => new(Db, NullLoggerFactory.Instance);

    private async Task<(int roomId, int ownerId)> SeedRoomAndOwnerAsync()
    {
        var locationId = await Locations().InsertAsync(new Location { Name = "House" });
        var roomId = await Rooms().InsertAsync(new Room { Name = "Garage", LocationId = locationId });
        var ownerId = await People().InsertAsync(new Person { Name = "Alex" });
        return (roomId, ownerId);
    }

    [Fact]
    public async Task Insert_thenGetById_RoundTripsItemTypesJsonAndEagerLoadsGraph()
    {
        var items = Items();
        var (roomId, ownerId) = await SeedRoomAndOwnerAsync();

        var id = await items.InsertAsync(new Item
        {
            Name = "Desk Lamp",
            ItemTypes = [ItemType.Electronics, ItemType.Books],
            Price = 19.99m,
            RoomId = roomId,
            OwnerId = ownerId,
        });

        var found = await items.GetByIdAsync(id);

        found.ShouldNotBeNull();
        // The list column survives serialize -> store as nvarchar(max) -> deserialize, order intact.
        found.ItemTypes.ShouldBe([ItemType.Electronics, ItemType.Books]);
        found.Price.ShouldBe(19.99m);
        // ReadQuery eager-loads Room and Owner.
        found.Room.ShouldNotBeNull();
        found.Room.Name.ShouldBe("Garage");
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
    public async Task SoftDeleteItemByIdAsync_WhenMissing_ThrowsNotFound()
    {
        await Should.ThrowAsync<NotFoundException>(
            () => Items().SoftDeleteItemByIdAsync(999_999, DeletedReason.Lost));
    }

    [Fact]
    public async Task DeletingReferencedRoom_NullsItemRoomIdViaSetNull()
    {
        var items = Items();
        var (roomId, _) = await SeedRoomAndOwnerAsync();
        var id = await items.InsertAsync(new Item
        {
            Name = "Desk Lamp",
            ItemTypes = [ItemType.Electronics],
            RoomId = roomId,
        });

        // Item -> Room is configured OnDelete(SetNull), so removing the Room must not be
        // blocked and must clear the item's RoomId rather than delete the item.
        await Rooms().DeleteAsync(roomId);

        var reloaded = await items.GetByIdAsync(id);
        reloaded.ShouldNotBeNull();
        reloaded.RoomId.ShouldBeNull();
    }
}
