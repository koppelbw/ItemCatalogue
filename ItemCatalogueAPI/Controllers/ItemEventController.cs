using Application.DTOs;
using Application.ServicePorts;
using Microsoft.AspNetCore.Mvc;

namespace ItemCatalogueAPI.Controllers;

[ApiController]
[Route("api/items/{itemId:int}/events")]
[Produces("application/json")]
public sealed class ItemEventController(IItemEventService itemEventService) : ControllerBase
{
    // GET api/items/5/events
    // Returns all recorded events for the given item, newest first.
    // Returns an empty list (not 404) when no events exist; call GET api/items/{id} to verify item existence.
    [HttpGet(Name = "GetItemEvents")]
    [ProducesResponseType(typeof(IReadOnlyList<ItemEventResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ItemEventResponse>>> GetByItemId(int itemId, CancellationToken cancellationToken)
    {
        var events = await itemEventService.GetByItemIdAsync(itemId, cancellationToken);
        return Ok(events);
    }
}
