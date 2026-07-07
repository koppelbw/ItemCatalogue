using Application.AnthropicPorts;
using Application.DTOs;
using Application.Logging;
using Application.ServicePorts;
using FluentValidation;
using Microsoft.Extensions.Logging;

namespace Application.Services;

// The agent loop: send the conversation + tool catalog to the model; while the model answers with
// stop_reason "tool_use", execute the requested tools and send the results back; stop when it
// produces a final text answer (or the iteration cap trips).
public sealed class ChatService(
    IAnthropicClient anthropicClient,
    ChatToolDispatcher toolDispatcher,
    IValidator<ChatRequest> validator,
    ILogger<ChatService> logger) : IChatService
{
    // Cost guardrail: one user turn may trigger at most this many tool round-trips.
    private const int MaxToolIterations = 10;

    private const string SystemPrompt =
        """
        You are Habitat's home-inventory assistant, embedded in a 3D house viewer. You help the user
        find, add, move, update, and remove items in their home inventory.

        Data model: a Location (building) contains Floors, floors contain Rooms, rooms contain
        Containers (nestable: shelves, boxes, cabinets), and Items live either directly in a room OR
        inside a container - never both.

        Using tools:
        - Call get_house_structure to resolve names like "the garage" or "the red toolbox" to ids
          before searching or changing anything. Never guess an id.
        - Use search_items to find items; use get_item when you need full details or an item's
          location path.
        - After creating, updating, moving, or deleting, confirm what changed in one short sentence.

        Deep links: whenever you mention a specific item, container, room, or location, write it as a
        markdown link using the habitat scheme so the app can fly the camera to it when clicked:
        [the drill](habitat://item/42), [the garage](habitat://room/7),
        [red toolbox](habitat://container/12), [Home](habitat://location/1).
        Use these liberally - they are how the user navigates to what you found.

        Safety:
        - Only delete an item after the user has clearly asked to delete that specific item in this
          conversation. If it is ambiguous, ask first. Deletion needs a reason (Used, Broken,
          Donated, Gifted, or Lost) - infer it from context or ask.
        - If a tool reports an error, explain the problem plainly and suggest what to try instead.

        Style: concise and conversational. Prefer a short sentence or two; use a markdown list only
        when listing several items. If you cannot find something, say so and suggest where to look.
        """;

    public async Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken = default)
    {
        await validator.ValidateAndThrowAsync(request, cancellationToken);

        var messages = request.Messages
            .Select(turn => new AnthropicMessage(turn.Role, [new AnthropicTextBlock(turn.Content)]))
            .ToList();

        var inputTokens = 0;
        var outputTokens = 0;
        var toolCalls = 0;
        var iterations = 0;
        AnthropicResponse response;

        while (true)
        {
            response = await anthropicClient.CreateMessageAsync(
                new AnthropicRequest(SystemPrompt, messages, ChatToolCatalog.All),
                cancellationToken);

            iterations++;
            inputTokens += response.Usage.InputTokens;
            outputTokens += response.Usage.OutputTokens;

            if (response.StopReason != "tool_use")
            {
                break;
            }

            if (iterations > MaxToolIterations)
            {
                logger.ChatIterationCapReached(MaxToolIterations);
                break;
            }

            // Echo the assistant's tool request back, then answer it in a user turn
            messages.Add(new AnthropicMessage("assistant", response.Content));

            var results = new List<AnthropicContentBlock>();
            foreach (var toolUse in response.Content.OfType<AnthropicToolUseBlock>())
            {
                toolCalls++;
                logger.ChatToolInvoked(toolUse.Name);
                results.Add(await toolDispatcher.ExecuteAsync(toolUse, cancellationToken));
            }

            messages.Add(new AnthropicMessage("user", results));
        }

        var reply = string.Join("\n\n", response.Content.OfType<AnthropicTextBlock>().Select(t => t.Text));
        if (string.IsNullOrWhiteSpace(reply))
        {
            reply = "I wasn't able to finish that request - please try rephrasing or breaking it into smaller steps.";
        }

        logger.ChatTurnCompleted(toolCalls, iterations, inputTokens, outputTokens);

        return new ChatResponse(reply, toolCalls, inputTokens, outputTokens);
    }
}
