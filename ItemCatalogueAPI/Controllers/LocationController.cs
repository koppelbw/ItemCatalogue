using Application.DTOs;
using Application.ServicePorts;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace ItemCatalogueAPI.Controllers;

[ApiController]
[Route("api/locations")]
[Produces("application/json")]
public sealed class LocationController(ILocationService locationService) : ControllerBase
{
    // GET api/locations?page=2&pageSize=50
    // Paginated list. PaginationQuery is bound from the query string; its [Range] guards
    // reject obviously-bad input as 400, and the service clamps to safe bounds server-side.
    [HttpGet(Name = "GetLocations")]
    [ProducesResponseType(typeof(PagedResponse<LocationResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<LocationResponse>>> GetAll([FromQuery] PaginationQuery pagination, CancellationToken cancellationToken)
    {
        var page = await locationService.GetAllAsync(pagination, cancellationToken);
        return Ok(page);
    }

    // GET api/locations/5
    [HttpGet("{id:int}", Name = "GetLocationById")]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocationResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var location = await locationService.GetByIdAsync(id, cancellationToken);
        return Ok(location);
    }

    // GET api/locations/5/map
    // Returns the full spatial graph for the location (floors -> rooms with geometry/colours ->
    // nested container tree + doors) so a consumer can reconstruct the whole building in one call.
    [HttpGet("{id:int}/map", Name = "GetLocationMap")]
    [ProducesResponseType(typeof(LocationMapResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LocationMapResponse>> GetMap(int id, CancellationToken cancellationToken)
    {
        var map = await locationService.GetMapAsync(id, cancellationToken);
        return Ok(map);
    }

    // POST api/locations
    [HttpPost]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<LocationResponse>> Create(CreateLocationRequest request, CancellationToken cancellationToken)
    {
        var created = await locationService.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetLocationById", new { id = created.Id }, created);
    }

    // PUT api/locations/5
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(LocationResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<LocationResponse>> Update(int id, UpdateLocationRequest request, CancellationToken cancellationToken)
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

        var updated = await locationService.UpdateAsync(request, cancellationToken);
        return Ok(updated);
    }

    // DELETE api/locations/5
    // Location uses hard delete. A restricted foreign key (e.g. Items still referencing the
    // location) surfaces as EntityInUseException, mapped to 409.
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await locationService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
