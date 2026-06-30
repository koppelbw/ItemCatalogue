using Application.DTOs;
using Application.ServicePorts;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace ItemCatalogueAPI.Controllers;

[ApiController]
[Route("api/containers")]
[Produces("application/json")]
public sealed class ContainerController(IContainerService containerService, IItemService itemService) : ControllerBase
{
    // GET api/containers/5/items?page=1&pageSize=20
    [HttpGet("{id:int}/items", Name = "GetContainerItems")]
    [ProducesResponseType(typeof(PagedResponse<ItemResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ItemResponse>>> GetItems(int id, [FromQuery] PaginationQuery pagination, CancellationToken cancellationToken)
    {
        var page = await itemService.GetItemsByContainerAsync(id, pagination, cancellationToken);
        return Ok(page);
    }

    // GET api/containers?page=2&pageSize=50
    // Paginated list. PaginationQuery is bound from the query string; its [Range] guards
    // reject obviously-bad input as 400, and the service clamps to safe bounds server-side.
    [HttpGet(Name = "GetContainers")]
    [ProducesResponseType(typeof(PagedResponse<ContainerResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<ContainerResponse>>> GetAll([FromQuery] PaginationQuery pagination, CancellationToken cancellationToken)
    {
        var page = await containerService.GetAllAsync(pagination, cancellationToken);
        return Ok(page);
    }

    // GET api/containers/5
    [HttpGet("{id:int}", Name = "GetContainerById")]
    [ProducesResponseType(typeof(ContainerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ContainerResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var container = await containerService.GetByIdAsync(id, cancellationToken);
        return Ok(container);
    }

    // POST api/containers
    // Set exactly one of RoomId (top-level) or ParentContainerId (nested); the validator rejects
    // a request that sets both or neither with a 400.
    [HttpPost]
    [ProducesResponseType(typeof(ContainerResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ContainerResponse>> Create(CreateContainerRequest request, CancellationToken cancellationToken)
    {
        var created = await containerService.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetContainerById", new { id = created.Id }, created);
    }

    // PUT api/containers/5
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ContainerResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ContainerResponse>> Update(int id, UpdateContainerRequest request, CancellationToken cancellationToken)
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

        var updated = await containerService.UpdateAsync(request, cancellationToken);
        return Ok(updated);
    }

    // DELETE api/containers/5
    // Container uses hard delete. A restricted foreign key (child containers still referencing this
    // one) surfaces as EntityInUseException, mapped to 409.
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await containerService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
