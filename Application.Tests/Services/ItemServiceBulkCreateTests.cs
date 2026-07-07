using Application.DTOs;
using Application.Services;
using Application.Validation;
using Domain.Entities;
using Domain.Enums;
using Domain.RepositoryPorts;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Application.Tests.Services;

// CreateManyAsync is the transport-agnostic core of bulk import: every chunk processed by the
// queue-triggered Function funnels through it. These tests pin its partial-success contract —
// row-level failures (validation, dangling FKs) never reject neighboring rows.
public class ItemServiceBulkCreateTests
{
    private readonly IItemRepository _repository = Substitute.For<IItemRepository>();
    private readonly IRoomRepository _roomRepository = Substitute.For<IRoomRepository>();
    private readonly IContainerRepository _containerRepository = Substitute.For<IContainerRepository>();
    private readonly IPersonRepository _personRepository = Substitute.For<IPersonRepository>();
    private readonly ItemService _service;

    public ItemServiceBulkCreateTests()
    {
        // Real validators, faked repositories — same convention as ItemServiceTests.
        _service = new ItemService(
            _repository,
            Substitute.For<IItemEventRepository>(),
            _roomRepository,
            _containerRepository,
            _personRepository,
            TimeProvider.System,
            new CreateItemRequestValidator(),
            new UpdateItemRequestValidator(),
            new SetItemTagsRequestValidator(),
            new ItemSearchQueryValidator(),
            NullLogger<ItemService>.Instance);

        // Default: every referenced id exists (the repo echoes the requested ids back), and the
        // repository assigns ascending ids on insert the way SaveChanges would.
        EchoExistingIds(_roomRepository);
        EchoExistingIds(_containerRepository);
        EchoExistingIds(_personRepository);

        var nextId = 100;
        _repository.InsertRangeAsync(Arg.Any<IReadOnlyCollection<Item>>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(call =>
            {
                foreach (var item in call.Arg<IReadOnlyCollection<Item>>())
                {
                    item.Id = ++nextId;
                }
            });
    }

    private static void EchoExistingIds<TEntity>(IGenericRepository<TEntity> repository)
        where TEntity : class, IEntity
        => repository.FilterExistingIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns(call => (IReadOnlyList<int>)call.Arg<IReadOnlyCollection<int>>().ToList());

    private static void NoIdsExist<TEntity>(IGenericRepository<TEntity> repository)
        where TEntity : class, IEntity
        => repository.FilterExistingIdsAsync(Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>())
            .Returns((IReadOnlyList<int>)new List<int>());

    private static CreateItemRequest Valid(string name = "Lamp") =>
        new(name, null, [ItemType.Electronics], 5m, null, null, null, null, null, 1, null, null, null, null, false, true, null, null, null, null, null, null);

    [Fact]
    public async Task CreateManyAsync_AllRowsValid_InsertsAllInOneCall_AndReturnsIds()
    {
        var result = await _service.CreateManyAsync([Valid("A"), Valid("B"), Valid("C")]);

        result.Errors.ShouldBeEmpty();
        result.CreatedIds.Count.ShouldBe(3);
        result.CreatedIds.ShouldBe([101, 102, 103]);
        await _repository.Received(1).InsertRangeAsync(
            Arg.Is<IReadOnlyCollection<Item>>(items => items.Count == 3),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateManyAsync_InvalidRow_IsReportedByIndex_WhileValidRowsInsert()
    {
        // Row 1 fails two validation rules (empty name, no item types).
        var bad = Valid("") with { ItemTypes = [] };

        var result = await _service.CreateManyAsync([Valid("A"), bad, Valid("C")]);

        result.CreatedIds.Count.ShouldBe(2);
        var error = result.Errors.ShouldHaveSingleItem();
        error.Index.ShouldBe(1);
        error.Messages.Count.ShouldBe(2);
        await _repository.Received(1).InsertRangeAsync(
            Arg.Is<IReadOnlyCollection<Item>>(items => items.Count == 2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateManyAsync_DanglingRoomReference_BecomesRowError_NotException()
    {
        NoIdsExist(_roomRepository);

        var result = await _service.CreateManyAsync([Valid("A") with { RoomId = 999 }, Valid("B")]);

        result.CreatedIds.Count.ShouldBe(1);
        var error = result.Errors.ShouldHaveSingleItem();
        error.Index.ShouldBe(0);
        error.Messages.ShouldHaveSingleItem().ShouldBe("Room 999 does not exist.");
    }

    [Fact]
    public async Task CreateManyAsync_ChecksEachReferencedTableInOneBatchedQuery()
    {
        await _service.CreateManyAsync(
        [
            Valid("A") with { RoomId = 1 },
            Valid("B") with { RoomId = 2 },
            Valid("C") with { ContainerId = 3 },
            Valid("D") with { OwnerId = 4 },
        ]);

        // Two rows reference rooms, but the existence check is one query with both ids.
        await _roomRepository.Received(1).FilterExistingIdsAsync(
            Arg.Is<IReadOnlyCollection<int>>(ids => ids.Count == 2),
            Arg.Any<CancellationToken>());
        await _containerRepository.Received(1).FilterExistingIdsAsync(
            Arg.Is<IReadOnlyCollection<int>>(ids => ids.Count == 1),
            Arg.Any<CancellationToken>());
        await _personRepository.Received(1).FilterExistingIdsAsync(
            Arg.Is<IReadOnlyCollection<int>>(ids => ids.Count == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateManyAsync_RowFailingValidation_IsExcludedFromFkChecks()
    {
        var invalidWithRoom = Valid("") with { RoomId = 7 };

        await _service.CreateManyAsync([invalidWithRoom, Valid("B")]);

        // The invalid row's RoomId must not reach the FK query — no other row references a room,
        // so the room repository is never consulted at all.
        await _roomRepository.DidNotReceive().FilterExistingIdsAsync(
            Arg.Any<IReadOnlyCollection<int>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateManyAsync_AllRowsInvalid_NeverTouchesTheDatabase()
    {
        var result = await _service.CreateManyAsync([Valid(""), Valid("") with { Quantity = 0 }]);

        result.CreatedIds.ShouldBeEmpty();
        result.Errors.Count.ShouldBe(2);
        result.Errors.Select(e => e.Index).ShouldBe([0, 1]);
        await _repository.DidNotReceive().InsertRangeAsync(
            Arg.Any<IReadOnlyCollection<Item>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateManyAsync_EmptyInput_ReturnsEmptyResult()
    {
        var result = await _service.CreateManyAsync([]);

        result.CreatedIds.ShouldBeEmpty();
        result.Errors.ShouldBeEmpty();
        await _repository.DidNotReceive().InsertRangeAsync(
            Arg.Any<IReadOnlyCollection<Item>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateManyAsync_RowWithMultipleDanglingReferences_ReportsEachOne()
    {
        NoIdsExist(_containerRepository);
        NoIdsExist(_personRepository);

        var result = await _service.CreateManyAsync([Valid("A") with { ContainerId = 5, OwnerId = 6 }]);

        result.CreatedIds.ShouldBeEmpty();
        var error = result.Errors.ShouldHaveSingleItem();
        error.Messages.ShouldBe(["Container 5 does not exist.", "Person 6 does not exist."]);
    }
}
