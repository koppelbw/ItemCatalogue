using Domain.Entities;
using Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.RepositoryAdapters;
using Persistence.Tests.Infrastructure;
using Shouldly;

namespace Persistence.Tests;

// The Room -> Location foreign key uses DeleteBehavior.Restrict. Deleting a Location that still has
// Rooms must surface as the domain-level EntityInUseException (translated from SQL Server
// error 547), so the API can map it to 409 Conflict without referencing EF. This is the core of
// the graceful-delete plan.
public class FkRestrictDeleteTests(SqlServerFixture fixture) : PersistenceTestBase(fixture)
{
    private RoomRepository Rooms() => new(Db, NullLoggerFactory.Instance);
    private LocationRepository Locations() => new(Db, NullLoggerFactory.Instance);

    [Fact]
    public async Task DeleteLocation_WhileReferencedByRoom_ThrowsEntityInUse()
    {
        var locations = Locations();
        var locationId = await locations.InsertAsync(new Location { Name = "House" });
        await Rooms().InsertAsync(new Room { Name = "Garage", LocationId = locationId });

        await Should.ThrowAsync<EntityInUseException>(() => locations.DeleteAsync(locationId));

        // The restrict means nothing was deleted; the location is still there.
        (await locations.GetByIdAsync(locationId)).ShouldNotBeNull();
    }

    [Fact]
    public async Task DeleteLocation_AfterRemovingItsRooms_Succeeds()
    {
        var rooms = Rooms();
        var locations = Locations();
        var locationId = await locations.InsertAsync(new Location { Name = "House" });
        var roomId = await rooms.InsertAsync(new Room { Name = "Garage", LocationId = locationId });

        // Remove the only child, then the parent delete is no longer restricted.
        await rooms.DeleteAsync(roomId);
        var affected = await locations.DeleteAsync(locationId);

        affected.ShouldBe(1);
        (await locations.GetByIdAsync(locationId)).ShouldBeNull();
    }
}
