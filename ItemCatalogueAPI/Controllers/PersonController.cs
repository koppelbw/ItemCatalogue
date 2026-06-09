using Application.DTOs;
using Application.ServicePorts;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace ItemCatalogueAPI.Controllers;

[ApiController]
[Route("api/persons")]
[Produces("application/json")]
public sealed class PersonController(IPersonService personService) : ControllerBase
{
    // GET api/persons?page=2&pageSize=50
    // Paginated list. PaginationQuery is bound from the query string; its [Range] guards
    // reject obviously-bad input as 400, and the service clamps to safe bounds server-side.
    [HttpGet(Name = "GetPersons")]
    [ProducesResponseType(typeof(PagedResponse<PersonResponse>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<PersonResponse>>> GetAll([FromQuery] PaginationQuery pagination, CancellationToken cancellationToken)
    {
        var page = await personService.GetAllAsync(pagination, cancellationToken);
        return Ok(page);
    }

    // GET api/persons/5
    [HttpGet("{id:int}", Name = "GetPersonById")]
    [ProducesResponseType(typeof(PersonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PersonResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var person = await personService.GetByIdAsync(id, cancellationToken);
        return Ok(person);
    }

    // POST api/persons
    [HttpPost]
    [ProducesResponseType(typeof(PersonResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PersonResponse>> Create(CreatePersonRequest request, CancellationToken cancellationToken)
    {
        var created = await personService.CreateAsync(request, cancellationToken);
        return CreatedAtRoute("GetPersonById", new { id = created.Id }, created);
    }

    // PUT api/persons/5
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(PersonResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PersonResponse>> Update(int id, UpdatePersonRequest request, CancellationToken cancellationToken)
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

        var updated = await personService.UpdateAsync(request, cancellationToken);
        return Ok(updated);
    }

    // DELETE api/persons/5
    // Person uses hard delete. A restricted foreign key (e.g. Items still referencing the
    // person) surfaces as EntityInUseException, mapped to 409.
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await personService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
