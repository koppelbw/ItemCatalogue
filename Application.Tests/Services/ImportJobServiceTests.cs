using Application.DTOs;
using Application.Mapping;
using Application.Options;
using Application.Services;
using Application.StoragePorts;
using Application.Validation;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.RepositoryPorts;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Shouldly;

namespace Application.Tests.Services;

public class ImportJobServiceTests
{
    private readonly ICsvItemParser _parser = Substitute.For<ICsvItemParser>();
    private readonly IImportPayloadStore _payloadStore = Substitute.For<IImportPayloadStore>();
    private readonly IImportDispatcher _dispatcher = Substitute.For<IImportDispatcher>();
    private readonly IImportJobRepository _jobRepository = Substitute.For<IImportJobRepository>();
    private readonly IRoomRepository _roomRepository = Substitute.For<IRoomRepository>();
    private readonly IContainerRepository _containerRepository = Substitute.For<IContainerRepository>();
    private readonly IPersonRepository _personRepository = Substitute.For<IPersonRepository>();
    private readonly ImportJobService _service;

    public ImportJobServiceTests()
    {
        // ChunkSize 2 keeps the chunk math visible in assertions; MaxRows 10 makes the cap testable.
        _service = new ImportJobService(
            _parser,
            _payloadStore,
            _dispatcher,
            _jobRepository,
            new ItemBulkPreparer(new CreateItemRequestValidator(), _roomRepository, _containerRepository, _personRepository),
            TimeProvider.System,
            Microsoft.Extensions.Options.Options.Create(new ImportOptions { ChunkSize = 2, MaxRows = 10 }),
            NullLogger<ImportJobService>.Instance);

        // The repository assigns the identity the way SaveChanges would.
        _jobRepository.InsertAsync(Arg.Any<ImportJob>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                call.Arg<ImportJob>().Id = 42;
                return 42;
            });
        _jobRepository.RecordChunkAsync(Arg.Any<ImportChunk>(), Arg.Any<IReadOnlyCollection<Item>>(), Arg.Any<CancellationToken>())
            .Returns(true);

        // The CSV carries reference ids directly; the only reference validation is the preparer's
        // chunk-time FK re-check, which here treats every id as existing.
        EchoExistingIds(_roomRepository);
        EchoExistingIds(_containerRepository);
        EchoExistingIds(_personRepository);
    }

    private static void EchoExistingIds<TEntity>(IGenericRepository<TEntity> repository)
        where TEntity : class, IEntity
        => repository.FilterExistingIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(call => (IReadOnlyList<int>)call.Arg<IReadOnlyCollection<int>>().ToList());

    private void ParserReturns(IReadOnlyList<CsvItemRow> rows, IReadOnlyList<ImportRowError>? errors = null)
        => _parser.ParseAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns(new CsvParseResult(rows, errors ?? []));

    private static CsvItemRow Row(int rowNumber, string name = "Lamp", int? roomId = null, int? containerId = null, int? ownerId = null) =>
        new(rowNumber, name, null, [ItemType.Electronics], null, null, null, null, null, null, 1,
            null, null, null, null, false, true, roomId, containerId, ownerId, null, null, null);

    private Task<ImportJobResponse> StartAsync() => _service.StartImportAsync(Stream.Null, "items.csv");

    [Fact]
    public async Task StartImportAsync_PassesReferenceIdsThroughToPayload_AndDispatchesChunks()
    {
        ParserReturns([Row(2, "A", roomId: 7), Row(3, "B", ownerId: 3), Row(4, "C")]);

        var response = await StartAsync();

        response.Id.ShouldBe(42);
        response.Status.ShouldBe(ImportJobStatus.Queued);
        response.TotalRows.ShouldBe(3);
        response.EnqueuedRows.ShouldBe(3);
        response.TotalChunks.ShouldBe(2);

        // Reference ids flow straight from the parsed row onto the request — no resolution.
        await _payloadStore.Received(1).WriteAsync(42, Arg.Is<IReadOnlyList<ImportPayloadRow>>(rows =>
            rows.Count == 3 &&
            rows[0].RowNumber == 2 && rows[0].Item.RoomId == 7 &&
            rows[1].RowNumber == 3 && rows[1].Item.OwnerId == 3 &&
            rows[2].Item.RoomId == null), Arg.Any<CancellationToken>());

        // 3 rows at chunk size 2 -> (0: rows 0-1) and (1: row 2).
        await _dispatcher.Received(1).DispatchAsync(42, Arg.Is<IReadOnlyList<ChunkRef>>(chunks =>
            chunks.Count == 2 &&
            chunks[0] == new ChunkRef(0, 0, 2) &&
            chunks[1] == new ChunkRef(1, 2, 1)), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartImportAsync_ParseErrors_CountTowardTotalsAndIntakeRejections()
    {
        ParserReturns([Row(3, "B")], [new ImportRowError(2, ["'abc' is not a valid price."])]);

        var response = await StartAsync();

        response.TotalRows.ShouldBe(2);
        response.RejectedAtIntake.ShouldBe(1);
        response.EnqueuedRows.ShouldBe(1);
        response.Errors.ShouldHaveSingleItem().RowNumber.ShouldBe(2);
    }

    [Fact]
    public async Task StartImportAsync_EmptyFile_ThrowsValidation()
    {
        ParserReturns([]);

        var ex = await Should.ThrowAsync<ValidationException>(StartAsync);
        ex.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldBe("The file contains no data rows.");
        await _jobRepository.DidNotReceive().InsertAsync(Arg.Any<ImportJob>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartImportAsync_MoreRowsThanMaxRows_ThrowsValidation()
    {
        ParserReturns(Enumerable.Range(2, 11).Select(i => Row(i)).ToList());

        var ex = await Should.ThrowAsync<ValidationException>(StartAsync);
        ex.Errors.ShouldHaveSingleItem().ErrorMessage.ShouldContain("maximum per import is 10");
    }

    [Fact]
    public async Task StartImportAsync_AllRowsRejected_SkipsPayloadAndDispatch_AndIsCompleted()
    {
        // Every data row failed to parse, so none survive to be enqueued.
        ParserReturns([], [new ImportRowError(2, ["'abc' is not a valid PurchasePrice."])]);

        var response = await StartAsync();

        response.EnqueuedRows.ShouldBe(0);
        response.TotalChunks.ShouldBe(0);
        // Nothing to process, so the job is terminal immediately.
        response.Status.ShouldBe(ImportJobStatus.Completed);
        await _payloadStore.DidNotReceive().WriteAsync(Arg.Any<int>(), Arg.Any<IReadOnlyList<ImportPayloadRow>>(), Arg.Any<CancellationToken>());
        await _dispatcher.DidNotReceive().DispatchAsync(Arg.Any<int>(), Arg.Any<IReadOnlyList<ChunkRef>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessChunkAsync_RecordsOutcome_WithErrorsKeyedByCsvRowNumber()
    {
        var valid = Row(2, "Drill").ToCreateRequest();
        var invalid = Row(3, "").ToCreateRequest();
        _payloadStore.ReadChunkAsync(42, 0, 2, Arg.Any<CancellationToken>())
            .Returns([new ImportPayloadRow(2, valid), new ImportPayloadRow(3, invalid)]);

        await _service.ProcessChunkAsync(new ImportChunkMessage(42, 0, 0, 2));

        await _jobRepository.Received(1).RecordChunkAsync(
            Arg.Is<ImportChunk>(c =>
                c.JobId == 42 &&
                c.ChunkIndex == 0 &&
                c.Succeeded == 1 &&
                c.Failed == 1 &&
                c.ErrorsJson!.Contains("\"RowNumber\":3")),
            Arg.Is<IReadOnlyCollection<Item>>(items => items.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ProcessChunkAsync_RedeliveredMessage_CompletesWithoutThrowing()
    {
        _jobRepository.RecordChunkAsync(Arg.Any<ImportChunk>(), Arg.Any<IReadOnlyCollection<Item>>(), Arg.Any<CancellationToken>())
            .Returns(false);
        _payloadStore.ReadChunkAsync(42, 0, 1, Arg.Any<CancellationToken>())
            .Returns([new ImportPayloadRow(2, Row(2, "Drill").ToCreateRequest())]);

        await Should.NotThrowAsync(() => _service.ProcessChunkAsync(new ImportChunkMessage(42, 0, 0, 1)));
    }

    [Fact]
    public async Task MarkChunkFailedAsync_PayloadReadable_RecordsPerRowFailuresWithNoItems()
    {
        _payloadStore.ReadChunkAsync(42, 0, 2, Arg.Any<CancellationToken>())
            .Returns([
                new ImportPayloadRow(2, Row(2, "Drill").ToCreateRequest()),
                new ImportPayloadRow(3, Row(3, "Ladder").ToCreateRequest()),
            ]);

        await _service.MarkChunkFailedAsync(new ImportChunkMessage(42, 0, 0, 2), "poisoned");

        await _jobRepository.Received(1).RecordChunkAsync(
            Arg.Is<ImportChunk>(c =>
                c.JobId == 42 &&
                c.Succeeded == 0 &&
                c.Failed == 2 &&
                c.ErrorsJson!.Contains("\"RowNumber\":2") &&
                c.ErrorsJson.Contains("\"RowNumber\":3") &&
                c.ErrorsJson.Contains("poisoned")),
            Arg.Is<IReadOnlyCollection<Item>>(items => items.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task MarkChunkFailedAsync_PayloadUnreadable_StillRecordsAnAggregateFailure()
    {
        // The most likely reason a chunk poisons is that its payload cannot be read — the failure
        // marker must not depend on reading it again.
        _payloadStore.ReadChunkAsync(42, 3, 25, Arg.Any<CancellationToken>())
            .Returns<IReadOnlyList<ImportPayloadRow>>(_ => throw new InvalidOperationException("blob missing"));

        await _service.MarkChunkFailedAsync(new ImportChunkMessage(42, 3, 3, 25), "poisoned");

        await _jobRepository.Received(1).RecordChunkAsync(
            Arg.Is<ImportChunk>(c => c.Failed == 25 && c.ErrorsJson!.Contains("25 row(s) in chunk 3")),
            Arg.Is<IReadOnlyCollection<Item>>(items => items.Count == 0),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetStatusAsync_DerivesProgressAndMergesErrorsFromMarkers()
    {
        _jobRepository.GetWithChunksAsync(42, Arg.Any<CancellationToken>()).Returns(new ImportJob
        {
            Id = 42,
            FileName = "items.csv",
            TotalRows = 5,
            RejectedAtIntake = 1,
            EnqueuedRows = 4,
            TotalChunks = 2,
            IntakeErrorsJson = """[{"RowNumber":2,"Messages":["Unknown Room 'Attic'."]}]""",
            Chunks =
            [
                new ImportChunk { ChunkIndex = 0, Succeeded = 1, Failed = 1, ErrorsJson = """[{"RowNumber":5,"Messages":["'Name' must not be empty."]}]""" },
            ],
        });

        var response = await _service.GetStatusAsync(42);

        response.Status.ShouldBe(ImportJobStatus.Processing);
        response.ProcessedChunks.ShouldBe(1);
        response.Succeeded.ShouldBe(1);
        // 1 intake rejection + 1 chunk failure.
        response.Failed.ShouldBe(2);
        response.Errors.Select(e => e.RowNumber).ShouldBe([2, 5]);
    }

    [Fact]
    public async Task GetStatusAsync_AllChunksRecorded_IsCompleted()
    {
        _jobRepository.GetWithChunksAsync(42, Arg.Any<CancellationToken>()).Returns(new ImportJob
        {
            Id = 42,
            FileName = "items.csv",
            TotalRows = 4,
            EnqueuedRows = 4,
            TotalChunks = 2,
            Chunks =
            [
                new ImportChunk { ChunkIndex = 0, Succeeded = 2 },
                new ImportChunk { ChunkIndex = 1, Succeeded = 2 },
            ],
        });

        var response = await _service.GetStatusAsync(42);

        response.Status.ShouldBe(ImportJobStatus.Completed);
        response.Succeeded.ShouldBe(4);
        response.Failed.ShouldBe(0);
    }

    [Fact]
    public async Task GetStatusAsync_UnknownJob_ThrowsNotFound()
    {
        _jobRepository.GetWithChunksAsync(99, Arg.Any<CancellationToken>()).Returns((ImportJob?)null);

        var ex = await Should.ThrowAsync<NotFoundException>(() => _service.GetStatusAsync(99));
        ex.Message.ShouldBe("ImportJob with id 99 not found.");
    }
}
