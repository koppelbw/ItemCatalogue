using Application.DTOs;
using Application.ServicePorts;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace ItemCatalogueAPI.Controllers;

[ApiController]
[Route("api/collections")]
[Produces("application/json")]
public sealed class CollectionController(ICollectionService collectionService) : ControllerBase
{
    // GET api/collections?page=2&pageSize=50
    [HttpGet(Name = "GetCollections")]
    [ProducesResponseType(typeof(PagedResponse<CollectionResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<CollectionResponse>>> GetAll([FromQuery] PaginationQuery pagination, CancellationToken cancellationToken)
    {
        var page = await collectionService.GetAllAsync(pagination, cancellationToken);
        return Ok(page);
    }

    // GET api/collections/5
    [HttpGet("{id:int}", Name = "GetCollectionById")]
    [ProducesResponseType(typeof(CollectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var collection = await collectionService.GetByIdAsync(id, cancellationToken);
        return Ok(collection);
    }

    // POST api/collections
    // A duplicate Name violates the unique index and surfaces as 409 (translated from the provider's
    // unique-violation error in GenericRepository).
    [HttpPost]
    [ProducesResponseType(typeof(CollectionResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CollectionResponse>> Create(CreateCollectionRequest request, CancellationToken cancellationToken)
    {
        var created = await collectionService.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetCollectionById", new { id = created.Id }, created);
    }

    // PUT api/collections/5
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(CollectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<CollectionResponse>> Update(int id, UpdateCollectionRequest request, CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.Id), $"Route id {id} does not match body id {request.Id}."),
            ]);
        }

        var updated = await collectionService.UpdateAsync(request, cancellationToken);
        return Ok(updated);
    }

    // DELETE api/collections/5
    // Hard delete; the FK from CollectionItem cascades, so deleting a collection removes its memberships.
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await collectionService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    // POST api/collections/5/items
    // Adds an item to the collection with its rich-join payload. 404 if the collection or item is
    // missing; 400 if the item is already a member (use the PUT below to change an existing membership).
    [HttpPost("{id:int}/items")]
    [ProducesResponseType(typeof(CollectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionResponse>> AddItem(int id, AddCollectionItemRequest request, CancellationToken cancellationToken)
    {
        var updated = await collectionService.AddItemAsync(id, request, cancellationToken);
        return Ok(updated);
    }

    // PUT api/collections/5/items/8
    // Updates the membership payload (quantity, ordering, role) for one item in the collection.
    [HttpPut("{id:int}/items/{itemId:int}")]
    [ProducesResponseType(typeof(CollectionResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CollectionResponse>> UpdateItem(int id, int itemId, UpdateCollectionItemRequest request, CancellationToken cancellationToken)
    {
        var updated = await collectionService.UpdateItemAsync(id, itemId, request, cancellationToken);
        return Ok(updated);
    }

    // DELETE api/collections/5/items/8
    // Removes one item from the collection (the item itself is untouched).
    [HttpDelete("{id:int}/items/{itemId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveItem(int id, int itemId, CancellationToken cancellationToken)
    {
        await collectionService.RemoveItemAsync(id, itemId, cancellationToken);
        return NoContent();
    }
}
