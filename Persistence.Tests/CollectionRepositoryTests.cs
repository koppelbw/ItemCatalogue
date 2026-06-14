using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.RepositoryAdapters;
using Persistence.Tests.Infrastructure;
using Shouldly;

namespace Persistence.Tests;

// Collection carries the rich Collection <-> Item join (CollectionItem). Membership is maintained
// through the tracked aggregate (GetForUpdateWithItemsAsync + SaveAsync); reads come back ordered by
// SortOrder with each member's Item, and deleting the collection cascades the membership rows away
// while leaving the items themselves intact.
public class CollectionRepositoryTests(SqlServerFixture fixture) : PersistenceTestBase(fixture)
{
    private CollectionRepository Collections() => new(Db, NullLoggerFactory.Instance);
    private ItemRepository Items() => new(Db, Clock, NullLoggerFactory.Instance);

    private Task<int> SeedItemAsync(string name) =>
        Items().InsertAsync(new Item { Name = name, ItemTypes = [ItemType.Electronics] });

    [Fact]
    public async Task AddMembers_thenGetById_ReturnsMembersOrderedBySortOrderWithItems()
    {
        var collections = Collections();
        var collectionId = await collections.InsertAsync(new Collection { Name = "Office Kit" });
        var laptopId = await SeedItemAsync("Laptop");
        var mouseId = await SeedItemAsync("Mouse");

        // Add out of order to prove the read path sorts by SortOrder.
        var tracked = await collections.GetForUpdateWithItemsAsync(collectionId);
        tracked!.Items.Add(new CollectionItem { CollectionId = collectionId, ItemId = mouseId, Quantity = 1, SortOrder = 2, Role = "Accessory" });
        tracked.Items.Add(new CollectionItem { CollectionId = collectionId, ItemId = laptopId, Quantity = 1, SortOrder = 1, Role = "Primary" });
        await collections.SaveAsync();

        var loaded = await collections.GetByIdAsync(collectionId);

        loaded.ShouldNotBeNull();
        loaded.Items.Count.ShouldBe(2);
        loaded.Items.Select(ci => ci.Item!.Name).ShouldBe(["Laptop", "Mouse"]);
        loaded.Items.First().Role.ShouldBe("Primary");
    }

    [Fact]
    public async Task RemoveMember_thenGetById_OmitsIt()
    {
        var collections = Collections();
        var collectionId = await collections.InsertAsync(new Collection { Name = "Office Kit" });
        var itemId = await SeedItemAsync("Laptop");

        var tracked = await collections.GetForUpdateWithItemsAsync(collectionId);
        tracked!.Items.Add(new CollectionItem { CollectionId = collectionId, ItemId = itemId });
        await collections.SaveAsync();

        var toEdit = await collections.GetForUpdateWithItemsAsync(collectionId);
        toEdit!.Items.Remove(toEdit.Items.Single(ci => ci.ItemId == itemId));
        await collections.SaveAsync();

        (await collections.GetByIdAsync(collectionId))!.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task DeleteCollection_CascadesToMembershipsButKeepsItems()
    {
        var collections = Collections();
        var collectionId = await collections.InsertAsync(new Collection { Name = "Office Kit" });
        var itemId = await SeedItemAsync("Laptop");

        var tracked = await collections.GetForUpdateWithItemsAsync(collectionId);
        tracked!.Items.Add(new CollectionItem { CollectionId = collectionId, ItemId = itemId });
        await collections.SaveAsync();

        await collections.DeleteAsync(collectionId);

        (await collections.GetByIdAsync(collectionId)).ShouldBeNull();
        (await Db.CollectionItems.CountAsync(ci => ci.CollectionId == collectionId)).ShouldBe(0);
        // The item itself is untouched by the cascade.
        (await Items().GetByIdAsync(itemId)).ShouldNotBeNull();
    }
}
