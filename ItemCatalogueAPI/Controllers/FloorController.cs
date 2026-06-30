using Application.DTOs;
using Application.ServicePorts;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace ItemCatalogueAPI.Controllers;

[ApiController]
[Route("api/floors")]
[Produces("application/json")]
public sealed class FloorController(IFloorService floorService) : ControllerBase
{
    // GET api/floors?page=2&pageSize=50
    [HttpGet(Name = "GetFloors")]
    [ProducesResponseType(typeof(PagedResponse<FloorResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<FloorResponse>>> GetAll([FromQuery] PaginationQuery pagination, CancellationToken cancellationToken)
    {
        var page = await floorService.GetAllAsync(pagination, cancellationToken);
        return Ok(page);
    }

    // GET api/floors/5
    [HttpGet("{id:int}", Name = "GetFloorById")]
    [ProducesResponseType(typeof(FloorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<FloorResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var floor = await floorService.GetByIdAsync(id, cancellationToken);
        return Ok(floor);
    }

    // POST api/floors
    [HttpPost]
    [ProducesResponseType(typeof(FloorResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FloorResponse>> Create(CreateFloorRequest request, CancellationToken cancellationToken)
    {
        var created = await floorService.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetFloorById", new { id = created.Id }, created);
    }

    // PUT api/floors/5
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(FloorResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<FloorResponse>> Update(int id, UpdateFloorRequest request, CancellationToken cancellationToken)
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

        var updated = await floorService.UpdateAsync(request, cancellationToken);
        return Ok(updated);
    }

    // DELETE api/floors/5
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await floorService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
