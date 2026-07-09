using Domain.Entities;
using Domain.Enums;
using Domain.Pagination;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Persistence.RepositoryAdapters;
using Persistence.Tests.Infrastructure;
using Shouldly;

namespace Persistence.Tests;

public class ImportJobRepositoryTests(SqlServerFixture fixture) : PersistenceTestBase(fixture)
{
    private ImportJobRepository Jobs() => new(Db, NullLoggerFactory.Instance);

    private static ImportJob NewJob(int totalRows = 50, int totalChunks = 2) => new()
    {
        FileName = "items.csv",
        TotalRows = totalRows,
        RejectedAtIntake = 0,
        EnqueuedRows = totalRows,
        TotalChunks = totalChunks,
    };

    private static Item NewItem(string name) => new() { Name = name, ItemTypes = [ItemType.Electronics] };

    private ImportChunk NewChunk(int jobId, int chunkIndex, int succeeded, int failed = 0) => new()
    {
        JobId = jobId,
        ChunkIndex = chunkIndex,
        Succeeded = succeeded,
        Failed = failed,
        ProcessedAt = Clock.GetUtcNow().UtcDateTime,
    };

    [Fact]
    public async Task Insert_thenGetWithChunks_RoundTripsAndStampsCreatedDate()
    {
        var jobs = Jobs();

        var id = await jobs.InsertAsync(new ImportJob
        {
            FileName = "garage-items.csv",
            TotalRows = 100,
            RejectedAtIntake = 3,
            EnqueuedRows = 97,
            TotalChunks = 4,
            IntakeErrorsJson = """[{"RowNumber":2,"Messages":["bad date"]}]""",
        });

        var found = await jobs.GetWithChunksAsync(id);

        found.ShouldNotBeNull();
        found.FileName.ShouldBe("garage-items.csv");
        found.TotalRows.ShouldBe(100);
        found.RejectedAtIntake.ShouldBe(3);
        found.EnqueuedRows.ShouldBe(97);
        found.TotalChunks.ShouldBe(4);
        found.IntakeErrorsJson.ShouldNotBeNull();
        found.Chunks.ShouldBeEmpty();
        // The auditing interceptor stamps CreatedDate from the (fake) clock.
        found.CreatedDate.ShouldBe(Clock.GetUtcNow().UtcDateTime);
    }

    [Fact]
    public async Task RecordChunkAsync_InsertsMarkerAndItemsInOneTransaction()
    {
        var jobs = Jobs();
        var jobId = await jobs.InsertAsync(NewJob());

        var recorded = await jobs.RecordChunkAsync(
            NewChunk(jobId, chunkIndex: 0, succeeded: 2, failed: 1),
            [NewItem("Drill"), NewItem("Ladder")]);

        recorded.ShouldBeTrue();
        var job = await jobs.GetWithChunksAsync(jobId);
        var chunk = job!.Chunks.ShouldHaveSingleItem();
        chunk.ChunkIndex.ShouldBe(0);
        chunk.Succeeded.ShouldBe(2);
        chunk.Failed.ShouldBe(1);
        (await Db.Items.CountAsync(i => i.Name == "Drill" || i.Name == "Ladder")).ShouldBe(2);
    }

    [Fact]
    public async Task RecordChunkAsync_RedeliveredChunk_InsertsNothingAndReturnsFalse()
    {
        var jobs = Jobs();
        var jobId = await jobs.InsertAsync(NewJob());

        (await jobs.RecordChunkAsync(NewChunk(jobId, 0, succeeded: 1), [NewItem("Drill")])).ShouldBeTrue();

        // Same (JobId, ChunkIndex) again — a redelivered queue message. The unique index must
        // reject the whole transaction: no second marker AND no duplicate item.
        var redelivered = await jobs.RecordChunkAsync(NewChunk(jobId, 0, succeeded: 1), [NewItem("Drill")]);

        redelivered.ShouldBeFalse();
        (await Db.Items.CountAsync(i => i.Name == "Drill")).ShouldBe(1);
        (await jobs.GetWithChunksAsync(jobId))!.Chunks.Count.ShouldBe(1);
    }

    [Fact]
    public async Task RecordChunkAsync_AfterRejectedRedelivery_ContextStillProcessesNextChunk()
    {
        var jobs = Jobs();
        var jobId = await jobs.InsertAsync(NewJob());

        await jobs.RecordChunkAsync(NewChunk(jobId, 0, succeeded: 1), [NewItem("Drill")]);
        await jobs.RecordChunkAsync(NewChunk(jobId, 0, succeeded: 1), [NewItem("Drill")]);

        // The failed save detached its staged entities, so the same scoped context can go on to
        // record a different chunk cleanly.
        var next = await jobs.RecordChunkAsync(NewChunk(jobId, 1, succeeded: 1), [NewItem("Ladder")]);

        next.ShouldBeTrue();
        var job = await jobs.GetWithChunksAsync(jobId);
        job!.Chunks.Select(c => c.ChunkIndex).OrderBy(i => i).ShouldBe([0, 1]);
        (await Db.Items.CountAsync()).ShouldBe(2);
    }

    [Fact]
    public async Task RecordChunkAsync_AllRowsFailed_RecordsMarkerWithNoItems()
    {
        var jobs = Jobs();
        var jobId = await jobs.InsertAsync(NewJob());

        var recorded = await jobs.RecordChunkAsync(
            NewChunk(jobId, 0, succeeded: 0, failed: 25),
            []);

        recorded.ShouldBeTrue();
        var chunk = (await jobs.GetWithChunksAsync(jobId))!.Chunks.ShouldHaveSingleItem();
        chunk.Failed.ShouldBe(25);
        (await Db.Items.CountAsync()).ShouldBe(0);
    }

    [Fact]
    public async Task GetRecentWithChunksAsync_ReturnsNewestFirst_WithChunksLoaded_AndPages()
    {
        var jobs = Jobs();
        var id1 = await jobs.InsertAsync(NewJob());
        var id2 = await jobs.InsertAsync(NewJob());
        var id3 = await jobs.InsertAsync(NewJob());
        await jobs.RecordChunkAsync(NewChunk(id3, 0, succeeded: 2), [NewItem("A"), NewItem("B")]);

        var page1 = await jobs.GetRecentWithChunksAsync(PageRequest.Create(1, 2));

        page1.TotalCount.ShouldBe(3);
        // Newest first (highest id).
        page1.Items.Select(j => j.Id).ShouldBe([id3, id2]);
        // Chunk markers are eager-loaded so status/counts can be derived.
        page1.Items[0].Chunks.ShouldHaveSingleItem().Succeeded.ShouldBe(2);

        var page2 = await jobs.GetRecentWithChunksAsync(PageRequest.Create(2, 2));
        page2.Items.Select(j => j.Id).ShouldBe([id1]);
    }

    [Fact]
    public async Task DeletingJob_CascadesToChunkMarkers()
    {
        var jobs = Jobs();
        var jobId = await jobs.InsertAsync(NewJob());
        await jobs.RecordChunkAsync(NewChunk(jobId, 0, succeeded: 1), [NewItem("Drill")]);

        await jobs.DeleteAsync(jobId);

        (await Db.ImportChunks.CountAsync()).ShouldBe(0);
        // Items are not children of the job; they survive.
        (await Db.Items.CountAsync(i => i.Name == "Drill")).ShouldBe(1);
    }
}
