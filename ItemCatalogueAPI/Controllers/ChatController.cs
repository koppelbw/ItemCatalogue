using Application.DTOs;
using Application.ServicePorts;
using Microsoft.AspNetCore.Mvc;

namespace ItemCatalogueAPI.Controllers;

// The AI assistant endpoint. Stateless: the client sends the whole visible conversation each time
// and ChatService runs the Anthropic tool-use loop for the latest user turn. Responses can take
// tens of seconds when the model chains several tool calls.
[ApiController]
[Route("api/chat")]
[Produces("application/json")]
public sealed class ChatController(IChatService chatService) : ControllerBase
{
    // POST api/chat
    [HttpPost]
    [ProducesResponseType(typeof(ChatResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatResponse>> Send(ChatRequest request, CancellationToken cancellationToken)
    {
        var response = await chatService.SendAsync(request, cancellationToken);
        return Ok(response);
    }
}
