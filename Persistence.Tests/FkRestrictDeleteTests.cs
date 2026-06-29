using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.RepositoryAdapters;
using Persistence.Tests.Infrastructure;
using Shouldly;

namespace Persistence.Tests;

public class FkRestrictDeleteTests(SqlServerFixture fixture) : PersistenceTestBase(fixture)
{
    private RoomRepository Rooms() => new(Db, NullLoggerFactory.Instance);
    private FloorRepository Floors() => new(Db, NullLoggerFactory.Instance);
    private LocationRepository Locations() => new(Db, NullLoggerFactory.Instance);
    private ContainerRepository Containers() => new(Db, NullLoggerFactory.Instance);
    private DoorRepository Doors() => new(Db, NullLoggerFactory.Instance);
    private StairRepository Stairs() => new(Db, NullLoggerFactory.Instance);

    private async Task<int> SeedFloorAsync()
    {
        var locationId = await Locations().InsertAsync(new Location { Name = "House" });
        return await Floors().InsertAsync(new Floor { Name = "Main", LocationId = locationId });
    }

    private async Task<int> SeedRoomAsync()
    {
        var floorId = await SeedFloorAsync();
        return await Rooms().InsertAsync(new Room { Name = "Bedroom", FloorId = floorId });
    }

    [Fact]
    public async Task DeleteLocation_WhileReferencedByFloor_ThrowsEntityInUse()
    {
        var locations = Locations();
        var locationId = await locations.InsertAsync(new Location { Name = "House" });
        await Floors().InsertAsync(new Floor { Name = "Main", LocationId = locationId });

        await Should.ThrowAsync<EntityInUseException>(() => locations.DeleteAsync(locationId));

        (await locations.GetByIdAsync(locationId)).ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteFloor_WhileReferencedByRoom_ThrowsEntityInUse()
    {
        var floors = Floors();
        var floorId = await SeedFloorAsync();
        await Rooms().InsertAsync(new Room { Name = "Garage", FloorId = floorId });

        await Should.ThrowAsync<EntityInUseException>(() => floors.DeleteAsync(floorId));

        (await floors.GetByIdAsync(floorId)).ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteFloor_AfterRemovingItsRooms_Succeeds()
    {
        var rooms = Rooms();
        var floors = Floors();
        var floorId = await SeedFloorAsync();
        var roomId = await rooms.InsertAsync(new Room { Name = "Garage", FloorId = floorId });

        await rooms.DeleteAsync(roomId);
        var affected = await floors.DeleteAsync(floorId);

        affected.ShouldBe(1);
        (await floors.GetByIdAsync(floorId)).ShouldBeNull();
    }

    [Fact]
    public async Task DeleteRoom_WhileReferencedByContainer_ThrowsEntityInUse()
    {
        var rooms = Rooms();
        var roomId = await SeedRoomAsync();
        await Containers().InsertAsync(new Container { Name = "Dresser", RoomId = roomId });

        await Should.ThrowAsync<EntityInUseException>(() => rooms.DeleteAsync(roomId));

        (await rooms.GetByIdAsync(roomId)).ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteRoom_WhileReferencedByDoorFromRoom_ThrowsEntityInUse()
    {
        var rooms = Rooms();
        var roomId = await SeedRoomAsync();
        // A door placed in the room (FromRoomId) that leads outside (ToRoomId null).
        await Doors().InsertAsync(new Door
        {
            Kind = DoorKind.Door,
            FromRoomId = roomId,
            Wall = Wall.North,
            OffsetInches = 12,
            WidthInches = 36,
            HeightInches = 80,
        });

        await Should.ThrowAsync<EntityInUseException>(() => rooms.DeleteAsync(roomId));

        (await rooms.GetByIdAsync(roomId)).ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteRoom_ThatIsADoorToRoom_NullsToRoomIdViaSetNull()
    {
        var rooms = Rooms();
        var doors = Doors();
        var floorId = await SeedFloorAsync();
        var fromRoomId = await rooms.InsertAsync(new Room { Name = "Hall", FloorId = floorId });
        var toRoomId = await rooms.InsertAsync(new Room { Name = "Kitchen", FloorId = floorId });

        var doorId = await doors.InsertAsync(new Door
        {
            Kind = DoorKind.Doorway,
            FromRoomId = fromRoomId,
            ToRoomId = toRoomId,
            Wall = Wall.East,
            OffsetInches = 6,
            WidthInches = 32,
            HeightInches = 80,
        });

        // Door -> ToRoom is OnDelete(SetNull): removing the connected room must not be blocked and
        // must clear the door's ToRoomId (the door now leads outside).
        await rooms.DeleteAsync(toRoomId);

        var reloaded = await doors.GetByIdAsync(doorId);
        reloaded.ShouldNotBeNull();
        reloaded.ToRoomId.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteRoom_WhileReferencedByStairFromRoom_ThrowsEntityInUse()
    {
        var rooms = Rooms();
        var roomId = await SeedRoomAsync();
        await Stairs().InsertAsync(new Stair { Shape = StairShape.Straight, FromRoomId = roomId });

        await Should.ThrowAsync<EntityInUseException>(() => rooms.DeleteAsync(roomId));

        (await rooms.GetByIdAsync(roomId)).ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteRoom_ThatIsAStairToRoom_NullsToRoomIdViaSetNull()
    {
        var rooms = Rooms();
        var stairs = Stairs();
        var floorId = await SeedFloorAsync();
        var fromRoomId = await rooms.InsertAsync(new Room { Name = "Cellar", FloorId = floorId });
        var toRoomId = await rooms.InsertAsync(new Room { Name = "Hall", FloorId = floorId });

        var stairId = await stairs.InsertAsync(new Stair { Shape = StairShape.Straight, FromRoomId = fromRoomId, ToRoomId = toRoomId });

        await rooms.DeleteAsync(toRoomId);

        var reloaded = await stairs.GetByIdAsync(stairId);
        reloaded.ShouldNotBeNull();
        reloaded.ToRoomId.ShouldBeNull();
    }

    [Fact]
    public async Task DeleteContainer_WhileReferencedByChildContainer_ThrowsEntityInUse()
    {
        var containers = Containers();
        var roomId = await SeedRoomAsync();
        var parentId = await containers.InsertAsync(new Container { Name = "Closet", RoomId = roomId });
        await containers.InsertAsync(new Container { Name = "Storage Bin", ParentContainerId = parentId });

        await Should.ThrowAsync<EntityInUseException>(() => containers.DeleteAsync(parentId));

        (await containers.GetByIdAsync(parentId)).ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteContainer_AfterRemovingItsChildren_Succeeds()
    {
        var containers = Containers();
        var roomId = await SeedRoomAsync();
        var parentId = await containers.InsertAsync(new Container { Name = "Closet", RoomId = roomId });
        var childId = await containers.InsertAsync(new Container { Name = "Storage Bin", ParentContainerId = parentId });

        await containers.DeleteAsync(childId);
        var affected = await containers.DeleteAsync(parentId);

        affected.ShouldBe(1);
        (await containers.GetByIdAsync(parentId)).ShouldBeNull();
    }
}
