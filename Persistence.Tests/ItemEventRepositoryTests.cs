using Domain.Entities;
using Domain.Enums;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.RepositoryAdapters;
using Persistence.Tests.Infrastructure;
using Shouldly;

namespace Persistence.Tests;

public class ItemEventRepositoryTests(SqlServerFixture fixture) : PersistenceTestBase(fixture)
{
    private ItemEventRepository Events() => new(Db);
    private ItemRepository Items() => new(Db, Clock, NullLoggerFactory.Instance);
    private RoomRepository Rooms() => new(Db, NullLoggerFactory.Instance);
    private LocationRepository Locations() => new(Db, NullLoggerFactory.Instance);

    private async Task<int> SeedItemAsync(int? roomId = null)
    {
        return await Items().InsertAsync(new Item
        {
            Name = "Test Item",
            ItemTypes = [ItemType.Electronics],
            RoomId = roomId,
        });
    }

    [Fact]
    public async Task InsertAsync_thenGetByItemId_RoundTrips()
    {
        var itemId = await SeedItemAsync();
        var repo = Events();

        await repo.InsertAsync(new ItemEvent
        {
            ItemId = itemId,
            EventType = ItemEventType.ValueChanged,
            OccurredAt = Clock.GetUtcNow().UtcDateTime,
            OldValue = "100",
            NewValue = "150",
            Notes = "Revalued",
        });

        var events = await repo.GetByItemIdAsync(itemId);

        // Created event from interceptor + the manually inserted ValueChanged
        events.ShouldContain(e => e.EventType == ItemEventType.ValueChanged);
        var ev = events.Single(e => e.EventType == ItemEventType.ValueChanged);
        ev.OldValue.ShouldBe("100");
        ev.NewValue.ShouldBe("150");
        ev.Notes.ShouldBe("Revalued");
    }

    [Fact]
    public async Task GetByItemIdAsync_ReturnsNewestFirst()
    {
        var itemId = await SeedItemAsync();
        var repo = Events();

        var t1 = Clock.GetUtcNow().UtcDateTime;
        Clock.Advance(TimeSpan.FromMinutes(5));
        var t2 = Clock.GetUtcNow().UtcDateTime;

        await repo.InsertAsync(new ItemEvent { ItemId = itemId, EventType = ItemEventType.ValueChanged, OccurredAt = t1 });
        await repo.InsertAsync(new ItemEvent { ItemId = itemId, EventType = ItemEventType.ConditionChanged, OccurredAt = t2 });

        var events = await repo.GetByItemIdAsync(itemId);

        // First result must be the newest
        events[0].OccurredAt.ShouldBeGreaterThanOrEqualTo(events[1].OccurredAt);
    }

    [Fact]
    public async Task Interceptor_OnItemInsert_EmitsCreatedEvent()
    {
        var itemId = await SeedItemAsync();

        var events = await Events().GetByItemIdAsync(itemId);

        events.ShouldContain(e => e.EventType == ItemEventType.Created);
    }

    [Fact]
    public async Task Interceptor_OnRoomIdChange_EmitsMovedEvent()
    {
        var locationId = await Locations().InsertAsync(new Location { Name = "House" });
        var roomA = await Rooms().InsertAsync(new Room { Name = "Garage", LocationId = locationId });
        var roomB = await Rooms().InsertAsync(new Room { Name = "Attic", LocationId = locationId });

        var itemId = await SeedItemAsync(roomA);

        // Tracked update — change the room
        var item = await Items().GetForUpdateAsync(itemId);
        item!.RoomId = roomB;
        await Items().UpdateAsync(item);

        var events = await Events().GetByItemIdAsync(itemId);

        events.ShouldContain(e => e.EventType == ItemEventType.Moved);
        var moved = events.First(e => e.EventType == ItemEventType.Moved);
        moved.OldValue.ShouldBe($"Room:{roomA}");
        moved.NewValue.ShouldBe($"Room:{roomB}");
    }

    [Fact]
    public async Task Interceptor_OnCurrentValueChange_EmitsValueChangedEvent()
    {
        var itemId = await SeedItemAsync();
        var items = Items();

        var item = await items.GetForUpdateAsync(itemId);
        item!.CurrentValue = 99.99m;
        await items.UpdateAsync(item);

        var events = await Events().GetByItemIdAsync(itemId);

        events.ShouldContain(e => e.EventType == ItemEventType.ValueChanged);
        var ev = events.First(e => e.EventType == ItemEventType.ValueChanged);
        ev.OldValue.ShouldBeNull();
        ev.NewValue.ShouldBe("99.99");
    }
}
