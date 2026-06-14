using Application.DTOs;
using Application.ServicePorts;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace ItemCatalogueAPI.Controllers;

// CRUD for the Tag vocabulary (the cross-cutting labels). Tags are assigned to items through the
// item-tags endpoints on ItemController, not here.
[ApiController]
[Route("api/tags")]
[Produces("application/json")]
public sealed class TagController(ITagService tagService) : ControllerBase
{
    // GET api/tags?page=2&pageSize=50
    [HttpGet(Name = "GetTags")]
    [ProducesResponseType(typeof(PagedResponse<TagResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<TagResponse>>> GetAll([FromQuery] PaginationQuery pagination, CancellationToken cancellationToken)
    {
        var page = await tagService.GetAllAsync(pagination, cancellationToken);
        return Ok(page);
    }

    // GET api/tags/5
    [HttpGet("{id:int}", Name = "GetTagById")]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TagResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var tag = await tagService.GetByIdAsync(id, cancellationToken);
        return Ok(tag);
    }

    // POST api/tags
    // A duplicate Name violates the unique index and surfaces as 409 (translated from the provider's
    // unique-violation error in GenericRepository).
    [HttpPost]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TagResponse>> Create(CreateTagRequest request, CancellationToken cancellationToken)
    {
        var created = await tagService.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetTagById", new { id = created.Id }, created);
    }

    // PUT api/tags/5
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(TagResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TagResponse>> Update(int id, UpdateTagRequest request, CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.Id), $"Route id {id} does not match body id {request.Id}."),
            ]);
        }

        var updated = await tagService.UpdateAsync(request, cancellationToken);
        return Ok(updated);
    }

    // DELETE api/tags/5
    // Hard delete; the FK from ItemTag cascades, so deleting a tag also removes it from every item.
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await tagService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
