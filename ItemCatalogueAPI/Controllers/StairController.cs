using Application.DTOs;
using Application.ServicePorts;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace ItemCatalogueAPI.Controllers;

[ApiController]
[Route("api/stairs")]
[Produces("application/json")]
public sealed class StairController(IStairService stairService) : ControllerBase
{
    [HttpGet(Name = "GetStairs")]
    [ProducesResponseType(typeof(PagedResponse<StairResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<StairResponse>>> GetAll([FromQuery] PaginationQuery pagination, CancellationToken cancellationToken)
    {
        var page = await stairService.GetAllAsync(pagination, cancellationToken);
        return Ok(page);
    }

    [HttpGet("{id:int}", Name = "GetStairById")]
    [ProducesResponseType(typeof(StairResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StairResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var stair = await stairService.GetByIdAsync(id, cancellationToken);
        return Ok(stair);
    }

    [HttpPost]
    [ProducesResponseType(typeof(StairResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StairResponse>> Create(CreateStairRequest request, CancellationToken cancellationToken)
    {
        var created = await stairService.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetStairById", new { id = created.Id }, created);
    }

    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(StairResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<StairResponse>> Update(int id, UpdateStairRequest request, CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.Id), $"Route id {id} does not match body id {request.Id}."),
            ]);
        }

        var updated = await stairService.UpdateAsync(request, cancellationToken);
        return Ok(updated);
    }

    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await stairService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
