using System.Text.Json;
using Application.AnthropicPorts;
using Application.DTOs;
using Application.Services;
using Application.ServicePorts;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Application.Tests.Services;

public class ChatToolDispatcherTests
{
    private readonly ILocationService _locationService = Substitute.For<ILocationService>();
    private readonly IItemService _itemService = Substitute.For<IItemService>();
    private readonly ChatToolDispatcher _dispatcher;

    public ChatToolDispatcherTests()
    {
        _dispatcher = new ChatToolDispatcher(_locationService, _itemService, NullLogger<ChatToolDispatcher>.Instance);
    }

    /** A fully-populated ItemResponse with sensible defaults; shared with ChatServiceTests. */
    public static ItemResponse Item(int id, string name, int? roomId = 1, int? containerId = null) =>
        new(id, name, null, [], null, null, null, null, null, null, 1, null, null, null, null,
            false, false, false, null, roomId, containerId, null, null, null, null,
            DateTime.UnixEpoch, null, [1, 2, 3]);

    private static AnthropicToolUseBlock Call(string toolName, string inputJson, string id = "toolu_1")
    {
        using var document = JsonDocument.Parse(inputJson);
        return new AnthropicToolUseBlock(id, toolName, document.RootElement.Clone());
    }

    [Fact]
    public async Task ExecuteAsync_UnknownTool_ReturnsErrorResult()
    {
        var result = await _dispatcher.ExecuteAsync(Call("summon_gnomes", "{}"));

        result.IsError.ShouldBeTrue();
        result.ToolUseId.ShouldBe("toolu_1");
        result.Content.ShouldContain("summon_gnomes");
    }

    [Fact]
    public async Task ExecuteAsync_GetItem_ReturnsItemAndLocationPath()
    {
        _itemService.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(Item(42, "Drill"));
        _itemService.GetLocationPathAsync(42, Arg.Any<CancellationToken>()).Returns(
            new ItemLocationPathResponse(42, "Drill", 1, "Home", 2, "Ground floor", 3, "Garage",
                [new ContainerPathStep(7, "Red toolbox")]));

        var result = await _dispatcher.ExecuteAsync(Call("get_item", """{"id":42}"""));

        result.IsError.ShouldBeFalse();
        result.Content.ShouldContain("Drill");
        result.Content.ShouldContain("Garage");
        result.Content.ShouldContain("Red toolbox");
    }

    [Fact]
    public async Task ExecuteAsync_GetItem_WithoutId_ReturnsValidationError()
    {
        var result = await _dispatcher.ExecuteAsync(Call("get_item", "{}"));

        result.IsError.ShouldBeTrue();
        result.Content.ShouldContain("id");
    }

    [Fact]
    public async Task ExecuteAsync_GetItem_WhenPathUnresolvable_StillReturnsItem()
    {
        _itemService.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(Item(42, "Drill", roomId: null));
        _itemService.GetLocationPathAsync(42, Arg.Any<CancellationToken>())
            .Returns<ItemLocationPathResponse>(_ => throw NotFoundException.For("Item", 42));

        var result = await _dispatcher.ExecuteAsync(Call("get_item", """{"id":42}"""));

        result.IsError.ShouldBeFalse();
        result.Content.ShouldContain("Drill");
    }

    [Fact]
    public async Task ExecuteAsync_SearchItems_MapsArgumentsToQuery()
    {
        ItemSearchQuery? captured = null;
        _itemService.GetAllAsync(Arg.Do<ItemSearchQuery>(q => captured = q), Arg.Any<CancellationToken>())
            .Returns(new PagedResponse<ItemResponse>([Item(1, "Drill")], 1, 1, 25, 1, false, false));

        var result = await _dispatcher.ExecuteAsync(
            Call("search_items", """{"query":"drill","roomId":3,"includeDeleted":true}"""));

        result.IsError.ShouldBeFalse();
        captured.ShouldNotBeNull();
        captured.Query.ShouldBe("drill");
        captured.RoomId.ShouldBe(3);
        captured.IncludeDeleted.ShouldBeTrue();
        captured.PageSize.ShouldBe(25);
    }

    [Fact]
    public async Task ExecuteAsync_CreateItem_DefaultsQuantityAndHidesFromScene()
    {
        CreateItemRequest? captured = null;
        _itemService.CreateAsync(Arg.Do<CreateItemRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(Item(10, "Hammer", roomId: null, containerId: 7));

        var result = await _dispatcher.ExecuteAsync(
            Call("create_item", """{"name":"Hammer","containerId":7,"itemTypes":["CleaningSupplies","bogus"]}"""));

        result.IsError.ShouldBeFalse();
        captured.ShouldNotBeNull();
        captured.Name.ShouldBe("Hammer");
        captured.ContainerId.ShouldBe(7);
        captured.RoomId.ShouldBeNull();
        captured.Quantity.ShouldBe(1);
        captured.IsShownInUI.ShouldBeFalse();
        // valid enum names parse (case-insensitively); unknown entries are dropped, not errors
        captured.ItemTypes.ShouldBe([ItemType.CleaningSupplies]);
    }

    [Fact]
    public async Task ExecuteAsync_UpdateItem_WithoutItemTypes_KeepsExistingTypes()
    {
        var existing = Item(42, "Drill") with { ItemTypes = [ItemType.Electronics] };
        _itemService.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(existing);

        UpdateItemRequest? captured = null;
        _itemService.UpdateAsync(Arg.Do<UpdateItemRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(existing);

        await _dispatcher.ExecuteAsync(Call("update_item", """{"id":42,"name":"Impact drill"}"""));

        captured.ShouldNotBeNull();
        captured.ItemTypes.ShouldBe([ItemType.Electronics]);
    }

    [Fact]
    public async Task ExecuteAsync_UpdateItem_MovingToContainer_ReplacesRoomPlacement()
    {
        _itemService.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(Item(42, "Drill", roomId: 3));

        UpdateItemRequest? captured = null;
        _itemService.UpdateAsync(Arg.Do<UpdateItemRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(Item(42, "Drill", roomId: null, containerId: 7));

        await _dispatcher.ExecuteAsync(Call("update_item", """{"id":42,"containerId":7}"""));

        captured.ShouldNotBeNull();
        captured.ContainerId.ShouldBe(7);
        captured.RoomId.ShouldBeNull(); // the old room placement must not survive the move
        captured.Name.ShouldBe("Drill"); // untouched fields carry over from the existing item
        captured.RowVersion.ShouldBe([1, 2, 3]);
    }

    [Fact]
    public async Task ExecuteAsync_UpdateItem_WithoutPlacementFields_KeepsCurrentPlacement()
    {
        _itemService.GetByIdAsync(42, Arg.Any<CancellationToken>()).Returns(Item(42, "Drill", roomId: 3));

        UpdateItemRequest? captured = null;
        _itemService.UpdateAsync(Arg.Do<UpdateItemRequest>(r => captured = r), Arg.Any<CancellationToken>())
            .Returns(Item(42, "Impact drill", roomId: 3));

        await _dispatcher.ExecuteAsync(Call("update_item", """{"id":42,"name":"Impact drill"}"""));

        captured.ShouldNotBeNull();
        captured.Name.ShouldBe("Impact drill");
        captured.RoomId.ShouldBe(3);
        captured.ContainerId.ShouldBeNull();
    }

    [Fact]
    public async Task ExecuteAsync_DeleteItem_WithValidReason_Deletes()
    {
        _itemService.DeleteAsync(42, DeletedReason.Donated, Arg.Any<CancellationToken>()).Returns(1);

        var result = await _dispatcher.ExecuteAsync(Call("delete_item", """{"id":42,"reason":"Donated"}"""));

        result.IsError.ShouldBeFalse();
        await _itemService.Received(1).DeleteAsync(42, DeletedReason.Donated, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_DeleteItem_WithBogusReason_ReturnsErrorWithoutDeleting()
    {
        var result = await _dispatcher.ExecuteAsync(Call("delete_item", """{"id":42,"reason":"FeltLikeIt"}"""));

        result.IsError.ShouldBeTrue();
        await _itemService.DidNotReceive().DeleteAsync(Arg.Any<int>(), Arg.Any<DeletedReason>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task ExecuteAsync_GetHouseStructure_ProjectsLocationsFloorsRoomsContainers()
    {
        _locationService.GetAllAsync(Arg.Any<PaginationQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResponse<LocationResponse>(
                [new LocationResponse(1, "Home", null, [], [9])], 1, 1, 50, 1, false, false));

        _locationService.GetMapAsync(1, Arg.Any<CancellationToken>()).Returns(new LocationMapResponse(
            1, "Home", null,
            [
                new FloorMap(2, "Ground floor", 0, null, null,
                [
                    new RoomMap(3, "Garage", null, null, null, null, null, null, null, null, null, null, null,
                        [new ContainerNode(7, "Red toolbox", null, null, null, null, null, null, null, null, null, null, [])],
                        [], []),
                ]),
            ]));

        var result = await _dispatcher.ExecuteAsync(Call("get_house_structure", "{}"));

        result.IsError.ShouldBeFalse();
        result.Content.ShouldContain("Home");
        result.Content.ShouldContain("Garage");
        result.Content.ShouldContain("Red toolbox");
        // token-diet check: the projection must not leak geometry fields
        result.Content.ShouldNotContain("widthInches");
    }
}
