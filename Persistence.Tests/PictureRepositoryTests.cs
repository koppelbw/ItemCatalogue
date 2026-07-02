using Domain.Entities;
using Domain.Enums;
using Domain.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.RepositoryAdapters;
using Persistence.Tests.Infrastructure;
using Shouldly;

namespace Persistence.Tests;

// A picture belongs to exactly one of Location/Room/Container/Item, enforced by
// CK_Picture_ExactlyOneOwner (Database/dbo/tables/Picture.sql) and all four FKs cascade, so
// deleting the owner removes its pictures. SchemaDriftTests separately proves the EF model and the
// SSDT table agree; these tests exercise the repository's owner-scoped querying and the DB-level
// constraint backstop.
public class PictureRepositoryTests(SqlServerFixture fixture) : PersistenceTestBase(fixture)
{
    private PictureRepository Pictures() => new(Db, NullLoggerFactory.Instance);
    private LocationRepository Locations() => new(Db, NullLoggerFactory.Instance);
    private FloorRepository Floors() => new(Db, NullLoggerFactory.Instance);
    private RoomRepository Rooms() => new(Db, NullLoggerFactory.Instance);
    private ContainerRepository Containers() => new(Db, NullLoggerFactory.Instance);
    private ItemRepository Items() => new(Db, Clock, NullLoggerFactory.Instance);

    private async Task<int> SeedLocationAsync() =>
        await Locations().InsertAsync(new Location { Name = "Home" });

    private async Task<int> SeedRoomAsync()
    {
        var locationId = await SeedLocationAsync();
        var floorId = await Floors().InsertAsync(new Floor { Name = "Ground", LocationId = locationId, LevelIndex = 0 });
        return await Rooms().InsertAsync(new Room { Name = "Office", FloorId = floorId });
    }

    private async Task<int> SeedContainerAsync()
    {
        var roomId = await SeedRoomAsync();
        return await Containers().InsertAsync(new Container { Name = "Shelf", RoomId = roomId });
    }

    private async Task<int> SeedItemAsync()
    {
        var roomId = await SeedRoomAsync();
        return await Items().InsertAsync(new Item { Name = "Lamp", RoomId = roomId, ItemTypes = [ItemType.Electronics] });
    }

    [Fact]
    public async Task InsertPicture_ForLocation_RoundTripsThroughGetForOwner()
    {
        var locationId = await SeedLocationAsync();
        var pictures = Pictures();
        await pictures.InsertAsync(new Picture { BlobName = "location/pic.jpg", ContentType = "image/jpeg", SizeBytes = 100, LocationId = locationId });

        var page = await pictures.GetForOwnerAsync(PictureOwnerType.Location, locationId, PageRequest.Create());

        page.TotalCount.ShouldBe(1);
        page.Items.Single().LocationId.ShouldBe(locationId);
    }

    [Fact]
    public async Task InsertPicture_ForRoom_RoundTripsThroughGetForOwner()
    {
        var roomId = await SeedRoomAsync();
        var pictures = Pictures();
        await pictures.InsertAsync(new Picture { BlobName = "room/pic.jpg", ContentType = "image/jpeg", SizeBytes = 100, RoomId = roomId });

        var page = await pictures.GetForOwnerAsync(PictureOwnerType.Room, roomId, PageRequest.Create());

        page.TotalCount.ShouldBe(1);
        page.Items.Single().RoomId.ShouldBe(roomId);
    }

    [Fact]
    public async Task InsertPicture_ForContainer_RoundTripsThroughGetForOwner()
    {
        var containerId = await SeedContainerAsync();
        var pictures = Pictures();
        await pictures.InsertAsync(new Picture { BlobName = "container/pic.jpg", ContentType = "image/jpeg", SizeBytes = 100, ContainerId = containerId });

        var page = await pictures.GetForOwnerAsync(PictureOwnerType.Container, containerId, PageRequest.Create());

        page.TotalCount.ShouldBe(1);
        page.Items.Single().ContainerId.ShouldBe(containerId);
    }

    [Fact]
    public async Task InsertPicture_ForItem_RoundTripsThroughGetForOwner()
    {
        var itemId = await SeedItemAsync();
        var pictures = Pictures();
        await pictures.InsertAsync(new Picture { BlobName = "item/pic.jpg", ContentType = "image/jpeg", SizeBytes = 100, ItemId = itemId });

        var page = await pictures.GetForOwnerAsync(PictureOwnerType.Item, itemId, PageRequest.Create());

        page.TotalCount.ShouldBe(1);
        page.Items.Single().ItemId.ShouldBe(itemId);
    }

    [Fact]
    public async Task GetForOwnerAsync_OrdersBySortOrderThenId()
    {
        var itemId = await SeedItemAsync();
        var pictures = Pictures();
        var secondId = await pictures.InsertAsync(new Picture { BlobName = "b.jpg", ContentType = "image/jpeg", SizeBytes = 1, ItemId = itemId, SortOrder = 2 });
        var firstId = await pictures.InsertAsync(new Picture { BlobName = "a.jpg", ContentType = "image/jpeg", SizeBytes = 1, ItemId = itemId, SortOrder = 1 });

        var page = await pictures.GetForOwnerAsync(PictureOwnerType.Item, itemId, PageRequest.Create());

        page.Items.Select(p => p.Id).ShouldBe([firstId, secondId]);
    }

    [Fact]
    public async Task ClearPrimaryForOwnerAsync_ClearsOthersButKeepsExcepted()
    {
        var itemId = await SeedItemAsync();
        var pictures = Pictures();
        var keepId = await pictures.InsertAsync(new Picture { BlobName = "keep.jpg", ContentType = "image/jpeg", SizeBytes = 1, ItemId = itemId, IsPrimary = true });
        var clearId = await pictures.InsertAsync(new Picture { BlobName = "clear.jpg", ContentType = "image/jpeg", SizeBytes = 1, ItemId = itemId, IsPrimary = true });

        await pictures.ClearPrimaryForOwnerAsync(PictureOwnerType.Item, itemId, keepId);

        (await pictures.GetByIdAsync(keepId))!.IsPrimary.ShouldBeTrue();
        (await pictures.GetByIdAsync(clearId))!.IsPrimary.ShouldBeFalse();
    }

    [Fact]
    public async Task InsertPicture_WithNoOwnerSet_ViolatesCheckConstraint()
    {
        var pictures = Pictures();

        await Should.ThrowAsync<DbUpdateException>(() =>
            pictures.InsertAsync(new Picture { BlobName = "x.jpg", ContentType = "image/jpeg", SizeBytes = 1 }));
    }

    [Fact]
    public async Task InsertPicture_WithTwoOwnersSet_ViolatesCheckConstraint()
    {
        var roomId = await SeedRoomAsync();
        var itemId = await Items().InsertAsync(new Item { Name = "Lamp", RoomId = roomId, ItemTypes = [ItemType.Electronics] });
        var pictures = Pictures();

        await Should.ThrowAsync<DbUpdateException>(() =>
            pictures.InsertAsync(new Picture { BlobName = "x.jpg", ContentType = "image/jpeg", SizeBytes = 1, RoomId = roomId, ItemId = itemId }));
    }

    [Fact]
    public async Task DeleteItem_CascadesToItsPictures()
    {
        var itemId = await SeedItemAsync();
        var pictures = Pictures();
        var pictureId = await pictures.InsertAsync(new Picture { BlobName = "item.jpg", ContentType = "image/jpeg", SizeBytes = 1, ItemId = itemId });

        await Items().DeleteAsync(itemId);

        (await pictures.GetByIdAsync(pictureId)).ShouldBeNull();
    }

    [Fact]
    public async Task DeleteRoom_CascadesToItsPictures()
    {
        var roomId = await SeedRoomAsync();
        var pictures = Pictures();
        var pictureId = await pictures.InsertAsync(new Picture { BlobName = "room.jpg", ContentType = "image/jpeg", SizeBytes = 1, RoomId = roomId });

        await Rooms().DeleteAsync(roomId);

        (await pictures.GetByIdAsync(pictureId)).ShouldBeNull();
    }
}
