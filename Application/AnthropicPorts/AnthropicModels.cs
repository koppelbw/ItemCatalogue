using System.Text.Json;

namespace Application.AnthropicPorts;

// In-process model of the Anthropic Messages API wire format (POST /v1/messages), owned by
// Application so ChatService can drive the agentic tool-use loop without referencing the
// Infrastructure adapter. Serialization to the snake_case wire JSON happens in
// Infrastructure/Anthropic/AnthropicClient.cs.


public sealed record AnthropicMessage(string Role, IReadOnlyList<AnthropicContentBlock> Content);

// The API represents message content as an array of typed blocks, not a plain string.
public abstract record AnthropicContentBlock;

public sealed record AnthropicTextBlock(string Text) : AnthropicContentBlock;

public sealed record AnthropicToolUseBlock(string Id, string Name, JsonElement Input) : AnthropicContentBlock;

public sealed record AnthropicToolResultBlock(string ToolUseId, string Content, bool IsError = false) : AnthropicContentBlock;

public sealed record AnthropicTool(string Name, string Description, JsonElement InputSchema);

// One /v1/messages call. Model and max_tokens are supplied by the Infrastructure adapter from
// AnthropicOptions, so Application stays free of deployment configuration.
public sealed record AnthropicRequest(
    string System,
    IReadOnlyList<AnthropicMessage> Messages,
    IReadOnlyList<AnthropicTool> Tools);

// StopReason is the loop-control signal: "end_turn" = done, "tool_use" = execute the ToolUseBlocks
// in Content and call again, "max_tokens" = truncated.
public sealed record AnthropicResponse(
    IReadOnlyList<AnthropicContentBlock> Content,
    string StopReason,
    AnthropicUsage Usage);

public sealed record AnthropicUsage(int InputTokens, int OutputTokens);
