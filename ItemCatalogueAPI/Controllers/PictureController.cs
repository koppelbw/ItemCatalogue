using Application.DTOs;
using Application.ServicePorts;
using Application.Validation;
using Domain.Enums;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Mvc;

namespace ItemCatalogueAPI.Controllers;

[ApiController]
[Produces("application/json")]
public sealed class PictureController(IPictureService pictureService) : ControllerBase
{
    // POST api/locations/5/pictures  (multipart/form-data: file, caption?, isPrimary?)
    [HttpPost("api/locations/{ownerId:int}/pictures", Name = "UploadLocationPicture")]
    [ProducesResponseType(typeof(PictureResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(PictureValidationRules.MaxSizeBytes + 4096)]
    public Task<ActionResult<PictureResponse>> UploadForLocation(
        int ownerId, IFormFile file, [FromForm] string? caption, [FromForm] bool isPrimary, CancellationToken cancellationToken)
        => UploadAsync(PictureOwnerType.Location, ownerId, file, caption, isPrimary, cancellationToken);

    // POST api/rooms/5/pictures
    [HttpPost("api/rooms/{ownerId:int}/pictures", Name = "UploadRoomPicture")]
    [ProducesResponseType(typeof(PictureResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(PictureValidationRules.MaxSizeBytes + 4096)]
    public Task<ActionResult<PictureResponse>> UploadForRoom(
        int ownerId, IFormFile file, [FromForm] string? caption, [FromForm] bool isPrimary, CancellationToken cancellationToken)
        => UploadAsync(PictureOwnerType.Room, ownerId, file, caption, isPrimary, cancellationToken);

    // POST api/containers/5/pictures
    [HttpPost("api/containers/{ownerId:int}/pictures", Name = "UploadContainerPicture")]
    [ProducesResponseType(typeof(PictureResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(PictureValidationRules.MaxSizeBytes + 4096)]
    public Task<ActionResult<PictureResponse>> UploadForContainer(
        int ownerId, IFormFile file, [FromForm] string? caption, [FromForm] bool isPrimary, CancellationToken cancellationToken)
        => UploadAsync(PictureOwnerType.Container, ownerId, file, caption, isPrimary, cancellationToken);

    // POST api/items/5/pictures
    [HttpPost("api/items/{ownerId:int}/pictures", Name = "UploadItemPicture")]
    [ProducesResponseType(typeof(PictureResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [RequestSizeLimit(PictureValidationRules.MaxSizeBytes + 4096)]
    public Task<ActionResult<PictureResponse>> UploadForItem(
        int ownerId, IFormFile file, [FromForm] string? caption, [FromForm] bool isPrimary, CancellationToken cancellationToken)
        => UploadAsync(PictureOwnerType.Item, ownerId, file, caption, isPrimary, cancellationToken);

    // GET api/locations/5/pictures?page=1&pageSize=20
    [HttpGet("api/locations/{ownerId:int}/pictures", Name = "GetLocationPictures")]
    [ProducesResponseType(typeof(PagedResponse<PictureResponse>), StatusCodes.Status200OK)]
    public Task<ActionResult<PagedResponse<PictureResponse>>> GetForLocation(
        int ownerId, [FromQuery] PaginationQuery pagination, CancellationToken cancellationToken)
        => GetForOwnerAsync(PictureOwnerType.Location, ownerId, pagination, cancellationToken);

    // GET api/rooms/5/pictures?page=1&pageSize=20
    [HttpGet("api/rooms/{ownerId:int}/pictures", Name = "GetRoomPictures")]
    [ProducesResponseType(typeof(PagedResponse<PictureResponse>), StatusCodes.Status200OK)]
    public Task<ActionResult<PagedResponse<PictureResponse>>> GetForRoom(
        int ownerId, [FromQuery] PaginationQuery pagination, CancellationToken cancellationToken)
        => GetForOwnerAsync(PictureOwnerType.Room, ownerId, pagination, cancellationToken);

    // GET api/containers/5/pictures?page=1&pageSize=20
    [HttpGet("api/containers/{ownerId:int}/pictures", Name = "GetContainerPictures")]
    [ProducesResponseType(typeof(PagedResponse<PictureResponse>), StatusCodes.Status200OK)]
    public Task<ActionResult<PagedResponse<PictureResponse>>> GetForContainer(
        int ownerId, [FromQuery] PaginationQuery pagination, CancellationToken cancellationToken)
        => GetForOwnerAsync(PictureOwnerType.Container, ownerId, pagination, cancellationToken);

    // GET api/items/5/pictures?page=1&pageSize=20
    [HttpGet("api/items/{ownerId:int}/pictures", Name = "GetItemPictures")]
    [ProducesResponseType(typeof(PagedResponse<PictureResponse>), StatusCodes.Status200OK)]
    public Task<ActionResult<PagedResponse<PictureResponse>>> GetForItem(
        int ownerId, [FromQuery] PaginationQuery pagination, CancellationToken cancellationToken)
        => GetForOwnerAsync(PictureOwnerType.Item, ownerId, pagination, cancellationToken);

    // GET api/pictures/5
    [HttpGet("api/pictures/{id:int}", Name = "GetPictureById")]
    [ProducesResponseType(typeof(PictureResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PictureResponse>> GetById(int id, CancellationToken cancellationToken)
    {
        var picture = await pictureService.GetByIdAsync(id, cancellationToken);
        return Ok(picture);
    }

    // PUT api/pictures/5  (metadata only — caption/primary/sort order; replacing the image bytes
    // is not supported, delete and re-upload instead)
    [HttpPut("api/pictures/{id:int}")]
    [ProducesResponseType(typeof(PictureResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PictureResponse>> Update(int id, UpdatePictureRequest request, CancellationToken cancellationToken)
    {
        if (id != request.Id)
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.Id), $"Route id {id} does not match body id {request.Id}."),
            ]);
        }

        var updated = await pictureService.UpdateAsync(request, cancellationToken);
        return Ok(updated);
    }

    // DELETE api/pictures/5
    [HttpDelete("api/pictures/{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken cancellationToken)
    {
        await pictureService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private async Task<ActionResult<PictureResponse>> UploadAsync(
        PictureOwnerType ownerType, int ownerId, IFormFile file, string? caption, bool isPrimary, CancellationToken cancellationToken)
    {
        if (file is null || file.Length == 0)
        {
            throw new ValidationException([new ValidationFailure("file", "A file is required.")]);
        }

        await using var stream = file.OpenReadStream();

        var request = new UploadPictureRequest(
            ownerType, ownerId, stream, file.ContentType, file.FileName, file.Length, caption, isPrimary);

        var created = await pictureService.UploadAsync(request, cancellationToken);
        return CreatedAtRoute("GetPictureById", new { id = created.Id }, created);
    }

    private async Task<ActionResult<PagedResponse<PictureResponse>>> GetForOwnerAsync(
        PictureOwnerType ownerType, int ownerId, PaginationQuery pagination, CancellationToken cancellationToken)
    {
        var page = await pictureService.GetForOwnerAsync(ownerType, ownerId, pagination, cancellationToken);
        return Ok(page);
    }
}
