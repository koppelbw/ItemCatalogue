using Domain.Entities;
using Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.RepositoryAdapters;
using Persistence.Tests.Infrastructure;
using Shouldly;

namespace Persistence.Tests;

// The Location -> Room foreign key uses DeleteBehavior.Restrict. Deleting a Room that still has
// Locations must surface as the domain-level EntityInUseException (translated from SQL Server
// error 547), so the API can map it to 409 Conflict without referencing EF. This is the core of
// the graceful-delete plan.
public class FkRestrictDeleteTests(SqlServerFixture fixture) : PersistenceTestBase(fixture)
{
    private RoomRepository Rooms() => new(Db, NullLoggerFactory.Instance);
    private LocationRepository Locations() => new(Db, NullLoggerFactory.Instance);

    [Fact]
    public async Task DeleteRoom_WhileReferencedByLocation_ThrowsEntityInUse()
    {
        var rooms = Rooms();
        var roomId = await rooms.InsertAsync(new Room { Name = "Garage" });
        await Locations().InsertAsync(new Location { Name = "Top shelf", RoomId = roomId });

        await Should.ThrowAsync<EntityInUseException>(() => rooms.DeleteAsync(roomId));

        // The restrict means nothing was deleted; the room is still there.
        (await rooms.GetByIdAsync(roomId)).ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteRoom_AfterRemovingItsLocations_Succeeds()
    {
        var rooms = Rooms();
        var locations = Locations();
        var roomId = await rooms.InsertAsync(new Room { Name = "Garage" });
        var locationId = await locations.InsertAsync(new Location { Name = "Top shelf", RoomId = roomId });

        // Remove the only child, then the parent delete is no longer restricted.
        await locations.DeleteAsync(locationId);
        var affected = await rooms.DeleteAsync(roomId);

        affected.ShouldBe(1);
        (await rooms.GetByIdAsync(roomId)).ShouldBeNull();
    }
}
