using Application.DTOs;
using Application.Services;
using Application.Validation;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Pagination;
using Domain.RepositoryPorts;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Application.Tests.Services;

public class ItemServiceTests
{
    private readonly IItemRepository _repository = Substitute.For<IItemRepository>();
    private readonly ItemService _service;

    public ItemServiceTests()
    {
        // Real validators are injected so the service's validate-then-persist behavior is
        // exercised exactly as in production; only the repository (I/O) is faked.
        _service = new ItemService(
            _repository,
            new CreateItemRequestValidator(),
            new UpdateItemRequestValidator(),
            NullLogger<ItemService>.Instance);
    }

    private static Item Existing(int id = 1) => new()
    {
        Id = id,
        Name = "Lamp",
        ItemTypes = [ItemType.Electronics],
        RowVersion = [1, 2, 3],
    };

    private static CreateItemRequest ValidCreate() =>
        new("Lamp", null, [ItemType.Electronics], 5m, false, null, null);

    private static UpdateItemRequest ValidUpdate(int id = 1) =>
        new(id, "Lamp", null, [ItemType.Electronics], 5m, false, null, null, [1, 2, 3]);

    [Fact]
    public async Task GetByIdAsync_WhenFound_ReturnsMappedResponse()
    {
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Existing());

        var response = await _service.GetByIdAsync(1);

        response.Id.ShouldBe(1);
        response.Name.ShouldBe("Lamp");
    }

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ThrowsNotFound()
    {
        _repository.GetByIdAsync(99, Arg.Any<CancellationToken>()).Returns((Item?)null);

        var ex = await Should.ThrowAsync<NotFoundException>(() => _service.GetByIdAsync(99));
        ex.Message.ShouldBe("Item with id 99 not found.");
    }

    [Fact]
    public async Task GetAllAsync_MapsPageToResponse()
    {
        var page = new PagedResult<Item>([Existing(1), Existing(2)], TotalCount: 2, Page: 1, PageSize: 20);
        _repository.GetAllAsync(Arg.Any<PageRequest>(), Arg.Any<CancellationToken>()).Returns(page);

        var response = await _service.GetAllAsync(new PaginationQuery { Page = 1, PageSize = 20 });

        response.Items.Count.ShouldBe(2);
        response.TotalCount.ShouldBe(2);
    }

    [Fact]
    public async Task GetAllAsync_ClampsPaginationBeforeQueryingRepository()
    {
        _repository.GetAllAsync(Arg.Any<PageRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResult<Item>([], 0, 1, PageRequest.MaxPageSize));

        // PaginationQuery itself is range-bound, but the service must still funnel through
        // PageRequest.Create. Use an oversized size to prove it is clamped to MaxPageSize.
        await _service.GetAllAsync(new PaginationQuery { Page = 1, PageSize = PageRequest.MaxPageSize + 500 });

        await _repository.Received(1).GetAllAsync(
            Arg.Is<PageRequest>(p => p.PageSize == PageRequest.MaxPageSize),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_InsertsAndReturnsPersistedEntity()
    {
        // Simulate the database assigning the identity on insert.
        _repository.When(r => r.InsertAsync(Arg.Any<Item>(), Arg.Any<CancellationToken>()))
            .Do(ci => ci.Arg<Item>().Id = 42);

        var response = await _service.CreateAsync(ValidCreate());

        response.Id.ShouldBe(42);
        await _repository.Received(1).InsertAsync(Arg.Is<Item>(i => i.Name == "Lamp"), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task CreateAsync_WithInvalidRequest_ThrowsAndDoesNotInsert()
    {
        var invalid = ValidCreate() with { Name = "" };

        await Should.ThrowAsync<ValidationException>(() => _service.CreateAsync(invalid));

        await _repository.DidNotReceive().InsertAsync(Arg.Any<Item>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenFound_AppliesChangesAndUpdates()
    {
        var existing = Existing();
        _repository.GetForUpdateAsync(1, Arg.Any<CancellationToken>()).Returns(existing);

        var response = await _service.UpdateAsync(ValidUpdate() with { Name = "Renamed" });

        response.Name.ShouldBe("Renamed");
        existing.Name.ShouldBe("Renamed");
        await _repository.Received(1).UpdateAsync(existing, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WhenMissing_ThrowsNotFoundAndDoesNotUpdate()
    {
        _repository.GetForUpdateAsync(7, Arg.Any<CancellationToken>()).Returns((Item?)null);

        await Should.ThrowAsync<NotFoundException>(() => _service.UpdateAsync(ValidUpdate(7)));

        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Item>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidRequest_ThrowsBeforeTouchingRepository()
    {
        var invalid = ValidUpdate() with { RowVersion = [] };

        await Should.ThrowAsync<ValidationException>(() => _service.UpdateAsync(invalid));

        await _repository.DidNotReceive().GetForUpdateAsync(Arg.Any<int>(), Arg.Any<CancellationToken>());
        await _repository.DidNotReceive().UpdateAsync(Arg.Any<Item>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesWithReasonAndReturnsAffectedRows()
    {
        _repository.SoftDeleteItemByIdAsync(3, DeletedReason.Broken, Arg.Any<CancellationToken>()).Returns(1);

        var rows = await _service.DeleteAsync(3, DeletedReason.Broken);

        rows.ShouldBe(1);
        await _repository.Received(1).SoftDeleteItemByIdAsync(3, DeletedReason.Broken, Arg.Any<CancellationToken>());
    }
}
