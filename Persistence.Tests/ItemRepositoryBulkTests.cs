using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.RepositoryAdapters;
using Persistence.Tests.Infrastructure;
using Shouldly;

namespace Persistence.Tests;

// Covers the GenericRepository bulk members (InsertRangeAsync / FilterExistingIdsAsync) through
// the concrete Item and Person adapters, against the real dacpac-published schema.
public class ItemRepositoryBulkTests(SqlServerFixture fixture) : PersistenceTestBase(fixture)
{
    private ItemRepository Items() => new(Db, Clock, NullLoggerFactory.Instance);
    private PersonRepository People() => new(Db, NullLoggerFactory.Instance);

    [Fact]
    public async Task InsertRangeAsync_InsertsAllRows_AndInterceptorStampsCreatedDate()
    {
        var items = Items();
        List<Item> batch =
        [
            new() { Name = "Drill", ItemTypes = [ItemType.Electronics] },
            new() { Name = "Ladder", ItemTypes = [ItemType.Electronics] },
            new() { Name = "Vise", ItemTypes = [ItemType.Electronics] },
        ];

        await items.InsertRangeAsync(batch);

        batch.Select(i => i.Id).ShouldAllBe(id => id > 0);
        batch.Select(i => i.Id).Distinct().Count().ShouldBe(3);
        var stored = await Db.Items.AsNoTracking().Where(i => i.Name == "Drill" || i.Name == "Ladder" || i.Name == "Vise").ToListAsync();
        stored.Count.ShouldBe(3);
        // One SaveChanges → the auditing interceptor stamped every row from the same fake clock.
        stored.ShouldAllBe(i => i.CreatedDate == Clock.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public async Task FilterExistingIdsAsync_ReturnsOnlyIdsThatExist()
    {
        var people = People();
        var id1 = await people.InsertAsync(new Person { Name = "Alex" });
        var id2 = await people.InsertAsync(new Person { Name = "Sam" });

        var existing = await people.FilterExistingIdsAsync([id1, id2, 99999]);

        existing.OrderBy(i => i).ShouldBe(new[] { id1, id2 }.OrderBy(i => i));
    }

    [Fact]
    public async Task FilterExistingIdsAsync_EmptyInput_SkipsTheQueryAndReturnsEmpty()
    {
        var existing = await People().FilterExistingIdsAsync([]);

        existing.ShouldBeEmpty();
    }
}
