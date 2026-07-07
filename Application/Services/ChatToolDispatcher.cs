using System.Text.Json;
using System.Text.Json.Serialization;
using Application.AnthropicPorts;
using Application.DTOs;
using Application.Logging;
using Application.ServicePorts;
using Domain.Enums;
using Domain.Exceptions;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Services;

// Executes the model's tool calls against the existing inventory services and packages the outcome
// as a tool_result. Business failures (validation, not-found, in-use) are returned to the model as
// error results - not thrown - so it can correct itself or explain the problem to the user;
// anything unexpected still bubbles to the global exception handler.
public sealed class ChatToolDispatcher(
    ILocationService locationService,
    IItemService itemService,
    ILogger<ChatToolDispatcher> logger)
{
    // Slim, camelCase JSON keeps tool results cheap: every character goes back through the model
    // as billed input tokens.
    private static readonly JsonSerializerOptions ResultOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    public async Task<AnthropicToolResultBlock> ExecuteAsync(AnthropicToolUseBlock toolUse, CancellationToken cancellationToken = default)
    {
        try
        {
            var result = toolUse.Name switch
            {
                ChatToolCatalog.GetHouseStructure => await GetHouseStructureAsync(cancellationToken),
                ChatToolCatalog.SearchItems => await SearchItemsAsync(toolUse.Input, cancellationToken),
                ChatToolCatalog.GetItem => await GetItemAsync(toolUse.Input, cancellationToken),
                ChatToolCatalog.CreateItem => await CreateItemAsync(toolUse.Input, cancellationToken),
                ChatToolCatalog.UpdateItem => await UpdateItemAsync(toolUse.Input, cancellationToken),
                ChatToolCatalog.DeleteItem => await DeleteItemAsync(toolUse.Input, cancellationToken),
                _ => Error($"Unknown tool '{toolUse.Name}'."),
            };

            return new AnthropicToolResultBlock(toolUse.Id, result.Content, result.IsError);
        }
        catch (ValidationException ex)
        {
            var failures = string.Join("; ", ex.Errors.Select(e => $"{e.PropertyName}: {e.ErrorMessage}"));
            logger.ChatToolFailed(toolUse.Name, failures);
            return new AnthropicToolResultBlock(toolUse.Id, $"Validation failed: {failures}", IsError: true);
        }
        catch (NotFoundException ex)
        {
            logger.ChatToolFailed(toolUse.Name, ex.Message);
            return new AnthropicToolResultBlock(toolUse.Id, ex.Message, IsError: true);
        }
        catch (EntityInUseException ex)
        {
            logger.ChatToolFailed(toolUse.Name, ex.Message);
            return new AnthropicToolResultBlock(toolUse.Id, ex.Message, IsError: true);
        }
    }

    private async Task<(string Content, bool IsError)> GetHouseStructureAsync(CancellationToken cancellationToken)
    {
        var locations = await locationService.GetAllAsync(new PaginationQuery { Page = 1, PageSize = 50 }, cancellationToken);

        var structure = new List<object>();
        foreach (var location in locations.Items)
        {
            var map = await locationService.GetMapAsync(location.Id, cancellationToken);
            structure.Add(new
            {
                map.Id,
                map.Name,
                Floors = map.Floors.Select(f => new
                {
                    f.Id,
                    f.Name,
                    f.LevelIndex,
                    Rooms = f.Rooms.Select(r => new
                    {
                        r.Id,
                        r.Name,
                        r.RoomType,
                        Containers = r.Containers.Select(SlimContainer),
                    }),
                }),
            });
        }

        return Ok(new { Locations = structure });
    }

    private static object SlimContainer(ContainerNode node) => new
    {
        node.Id,
        node.Name,
        node.ContainerType,
        Children = node.Children.Select(SlimContainer),
    };

    private async Task<(string Content, bool IsError)> SearchItemsAsync(JsonElement input, CancellationToken cancellationToken)
    {
        var query = new ItemSearchQuery
        {
            Page = 1,
            PageSize = 25,
            Query = GetString(input, "query"),
            RoomId = GetInt(input, "roomId"),
            ContainerId = GetInt(input, "containerId"),
            IncludeDeleted = GetBool(input, "includeDeleted") ?? false,
        };

        var page = await itemService.GetAllAsync(query, cancellationToken);

        return Ok(new
        {
            page.TotalCount,
            Showing = page.Items.Count,
            Items = page.Items.Select(SlimItem),
        });
    }

    private async Task<(string Content, bool IsError)> GetItemAsync(JsonElement input, CancellationToken cancellationToken)
    {
        var id = RequireInt(input, "id");
        var item = await itemService.GetByIdAsync(id, cancellationToken);

        object? path = null;
        try
        {
            var location = await itemService.GetLocationPathAsync(id, cancellationToken);
            path = new
            {
                location.LocationId,
                location.LocationName,
                location.FloorId,
                location.FloorName,
                location.RoomId,
                location.RoomName,
                ContainerPath = location.ContainerPath.Select(c => new { c.Id, c.Name }),
            };
        }
        catch (NotFoundException)
        {
            // The item exists but has no resolvable placement chain; return it without a path.
        }

        return Ok(new { Item = SlimItem(item), LocationPath = path });
    }

    private async Task<(string Content, bool IsError)> CreateItemAsync(JsonElement input, CancellationToken cancellationToken)
    {
        var request = new CreateItemRequest(
            Name: GetString(input, "name") ?? string.Empty,
            Description: GetString(input, "description"),
            // Empty when the model omitted/mangled it; CreateItemRequestValidator then rejects with
            // "ItemTypes must not be empty", which flows back to the model as an error tool-result.
            ItemTypes: GetEnumList<ItemType>(input, "itemTypes"),
            PurchasePrice: null,
            CurrentValue: GetDecimal(input, "currentValue"),
            Brand: GetString(input, "brand"),
            Model: GetString(input, "model"),
            SerialNumber: null,
            PurchasedFrom: null,
            Quantity: GetInt(input, "quantity") ?? 1,
            Condition: GetEnum<Condition>(input, "condition"),
            AcquisitionType: null,
            PurchaseDate: null,
            WarrantyExpiryDate: null,
            IsStored: false,
            // Chat-created items have no 3D geometry, so they are list-only by default.
            IsShownInUI: false,
            RoomId: GetInt(input, "roomId"),
            ContainerId: GetInt(input, "containerId"),
            OwnerId: null,
            ReleaseDate: null,
            ValuationDate: null,
            AcquisitionReference: null);

        var created = await itemService.CreateAsync(request, cancellationToken);
        return Ok(new { Created = SlimItem(created) });
    }

    private async Task<(string Content, bool IsError)> UpdateItemAsync(JsonElement input, CancellationToken cancellationToken)
    {
        var id = RequireInt(input, "id");
        var existing = await itemService.GetByIdAsync(id, cancellationToken);

        // Providing either placement field replaces the placement entirely (an item lives in a room
        // OR a container, never both); omitting both keeps the current placement.
        var movesItem = input.TryGetProperty("roomId", out _) || input.TryGetProperty("containerId", out _);
        var roomId = movesItem ? GetInt(input, "roomId") : existing.RoomId;
        var containerId = movesItem ? GetInt(input, "containerId") : existing.ContainerId;

        var newTypes = GetEnumList<ItemType>(input, "itemTypes");

        var request = new UpdateItemRequest(
            Id: id,
            Name: GetString(input, "name") ?? existing.Name,
            Description: GetString(input, "description") ?? existing.Description,
            ItemTypes: newTypes.Count > 0 ? newTypes : existing.ItemTypes,
            PurchasePrice: existing.PurchasePrice,
            CurrentValue: GetDecimal(input, "currentValue") ?? existing.CurrentValue,
            Brand: GetString(input, "brand") ?? existing.Brand,
            Model: GetString(input, "model") ?? existing.Model,
            SerialNumber: existing.SerialNumber,
            PurchasedFrom: existing.PurchasedFrom,
            Quantity: GetInt(input, "quantity") ?? existing.Quantity,
            Condition: GetEnum<Condition>(input, "condition") ?? existing.Condition,
            AcquisitionType: existing.AcquisitionType,
            PurchaseDate: existing.PurchaseDate,
            WarrantyExpiryDate: existing.WarrantyExpiryDate,
            IsStored: existing.IsStored,
            IsShownInUI: existing.IsShownInUI,
            RoomId: roomId,
            ContainerId: containerId,
            OwnerId: existing.OwnerId,
            ReleaseDate: existing.ReleaseDate,
            ValuationDate: existing.ValuationDate,
            AcquisitionReference: existing.AcquisitionReference,
            RowVersion: existing.RowVersion);

        var updated = await itemService.UpdateAsync(request, cancellationToken);
        return Ok(new { Updated = SlimItem(updated) });
    }

    private async Task<(string Content, bool IsError)> DeleteItemAsync(JsonElement input, CancellationToken cancellationToken)
    {
        var id = RequireInt(input, "id");
        var reason = GetEnum<DeletedReason>(input, "reason");
        if (reason is null)
        {
            return Error("A valid reason is required: Used, Broken, Donated, Gifted, or Lost.");
        }

        var rows = await itemService.DeleteAsync(id, reason.Value, cancellationToken);
        return Ok(new { Deleted = rows > 0, ItemId = id, Reason = reason.Value });
    }

    private static object SlimItem(ItemResponse item) => new
    {
        item.Id,
        item.Name,
        item.Description,
        item.Quantity,
        item.Brand,
        item.Model,
        item.Condition,
        item.CurrentValue,
        item.RoomId,
        item.ContainerId,
        IsDeleted = item.IsDeleted ? true : (bool?)null,
        item.ReasonForDeletion,
    };

    private static (string Content, bool IsError) Ok(object result) =>
        (JsonSerializer.Serialize(result, ResultOptions), false);

    private static (string Content, bool IsError) Error(string message) => (message, true);

    // The model controls tool inputs, so every read tolerates missing properties and JSON nulls.
    private static string? GetString(JsonElement input, string name) =>
        input.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static int? GetInt(JsonElement input, string name) =>
        input.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetInt32()
            : null;

    private static decimal? GetDecimal(JsonElement input, string name) =>
        input.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Number
            ? value.GetDecimal()
            : null;

    private static bool? GetBool(JsonElement input, string name) =>
        input.TryGetProperty(name, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
            ? value.GetBoolean()
            : null;

    private static TEnum? GetEnum<TEnum>(JsonElement input, string name) where TEnum : struct, Enum =>
        GetString(input, name) is { } text && Enum.TryParse<TEnum>(text, ignoreCase: true, out var parsed)
            ? parsed
            : null;

    // Parses an array of enum names, silently dropping entries that don't parse - the schemas
    // constrain the values, but the model is still an untrusted client.
    private static List<TEnum> GetEnumList<TEnum>(JsonElement input, string name) where TEnum : struct, Enum =>
        input.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.Array
            ? [.. value.EnumerateArray()
                .Where(e => e.ValueKind == JsonValueKind.String)
                .Select(e => Enum.TryParse<TEnum>(e.GetString(), ignoreCase: true, out var parsed) ? parsed : (TEnum?)null)
                .OfType<TEnum>()
                .Distinct()]
            : [];

    private static int RequireInt(JsonElement input, string name) =>
        GetInt(input, name) ?? throw new ValidationException(
        [
            new FluentValidation.Results.ValidationFailure(name, $"Tool input '{name}' is required and must be an integer."),
        ]);
}
