using Microsoft.Extensions.Logging;

namespace Application.Logging;

// Source-generated log methods for the chat agent, matching the ServiceLog conventions. Token
// counts are logged because they are the direct cost driver for this feature.
internal static partial class ChatLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Chat tool {ToolName} invoked")]
    public static partial void ChatToolInvoked(this ILogger logger, string toolName);


    [LoggerMessage(Level = LogLevel.Warning, Message = "Chat tool {ToolName} failed: {Reason}")]
    public static partial void ChatToolFailed(this ILogger logger, string toolName, string reason);


    [LoggerMessage(Level = LogLevel.Information, Message = "Chat turn completed: {ToolCalls} tool call(s), {Iterations} model call(s), {InputTokens} input / {OutputTokens} output tokens")]
    public static partial void ChatTurnCompleted(this ILogger logger, int toolCalls, int iterations, int inputTokens, int outputTokens);


    [LoggerMessage(Level = LogLevel.Warning, Message = "Chat turn hit the tool-iteration cap ({MaxIterations}) before the model finished")]
    public static partial void ChatIterationCapReached(this ILogger logger, int maxIterations);
}
