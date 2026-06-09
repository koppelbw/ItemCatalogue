using Application.DTOs;
using Application.ServicePorts;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace ItemCatalogueAPI.Controllers;

[ApiController]
[Route("api/rooms")]
[Produces("application/json")]
public sealed class RoomController(IRoomService roomService) : ControllerBase
{
    // GET api/rooms?page=2&pageSize=50
    // Paginated list. PaginationQuery is bound from the query string; its [Range] guards
    // reject obviously-bad input as 400, and the service clamps to safe bounds server-side.
    [HttpGet(Name = "GetRooms")]
    [ProducesResponseType(typeof(PagedResponse<RoomResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<RoomResponse>>> GetAll([FromQuery] PaginationQuery pagination, CancellationToken cancellationToken)
    {
        var page = await roomService.GetAllAsync(pagination, cancellationToken);
        return Ok(page);
    }

    // GET api/rooms/5
    [HttpGet("{id:int}", Name = "GetRoomById")]
    [ProducesResponseType(typeof(RoomResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoomResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var room = await roomService.GetByIdAsync(id, cancellationToken);
        return Ok(room);
    }

    // POST api/rooms
    [HttpPost]
    [ProducesResponseType(typeof(RoomResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RoomResponse>> Create(CreateRoomRequest request, CancellationToken cancellationToken)
    {
        var created = await roomService.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetRoomById", new { id = created.Id }, created);
    }

    // PUT api/rooms/5
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(RoomResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoomResponse>> Update(int id, UpdateRoomRequest request, CancellationToken cancellationToken)
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

        var updated = await roomService.UpdateAsync(request, cancellationToken);
        return Ok(updated);
    }

    // DELETE api/rooms/5
    // Room uses hard delete. A restricted foreign key (e.g. Locations still referencing the
    // room) surfaces as EntityInUseException, mapped to 409.
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await roomService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
