using System.Text.Json;
using Application.AnthropicPorts;
using Application.DTOs;
using Application.Services;
using Application.ServicePorts;
using Application.Validation;
using FluentValidation;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Shouldly;

namespace Application.Tests.Services;

public class ChatServiceTests
{
    private readonly IAnthropicClient _anthropic = Substitute.For<IAnthropicClient>();
    private readonly ILocationService _locationService = Substitute.For<ILocationService>();
    private readonly IItemService _itemService = Substitute.For<IItemService>();
    private readonly List<AnthropicRequest> _sentRequests = [];
    private readonly ChatService _service;

    public ChatServiceTests()
    {
        var dispatcher = new ChatToolDispatcher(_locationService, _itemService, NullLogger<ChatToolDispatcher>.Instance);
        _service = new ChatService(_anthropic, dispatcher, new ChatRequestValidator(), NullLogger<ChatService>.Instance);
    }

    private static ChatRequest UserAsks(string content) => new([new ChatTurn("user", content)]);

    private static AnthropicResponse TextAnswer(string text, int inputTokens = 100, int outputTokens = 20) =>
        new([new AnthropicTextBlock(text)], "end_turn", new AnthropicUsage(inputTokens, outputTokens));

    private static AnthropicResponse ToolCall(string toolName, string inputJson, string id = "toolu_1")
    {
        using var document = JsonDocument.Parse(inputJson);
        return new AnthropicResponse(
            [new AnthropicToolUseBlock(id, toolName, document.RootElement.Clone())],
            "tool_use",
            new AnthropicUsage(100, 30));
    }

    // Queue up successive model responses and capture every request the service sends.
    private void ModelReturns(params AnthropicResponse[] responses)
    {
        var call = 0;
        _anthropic
            .CreateMessageAsync(Arg.Do<AnthropicRequest>(_sentRequests.Add), Arg.Any<CancellationToken>())
            .Returns(_ => responses[Math.Min(call++, responses.Length - 1)]);
    }

    [Fact]
    public async Task SendAsync_WhenModelAnswersDirectly_ReturnsReplyWithUsage()
    {
        ModelReturns(TextAnswer("You have 3 items in the garage.", inputTokens: 250, outputTokens: 40));

        var response = await _service.SendAsync(UserAsks("What's in the garage?"));

        response.Reply.ShouldBe("You have 3 items in the garage.");
        response.ToolCallCount.ShouldBe(0);
        response.InputTokens.ShouldBe(250);
        response.OutputTokens.ShouldBe(40);
        _sentRequests.Count.ShouldBe(1);
    }

    [Fact]
    public async Task SendAsync_SendsSystemPromptToolsAndConversation()
    {
        ModelReturns(TextAnswer("Hi!"));

        await _service.SendAsync(new ChatRequest(
        [
            new ChatTurn("user", "hello"),
            new ChatTurn("assistant", "hi, how can I help?"),
            new ChatTurn("user", "where is the drill?"),
        ]));

        var request = _sentRequests.Single();
        request.System.ShouldContain("habitat://item");
        request.Tools.Select(t => t.Name).ShouldBe(
            ["get_house_structure", "search_items", "get_item", "create_item", "update_item", "delete_item"]);
        request.Messages.Count.ShouldBe(3);
        request.Messages[2].Content.OfType<AnthropicTextBlock>().Single().Text.ShouldBe("where is the drill?");
    }

    [Fact]
    public async Task SendAsync_WhenModelRequestsTool_ExecutesItAndLoops()
    {
        _itemService.GetAllAsync(Arg.Any<ItemSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResponse<ItemResponse>([ChatToolDispatcherTests.Item(42, "Drill")], 1, 1, 25, 1, false, false));

        ModelReturns(
            ToolCall("search_items", """{"query":"drill"}"""),
            TextAnswer("Found it: [Drill](habitat://item/42)"));

        var response = await _service.SendAsync(UserAsks("Where's the drill?"));

        response.Reply.ShouldBe("Found it: [Drill](habitat://item/42)");
        response.ToolCallCount.ShouldBe(1);
        _sentRequests.Count.ShouldBe(2);

        // The follow-up request must replay the assistant's tool_use turn and answer it with a
        // matching tool_result in a user turn - the wire shape the Messages API requires.
        var followUp = _sentRequests[1];
        followUp.Messages[^2].Role.ShouldBe("assistant");
        followUp.Messages[^2].Content.OfType<AnthropicToolUseBlock>().Single().Name.ShouldBe("search_items");
        followUp.Messages[^1].Role.ShouldBe("user");
        var result = followUp.Messages[^1].Content.OfType<AnthropicToolResultBlock>().Single();
        result.ToolUseId.ShouldBe("toolu_1");
        result.IsError.ShouldBeFalse();
        result.Content.ShouldContain("Drill");
    }

    [Fact]
    public async Task SendAsync_WhenToolTargetIsMissing_ReturnsErrorResultToModel()
    {
        _itemService.GetByIdAsync(99, Arg.Any<CancellationToken>())
            .Returns<ItemResponse>(_ => throw Domain.Exceptions.NotFoundException.For("Item", 99));

        ModelReturns(
            ToolCall("get_item", """{"id":99}"""),
            TextAnswer("That item doesn't exist."));

        var response = await _service.SendAsync(UserAsks("Show me item 99"));

        response.Reply.ShouldBe("That item doesn't exist.");
        var result = _sentRequests[1].Messages[^1].Content.OfType<AnthropicToolResultBlock>().Single();
        result.IsError.ShouldBeTrue();
        result.Content.ShouldContain("99");
    }

    [Fact]
    public async Task SendAsync_WhenModelNeverFinishes_StopsAtIterationCap()
    {
        _itemService.GetAllAsync(Arg.Any<ItemSearchQuery>(), Arg.Any<CancellationToken>())
            .Returns(new PagedResponse<ItemResponse>([], 0, 1, 25, 0, false, false));

        // The model asks for a tool on every response and never produces a final answer.
        ModelReturns(ToolCall("search_items", """{"query":"loop"}"""));

        var response = await _service.SendAsync(UserAsks("loop forever"));

        // 1 initial call + MaxToolIterations (10) loop iterations = 11 model calls, then bail out.
        _sentRequests.Count.ShouldBe(11);
        response.Reply.ShouldNotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task SendAsync_WithNoMessages_ThrowsValidation()
    {
        await Should.ThrowAsync<ValidationException>(() => _service.SendAsync(new ChatRequest([])));
        await _anthropic.DidNotReceive().CreateMessageAsync(Arg.Any<AnthropicRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SendAsync_WhenLastTurnIsAssistant_ThrowsValidation()
    {
        var request = new ChatRequest([new ChatTurn("user", "hi"), new ChatTurn("assistant", "hello")]);

        await Should.ThrowAsync<ValidationException>(() => _service.SendAsync(request));
    }
}
