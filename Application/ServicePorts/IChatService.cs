using Application.DTOs;

namespace Application.ServicePorts;

public interface IChatService
{
    // Runs the full agentic loop for one user turn: calls the model, executes any tool requests
    // against the inventory services, and repeats until the model produces a final text answer.
    Task<ChatResponse> SendAsync(ChatRequest request, CancellationToken cancellationToken = default);
}
