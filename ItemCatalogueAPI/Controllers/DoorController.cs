using Application.DTOs;
using Application.ServicePorts;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace ItemCatalogueAPI.Controllers;

[ApiController]
[Route("api/doors")]
[Produces("application/json")]
public sealed class DoorController(IDoorService doorService) : ControllerBase
{
    // GET api/doors?page=2&pageSize=50
    [HttpGet(Name = "GetDoors")]
    [ProducesResponseType(typeof(PagedResponse<DoorResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<DoorResponse>>> GetAll([FromQuery] PaginationQuery pagination, CancellationToken cancellationToken)
    {
        var page = await doorService.GetAllAsync(pagination, cancellationToken);
        return Ok(page);
    }

    // GET api/doors/5
    [HttpGet("{id:int}", Name = "GetDoorById")]
    [ProducesResponseType(typeof(DoorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DoorResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var door = await doorService.GetByIdAsync(id, cancellationToken);
        return Ok(door);
    }

    // POST api/doors
    [HttpPost]
    [ProducesResponseType(typeof(DoorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<DoorResponse>> Create(CreateDoorRequest request, CancellationToken cancellationToken)
    {
        var created = await doorService.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetDoorById", new { id = created.Id }, created);
    }

    // PUT api/doors/5
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(DoorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<DoorResponse>> Update(int id, UpdateDoorRequest request, CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.Id), $"Route id {id} does not match body id {request.Id}."),
            ]);
        }

        var updated = await doorService.UpdateAsync(request, cancellationToken);
        return Ok(updated);
    }

    // DELETE api/doors/5
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await doorService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
