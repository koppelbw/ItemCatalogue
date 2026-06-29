using Domain.Entities;
using Domain.Exceptions;
using Domain.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.RepositoryAdapters;
using Persistence.Tests.Infrastructure;
using Shouldly;

namespace Persistence.Tests;

public class GenericRepositoryTests(SqlServerFixture fixture) : PersistenceTestBase(fixture)
{
    private RoomRepository NewRepository() => new(Db, NullLoggerFactory.Instance);

    // Room.FloorId is a required FK, so every Room needs a parent Floor (which needs a Location).
    private async Task<int> SeedFloorAsync()
    {
        var locationId = await new LocationRepository(Db, NullLoggerFactory.Instance).InsertAsync(new Location { Name = "House" });
        return await new FloorRepository(Db, NullLoggerFactory.Instance).InsertAsync(new Floor { Name = "Main", LocationId = locationId });
    }

    [Fact]
    public async Task InsertAsync_AssignsIdentityAndStampsCreatedDateAndRowVersion()
    {
        var repository = NewRepository();
        var room = new Room { Name = "Garage", FloorId = await SeedFloorAsync() };

        var id = await repository.InsertAsync(room);

        id.ShouldBeGreaterThan(0);
        room.Id.ShouldBe(id);
        // CreatedDate comes from the auditing interceptor's (fake) clock.
        room.CreatedDate.ShouldBe(Clock.GetUtcNow().UtcDateTime);
        room.LastModifiedDate.ShouldBeNull();
        // SQL Server populates the rowversion on insert.
        room.RowVersion.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ReturnsNull()
    {
        (await NewRepository().GetByIdAsync(999_999)).ShouldBeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WhenPresent_ReturnsRow()
    {
        var repository = NewRepository();
        var id = await repository.InsertAsync(new Room { Name = "Garage", Description = "Out back", FloorId = await SeedFloorAsync() });

        var found = await repository.GetByIdAsync(id);

        found.ShouldNotBeNull();
        found.Name.ShouldBe("Garage");
        found.Description.ShouldBe("Out back");
    }

    [Fact]
    public async Task GetAllAsync_ReturnsRequestedWindowOrderedByIdWithTotalCount()
    {
        var repository = NewRepository();
        var floorId = await SeedFloorAsync();
        for (var i = 1; i <= 25; i++)
        {
            await repository.InsertAsync(new Room { Name = $"Room {i:D2}", FloorId = floorId });
        }

        var page = await repository.GetAllAsync(PageRequest.Create(page: 2, pageSize: 10));

        page.TotalCount.ShouldBe(25);
        page.Page.ShouldBe(2);
        page.PageSize.ShouldBe(10);
        page.Items.Count.ShouldBe(10);
        // Page 2 of size 10, ordered by Id, is the 11th..20th inserted rows.
        page.Items.First().Name.ShouldBe("Room 11");
        page.Items.Last().Name.ShouldBe("Room 20");
    }

    [Fact]
    public async Task UpdateAsync_PersistsChangesStampsModifiedDateAndBumpsRowVersion()
    {
        var repository = NewRepository();
        var id = await repository.InsertAsync(new Room { Name = "Garage", FloorId = await SeedFloorAsync() });
        var originalRowVersion = (await repository.GetForUpdateAsync(id))!.RowVersion;

        // Advance the clock so LastModifiedDate is distinguishable from CreatedDate.
        Clock.Advance(TimeSpan.FromMinutes(5));

        var toUpdate = await repository.GetForUpdateAsync(id);
        toUpdate!.Name = "Shed";
        await repository.UpdateAsync(toUpdate);

        var reloaded = await repository.GetByIdAsync(id);
        reloaded!.Name.ShouldBe("Shed");
        reloaded.LastModifiedDate.ShouldBe(Clock.GetUtcNow().UtcDateTime);
        reloaded.RowVersion.ShouldNotBe(originalRowVersion);
    }

    [Fact]
    public async Task UpdateAsync_WithStaleRowVersion_ThrowsConcurrencyConflict()
    {
        var repository = NewRepository();
        var id = await repository.InsertAsync(new Room { Name = "Garage", FloorId = await SeedFloorAsync() });

        // Load the row we intend to edit (carries the current rowversion as its original value)...
        var stale = await repository.GetForUpdateAsync(id);

        // ...then simulate a competing writer changing the same row out from under us. The raw
        // UPDATE bumps the server-maintained rowversion, so our pending save will match 0 rows.
        await Db.Database.ExecuteSqlAsync($"UPDATE [Room] SET [Name] = 'Concurrent' WHERE [Id] = {id}");

        stale!.Name = "Mine";

        await Should.ThrowAsync<ConcurrencyConflictException>(() => repository.UpdateAsync(stale));
    }

    [Fact]
    public async Task DeleteAsync_RemovesRowAndReturnsAffectedCount()
    {
        var repository = NewRepository();
        var id = await repository.InsertAsync(new Room { Name = "Garage", FloorId = await SeedFloorAsync() });

        var affected = await repository.DeleteAsync(id);

        affected.ShouldBe(1);
        (await repository.GetByIdAsync(id)).ShouldBeNull();
    }

    [Fact]
    public async Task DeleteAsync_WhenMissing_ThrowsNotFound()
    {
        await Should.ThrowAsync<NotFoundException>(() => NewRepository().DeleteAsync(999_999));
    }
}
