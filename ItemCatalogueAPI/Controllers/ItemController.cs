using Application.DTOs;
using Application.ServicePorts;
using Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace ItemCatalogueAPI.Controllers;

[ApiController]
[Route("api/items")]
[Produces("application/json")]
public sealed class ItemController(IItemService itemService) : ControllerBase
{
    // GET api/items?page=2&pageSize=50
    // Paginated list. PaginationQuery is bound from the query string; its [Range] guards
    // reject obviously-bad input as 400, and the service clamps to safe bounds server-side.
    [HttpGet(Name = "GetItems")]
    [ProducesResponseType(typeof(PagedResponse<ItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ItemResponse>>> GetAll([FromQuery] PaginationQuery pagination, CancellationToken cancellationToken)
    {
        var page = await itemService.GetAllAsync(pagination, cancellationToken);
        return Ok(page);
    }

    // GET api/items/5
    [HttpGet("{id:int}", Name = "GetItemById")]
    [ProducesResponseType(typeof(ItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var item = await itemService.GetByIdAsync(id, cancellationToken);
        return Ok(item);
    }

    // POST api/items
    [HttpPost]
    [ProducesResponseType(typeof(ItemResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ItemResponse>> Create(CreateItemRequest request, CancellationToken cancellationToken)
    {
        var created = await itemService.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetItemById", new { id = created.Id }, created);
    }

    // PUT api/items/5
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ItemResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ItemResponse>> Update(int id, UpdateItemRequest request, CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            // Surface the route/body id mismatch through the same validation-error channel as
            // FluentValidation failures, so the client gets one consistent 400 problem-details shape.
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.Id), $"Route id {id} does not match body id {request.Id}."),
            ]);
        }

        var updated = await itemService.UpdateAsync(request, cancellationToken);
        return Ok(updated);
    }

    // DELETE api/items/5?reason=Broken
    // Item uses soft delete: the row is flagged with a DeletedReason rather than removed.
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, [FromQuery] DeletedReason reason, CancellationToken cancellationToken)
    {
        await itemService.DeleteAsync(id, reason, cancellationToken);
        return NoContent();
    }

    // GET api/items/5/tags
    // The cross-cutting tags currently assigned to the item.
    [HttpGet("{id:int}/tags", Name = "GetItemTags")]
    [ProducesResponseType(typeof(ItemTagsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemTagsResponse>> GetTags(int id, CancellationToken cancellationToken)
    {
        var tags = await itemService.GetTagsAsync(id, cancellationToken);
        return Ok(tags);
    }

    // PUT api/items/5/tags
    // Replaces the item's full tag set with the supplied tag ids (an empty list clears all tags).
    // 404 if the item or any referenced tag is missing.
    [HttpPut("{id:int}/tags")]
    [ProducesResponseType(typeof(ItemTagsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ItemTagsResponse>> SetTags(int id, SetItemTagsRequest request, CancellationToken cancellationToken)
    {
        var tags = await itemService.SetTagsAsync(id, request, cancellationToken);
        return Ok(tags);
    }
}
