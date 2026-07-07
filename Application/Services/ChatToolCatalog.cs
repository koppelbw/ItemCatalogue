using System.Text.Json;
using Application.AnthropicPorts;

namespace Application.Services;

// The tool surface exposed to the model: JSON Schemas describing each callable operation.
// Execution lives in ChatToolDispatcher; keeping definitions and dispatch separate means the
// schemas stay a readable, single-purpose catalog.
internal static class ChatToolCatalog
{
    public const string GetHouseStructure = "get_house_structure";
    public const string SearchItems = "search_items";
    public const string GetItem = "get_item";
    public const string CreateItem = "create_item";
    public const string UpdateItem = "update_item";
    public const string DeleteItem = "delete_item";

    public static readonly IReadOnlyList<AnthropicTool> All =
    [
        new AnthropicTool(
            GetHouseStructure,
            "Get every location with its floors, rooms, and containers (nested), including their ids. " +
            "Call this first to resolve names like 'the garage' or 'the red toolbox' to ids.",
            Schema("""
            {
              "type": "object",
              "properties": {},
              "required": []
            }
            """)),

        new AnthropicTool(
            SearchItems,
            "Search inventory items by free text and/or place. Returns up to 25 matches with their ids " +
            "and placement. Use roomId/containerId (from get_house_structure) to scope the search.",
            Schema("""
            {
              "type": "object",
              "properties": {
                "query": { "type": "string", "description": "Substring matched against item name and description." },
                "roomId": { "type": "integer", "description": "Only items directly in this room." },
                "containerId": { "type": "integer", "description": "Only items inside this container." },
                "includeDeleted": { "type": "boolean", "description": "Include soft-deleted items. Default false." }
              },
              "required": []
            }
            """)),

        new AnthropicTool(
            GetItem,
            "Get one item's full details plus its location path (location > floor > room > container chain).",
            Schema("""
            {
              "type": "object",
              "properties": {
                "id": { "type": "integer", "description": "The item id." }
              },
              "required": ["id"]
            }
            """)),

        new AnthropicTool(
            CreateItem,
            "Create a new inventory item. Place it either directly in a room (roomId) or inside a " +
            "container (containerId) - exactly one of the two. At least one itemType is required - " +
            "pick the closest match even if imperfect.",
            Schema("""
            {
              "type": "object",
              "properties": {
                "name": { "type": "string" },
                "description": { "type": "string" },
                "itemTypes": {
                  "type": "array",
                  "items": { "type": "string", "enum": ["Electronics", "Bathroom", "CleaningSupplies", "Bedding", "Books"] },
                  "description": "One or more categories. Required; choose the closest match."
                },
                "roomId": { "type": "integer", "description": "Room to place the item in directly. Mutually exclusive with containerId." },
                "containerId": { "type": "integer", "description": "Container to place the item in. Mutually exclusive with roomId." },
                "quantity": { "type": "integer", "description": "Default 1." },
                "brand": { "type": "string" },
                "model": { "type": "string" },
                "condition": { "type": "string", "enum": ["New", "LikeNew", "Good", "Fair", "Poor", "ForRepair", "Broken"] },
                "currentValue": { "type": "number", "description": "Estimated current value in dollars." }
              },
              "required": ["name", "itemTypes"]
            }
            """)),

        new AnthropicTool(
            UpdateItem,
            "Update an existing item. Only the provided fields change. To move an item, provide the new " +
            "roomId OR containerId - providing either replaces the item's current placement entirely.",
            Schema("""
            {
              "type": "object",
              "properties": {
                "id": { "type": "integer", "description": "The item id (required)." },
                "name": { "type": "string" },
                "description": { "type": "string" },
                "itemTypes": {
                  "type": "array",
                  "items": { "type": "string", "enum": ["Electronics", "Bathroom", "CleaningSupplies", "Bedding", "Books"] },
                  "description": "Replaces the item's categories when provided."
                },
                "roomId": { "type": "integer", "description": "New room placement. Mutually exclusive with containerId." },
                "containerId": { "type": "integer", "description": "New container placement. Mutually exclusive with roomId." },
                "quantity": { "type": "integer" },
                "brand": { "type": "string" },
                "model": { "type": "string" },
                "condition": { "type": "string", "enum": ["New", "LikeNew", "Good", "Fair", "Poor", "ForRepair", "Broken"] },
                "currentValue": { "type": "number" }
              },
              "required": ["id"]
            }
            """)),

        new AnthropicTool(
            DeleteItem,
            "Soft-delete an item. Only call this after the user has clearly confirmed the deletion of " +
            "this specific item in the conversation. A reason is required.",
            Schema("""
            {
              "type": "object",
              "properties": {
                "id": { "type": "integer", "description": "The item id." },
                "reason": { "type": "string", "enum": ["Used", "Broken", "Donated", "Gifted", "Lost"] }
              },
              "required": ["id", "reason"]
            }
            """)),
    ];

    // Clone() detaches the schema from the parsed JsonDocument so the static instances outlive it.
    private static JsonElement Schema(string json)
    {
        using var document = JsonDocument.Parse(json);
        return document.RootElement.Clone();
    }
}
