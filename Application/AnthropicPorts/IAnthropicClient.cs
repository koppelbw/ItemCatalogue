namespace Application.AnthropicPorts;

// Port for the Anthropic Messages API (implemented by Infrastructure/Anthropic/AnthropicClient.cs),
// isolating Application from HTTP and wire-format concerns. The API is stateless: each call sends
// the full conversation and returns the next assistant message.
public interface IAnthropicClient
{
    Task<AnthropicResponse> CreateMessageAsync(AnthropicRequest request, CancellationToken cancellationToken = default);
}
