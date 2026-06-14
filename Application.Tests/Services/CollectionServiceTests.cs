using Application.DTOs;
using Application.Services;
using Application.Validation;
using Domain.Entities;
using Domain.Exceptions;
using Domain.RepositoryPorts;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Application.Tests.Services;

public class CollectionServiceTests
{
    private readonly ICollectionRepository _repository = Substitute.For<ICollectionRepository>();
    private readonly IItemRepository _itemRepository = Substitute.For<IItemRepository>();
    private readonly CollectionService _service;

    public CollectionServiceTests()
    {
        _service = new CollectionService(
            _repository,
            _itemRepository,
            new CreateCollectionRequestValidator(),
            new UpdateCollectionRequestValidator(),
            new AddCollectionItemRequestValidator(),
            new UpdateCollectionItemRequestValidator(),
            NullLogger<CollectionService>.Instance);
    }

    private static Collection Existing(int id = 1, params CollectionItem[] items) =>
        new() { Id = id, Name = "Office Kit", RowVersion = [1, 2, 3], Items = [.. items] };

    [Fact]
    public async Task GetByIdAsync_WhenMissing_ThrowsNotFound()
    {
        _repository.GetByIdAsync(5, Arg.Any<CancellationToken>()).Returns((Collection?)null);

        var ex = await Should.ThrowAsync<NotFoundException>(() => _service.GetByIdAsync(5));
        ex.Message.ShouldBe("Collection with id 5 not found.");
    }

    [Fact]
    public async Task CreateAsync_InsertsAndReturnsResponse()
    {
        _repository.When(r => r.InsertAsync(Arg.Any<Collection>(), Arg.Any<CancellationToken>()))
            .Do(ci => ci.Arg<Collection>().Id = 10);

        var response = await _service.CreateAsync(new CreateCollectionRequest("Office Kit", "Desk setup"));

        response.Id.ShouldBe(10);
        response.Name.ShouldBe("Office Kit");
    }

    [Fact]
    public async Task AddItemAsync_WhenItemMissing_ThrowsNotFoundItemAndDoesNotSave()
    {
        _itemRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns((Item?)null);

        var ex = await Should.ThrowAsync<NotFoundException>(
            () => _service.AddItemAsync(1, new AddCollectionItemRequest(7, 1, null, null)));
        ex.Message.ShouldBe("Item with id 7 not found.");

        await _repository.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddItemAsync_WhenCollectionMissing_ThrowsNotFoundCollection()
    {
        _itemRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(new Item { Id = 7, Name = "Laptop" });
        _repository.GetForUpdateWithItemsAsync(1, Arg.Any<CancellationToken>()).Returns((Collection?)null);

        await Should.ThrowAsync<NotFoundException>(
            () => _service.AddItemAsync(1, new AddCollectionItemRequest(7, 1, null, null)));
    }

    [Fact]
    public async Task AddItemAsync_WhenAlreadyMember_ThrowsValidationAndDoesNotSave()
    {
        _itemRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(new Item { Id = 7, Name = "Laptop" });
        _repository.GetForUpdateWithItemsAsync(1, Arg.Any<CancellationToken>())
            .Returns(Existing(1, new CollectionItem { CollectionId = 1, ItemId = 7 }));

        await Should.ThrowAsync<ValidationException>(
            () => _service.AddItemAsync(1, new AddCollectionItemRequest(7, 1, null, null)));

        await _repository.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task AddItemAsync_HappyPath_AddsMembershipWithPayloadAndSaves()
    {
        var tracked = Existing(1);
        _itemRepository.GetByIdAsync(7, Arg.Any<CancellationToken>()).Returns(new Item { Id = 7, Name = "Laptop" });
        _repository.GetForUpdateWithItemsAsync(1, Arg.Any<CancellationToken>()).Returns(tracked);
        // The service re-fetches through the read path to build the response.
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Existing(1,
            new CollectionItem { CollectionId = 1, ItemId = 7, Item = new Item { Id = 7, Name = "Laptop" }, Quantity = 2, SortOrder = 3, Role = "Primary" }));

        var response = await _service.AddItemAsync(1, new AddCollectionItemRequest(7, 2, 3, "Primary"));

        tracked.Items.ShouldContain(ci => ci.ItemId == 7 && ci.Quantity == 2 && ci.SortOrder == 3 && ci.Role == "Primary");
        await _repository.Received(1).SaveAsync(Arg.Any<CancellationToken>());
        response.Items.ShouldContain(i => i.ItemId == 7 && i.ItemName == "Laptop");
    }

    [Fact]
    public async Task UpdateItemAsync_WhenMembershipMissing_ThrowsNotFound()
    {
        _repository.GetForUpdateWithItemsAsync(1, Arg.Any<CancellationToken>()).Returns(Existing(1));

        await Should.ThrowAsync<NotFoundException>(
            () => _service.UpdateItemAsync(1, 7, new UpdateCollectionItemRequest(2, null, null)));

        await _repository.DidNotReceive().SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateItemAsync_HappyPath_UpdatesPayloadAndSaves()
    {
        var membership = new CollectionItem { CollectionId = 1, ItemId = 7, Quantity = 1, SortOrder = 0, Role = "Old" };
        _repository.GetForUpdateWithItemsAsync(1, Arg.Any<CancellationToken>()).Returns(Existing(1, membership));
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Existing(1, membership));

        await _service.UpdateItemAsync(1, 7, new UpdateCollectionItemRequest(5, 9, "New"));

        membership.Quantity.ShouldBe(5);
        membership.SortOrder.ShouldBe(9);
        membership.Role.ShouldBe("New");
        await _repository.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task UpdateItemAsync_WithNullSortOrder_KeepsExistingOrder()
    {
        var membership = new CollectionItem { CollectionId = 1, ItemId = 7, Quantity = 1, SortOrder = 4, Role = "Old" };
        _repository.GetForUpdateWithItemsAsync(1, Arg.Any<CancellationToken>()).Returns(Existing(1, membership));
        _repository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(Existing(1, membership));

        await _service.UpdateItemAsync(1, 7, new UpdateCollectionItemRequest(2, null, "New"));

        membership.SortOrder.ShouldBe(4);
    }

    [Fact]
    public async Task RemoveItemAsync_WhenMembershipMissing_ThrowsNotFound()
    {
        _repository.GetForUpdateWithItemsAsync(1, Arg.Any<CancellationToken>()).Returns(Existing(1));

        await Should.ThrowAsync<NotFoundException>(() => _service.RemoveItemAsync(1, 7));
    }

    [Fact]
    public async Task RemoveItemAsync_HappyPath_RemovesAndSaves()
    {
        var membership = new CollectionItem { CollectionId = 1, ItemId = 7 };
        var tracked = Existing(1, membership);
        _repository.GetForUpdateWithItemsAsync(1, Arg.Any<CancellationToken>()).Returns(tracked);

        await _service.RemoveItemAsync(1, 7);

        tracked.Items.ShouldBeEmpty();
        await _repository.Received(1).SaveAsync(Arg.Any<CancellationToken>());
    }
}
