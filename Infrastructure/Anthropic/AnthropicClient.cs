using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Application.AnthropicPorts;
using Microsoft.Extensions.Options;

namespace Infrastructure.Anthropic;

// Adapter for the Anthropic Messages API using a raw typed HttpClient (no SDK). Translates the
// Application-owned AnthropicModels to and from the snake_case wire JSON. BaseAddress and the
// x-api-key / anthropic-version headers are configured once in DependencyInjection.cs.
public sealed class AnthropicClient(HttpClient httpClient, IOptions<AnthropicOptions> options) : IAnthropicClient
{
    public async Task<AnthropicResponse> CreateMessageAsync(AnthropicRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(options.Value.ApiKey))
        {
            throw new InvalidOperationException(
                "Anthropic:ApiKey is not configured. Set it locally with " +
                "'dotnet user-secrets set \"Anthropic:ApiKey\" \"sk-ant-...\" --project ItemCatalogueAPI' " +
                "or via the Anthropic__ApiKey app setting in Azure.");
        }

        var body = new JsonObject
        {
            ["model"] = options.Value.Model,
            ["max_tokens"] = options.Value.MaxTokens,
            ["system"] = request.System,
            ["messages"] = new JsonArray([.. request.Messages.Select(SerializeMessage)]),
        };

        if (request.Tools.Count > 0)
        {
            body["tools"] = new JsonArray([.. request.Tools.Select(SerializeTool)]);
        }

        using var content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json");
        using var response = await httpClient.PostAsync("/v1/messages", content, cancellationToken);
        var payload = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw ToApiException((int)response.StatusCode, payload);
        }

        return ParseResponse(payload);
    }

    private static JsonNode SerializeMessage(AnthropicMessage message) => new JsonObject
    {
        ["role"] = message.Role,
        ["content"] = new JsonArray([.. message.Content.Select(SerializeBlock)]),
    };

    private static JsonNode SerializeBlock(AnthropicContentBlock block) => block switch
    {
        AnthropicTextBlock text => new JsonObject
        {
            ["type"] = "text",
            ["text"] = text.Text,
        },
        AnthropicToolUseBlock toolUse => new JsonObject
        {
            ["type"] = "tool_use",
            ["id"] = toolUse.Id,
            ["name"] = toolUse.Name,
            ["input"] = JsonNode.Parse(toolUse.Input.GetRawText()),
        },
        AnthropicToolResultBlock toolResult => new JsonObject
        {
            ["type"] = "tool_result",
            ["tool_use_id"] = toolResult.ToolUseId,
            ["content"] = toolResult.Content,
            ["is_error"] = toolResult.IsError,
        },
        _ => throw new InvalidOperationException($"Unsupported content block type {block.GetType().Name}."),
    };

    private static JsonNode SerializeTool(AnthropicTool tool) => new JsonObject
    {
        ["name"] = tool.Name,
        ["description"] = tool.Description,
        ["input_schema"] = JsonNode.Parse(tool.InputSchema.GetRawText()),
    };

    private static AnthropicResponse ParseResponse(string payload)
    {
        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;

        var blocks = new List<AnthropicContentBlock>();
        foreach (var block in root.GetProperty("content").EnumerateArray())
        {
            switch (block.GetProperty("type").GetString())
            {
                case "text":
                    blocks.Add(new AnthropicTextBlock(block.GetProperty("text").GetString() ?? string.Empty));
                    break;
                case "tool_use":
                    // Clone detaches the element from this JsonDocument, which is disposed on return.
                    blocks.Add(new AnthropicToolUseBlock(
                        block.GetProperty("id").GetString() ?? string.Empty,
                        block.GetProperty("name").GetString() ?? string.Empty,
                        block.GetProperty("input").Clone()));
                    break;
                default:
                    // Tolerate block types this integration doesn't use (e.g. thinking).
                    break;
            }
        }

        var usage = root.GetProperty("usage");

        return new AnthropicResponse(
            blocks,
            root.GetProperty("stop_reason").GetString() ?? string.Empty,
            new AnthropicUsage(
                usage.GetProperty("input_tokens").GetInt32(),
                usage.GetProperty("output_tokens").GetInt32()));
    }

    // API errors arrive as {"type":"error","error":{"type":"rate_limit_error","message":"..."}}.
    private static AnthropicApiException ToApiException(int statusCode, string payload)
    {
        var errorType = "unknown_error";
        var message = $"Anthropic API returned status {statusCode}.";

        try
        {
            using var document = JsonDocument.Parse(payload);
            if (document.RootElement.TryGetProperty("error", out var error))
            {
                errorType = error.TryGetProperty("type", out var type) ? type.GetString() ?? errorType : errorType;
                message = error.TryGetProperty("message", out var msg) ? msg.GetString() ?? message : message;
            }
        }
        catch (JsonException)
        {
            // Non-JSON error body (e.g. a gateway error page); keep the status-based message.
        }

        return new AnthropicApiException(statusCode, errorType, message);
    }
}
