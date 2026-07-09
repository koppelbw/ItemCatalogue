namespace Application.DTOs;

// One visible conversation turn. Role is "user" or "assistant". The backend is stateless: the
// client holds the whole conversation and sends it on every request. Only the visible text turns
// are replayed — tool_use/tool_result transcripts from earlier turns are not resent, which keeps
// requests small (the model re-queries via tools when it needs fresh data).
public sealed record ChatTurn(string Role, string Content);

public sealed record ChatRequest(List<ChatTurn> Messages);

// Reply is markdown and may contain habitat://{kind}/{id} deep links the UI turns into
// camera-navigation actions. Token counts are the summed usage across all agent-loop iterations.
public sealed record ChatResponse(
    string Reply,
    int ToolCallCount,
    int InputTokens,
    int OutputTokens);
