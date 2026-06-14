using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.RepositoryAdapters;
using Persistence.Tests.Infrastructure;
using Shouldly;

namespace Persistence.Tests;

// Tags are a standard aggregate (generic CRUD), but the interesting behaviour is the Item <-> Tag
// many-to-many: assigning a set through ItemRepository.SetTagsAsync, reading it back, and the
// ON DELETE CASCADE that drops a deleted tag from every item that carried it.
public class TagRepositoryTests(SqlServerFixture fixture) : PersistenceTestBase(fixture)
{
    private TagRepository Tags() => new(Db, NullLoggerFactory.Instance);
    private ItemRepository Items() => new(Db, Clock, NullLoggerFactory.Instance);

    private Task<int> SeedItemAsync(string name = "Laptop") =>
        Items().InsertAsync(new Item { Name = name, ItemTypes = [ItemType.Electronics] });

    [Fact]
    public async Task Insert_thenGetById_RoundTrips()
    {
        var tags = Tags();
        var id = await tags.InsertAsync(new Tag { Name = "Fragile", Description = "Handle with care" });

        var found = await tags.GetByIdAsync(id);

        found.ShouldNotBeNull();
        found.Name.ShouldBe("Fragile");
        found.Description.ShouldBe("Handle with care");
        found.RowVersion.ShouldNotBeEmpty();
    }

    [Fact]
    public async Task SetTagsAsync_AssignsTags_thenGetTagsReturnsThem()
    {
        var tags = Tags();
        var items = Items();
        var itemId = await SeedItemAsync();
        var fragileId = await tags.InsertAsync(new Tag { Name = "Fragile" });
        var electronicsId = await tags.InsertAsync(new Tag { Name = "Electronics" });

        await items.SetTagsAsync(itemId, [fragileId, electronicsId]);

        var assigned = await items.GetTagsAsync(itemId);
        assigned.Select(t => t.Name).OrderBy(n => n).ShouldBe(["Electronics", "Fragile"]);
    }

    [Fact]
    public async Task SetTagsAsync_ReplacesPreviousSet()
    {
        var tags = Tags();
        var items = Items();
        var itemId = await SeedItemAsync();
        var a = await tags.InsertAsync(new Tag { Name = "A" });
        var b = await tags.InsertAsync(new Tag { Name = "B" });

        await items.SetTagsAsync(itemId, [a]);
        await items.SetTagsAsync(itemId, [b]);

        (await items.GetTagsAsync(itemId)).Select(t => t.Id).ShouldBe([b]);
    }

    [Fact]
    public async Task SetTagsAsync_WithUnknownTag_ThrowsNotFound()
    {
        var itemId = await SeedItemAsync();

        await Should.ThrowAsync<NotFoundException>(() => Items().SetTagsAsync(itemId, [999_999]));
    }

    [Fact]
    public async Task SetTagsAsync_WhenItemMissing_ThrowsNotFound()
    {
        await Should.ThrowAsync<NotFoundException>(() => Items().SetTagsAsync(999_999, []));
    }

    [Fact]
    public async Task GetTagsAsync_WhenItemMissing_ThrowsNotFound()
    {
        await Should.ThrowAsync<NotFoundException>(() => Items().GetTagsAsync(999_999));
    }

    [Fact]
    public async Task InsertAsync_WithDuplicateName_ThrowsDuplicate()
    {
        var tags = Tags();
        await tags.InsertAsync(new Tag { Name = "Fragile" });

        await Should.ThrowAsync<DuplicateException>(() => tags.InsertAsync(new Tag { Name = "Fragile" }));
    }

    [Fact]
    public async Task UpdateAsync_ToDuplicateName_ThrowsDuplicate()
    {
        var tags = Tags();
        await tags.InsertAsync(new Tag { Name = "Fragile" });
        var secondId = await tags.InsertAsync(new Tag { Name = "Electronics" });

        var second = await tags.GetForUpdateAsync(secondId);
        second!.Name = "Fragile";

        await Should.ThrowAsync<DuplicateException>(() => tags.UpdateAsync(second));
    }

    [Fact]
    public async Task DeleteTag_CascadesAndRemovesItFromItems()
    {
        var tags = Tags();
        var items = Items();
        var itemId = await SeedItemAsync();
        var fragileId = await tags.InsertAsync(new Tag { Name = "Fragile" });
        await items.SetTagsAsync(itemId, [fragileId]);

        // Hard-deleting the tag cascades to ItemTag, so the item no longer carries it.
        await tags.DeleteAsync(fragileId);

        (await items.GetTagsAsync(itemId)).ShouldBeEmpty();
    }
}
