using Application.DTOs;
using Application.Logging;
using Application.Mapping;
using Application.ServicePorts;
using Application.StoragePorts;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Domain.Pagination;
using Domain.RepositoryPorts;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;

namespace Application.Services;

public sealed class PictureService(
    IPictureRepository pictureRepository,
    IImageStorage imageStorage,
    IValidator<UploadPictureRequest> uploadValidator,
    IValidator<UpdatePictureRequest> updateValidator,
    ILogger<PictureService> logger) : IPictureService
{
    // AllowedContentTypes in PictureValidationRules restricts uploads to exactly these four
    private static readonly IReadOnlyDictionary<string, string> ContentTypeExtensions = new Dictionary<string, string>
    {
        ["image/jpeg"] = ".jpg",
        ["image/png"] = ".png",
        ["image/webp"] = ".webp",
        ["image/gif"] = ".gif",
    };

    public async Task<PictureResponse> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var picture = await pictureRepository.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Picture", id);

        return picture.ToResponse(imageStorage.GetReadUrl(picture.BlobName));
    }

    public async Task<PagedResponse<PictureResponse>> GetForOwnerAsync(PictureOwnerType ownerType, int ownerId, PaginationQuery pagination, CancellationToken cancellationToken = default)
    {
        var page = await pictureRepository.GetForOwnerAsync(ownerType, ownerId, PageRequest.Create(pagination.Page, pagination.PageSize), cancellationToken);

        return page.ToResponse(p => p.ToResponse(imageStorage.GetReadUrl(p.BlobName)));
    }

    public async Task<PictureResponse> UploadAsync(UploadPictureRequest request, CancellationToken cancellationToken = default)
    {
        await uploadValidator.ValidateAndThrowAsync(request, cancellationToken);

        if (!await MatchesDeclaredContentTypeAsync(request.Content, request.ContentType, cancellationToken))
        {
            throw new ValidationException(
            [
                new ValidationFailure(nameof(request.ContentType), "File content does not match the declared content type."),
            ]);
        }

        var pictureId = Guid.NewGuid();
        var extension = ContentTypeExtensions[request.ContentType];

        var stored = await imageStorage.UploadAsync(
            new NewImage(request.OwnerType, request.OwnerId, pictureId, request.Content, request.ContentType, extension),
            cancellationToken);

        var picture = new Picture
        {
            BlobName = stored.BlobName,
            ContentType = request.ContentType,
            SizeBytes = stored.SizeBytes,
            OriginalFileName = request.FileName,
            Caption = request.Caption,
            IsPrimary = request.IsPrimary,
        };
        picture.SetOwner(request.OwnerType, request.OwnerId);

        await pictureRepository.InsertAsync(picture, cancellationToken);

        if (request.IsPrimary)
        {
            await pictureRepository.ClearPrimaryForOwnerAsync(request.OwnerType, request.OwnerId, picture.Id, cancellationToken);
        }

        logger.EntityCreated("Picture", picture.Id);
        return picture.ToResponse(imageStorage.GetReadUrl(picture.BlobName));
    }

    public async Task<PictureResponse> UpdateAsync(UpdatePictureRequest request, CancellationToken cancellationToken = default)
    {
        await updateValidator.ValidateAndThrowAsync(request, cancellationToken);

        var picture = await pictureRepository.GetForUpdateAsync(request.Id, cancellationToken)
            ?? throw NotFoundException.For("Picture", request.Id);

        request.ApplyTo(picture);
        await pictureRepository.UpdateAsync(picture, cancellationToken);

        if (request.IsPrimary)
        {
            var (ownerType, ownerId) = picture.GetOwner();
            await pictureRepository.ClearPrimaryForOwnerAsync(ownerType, ownerId, picture.Id, cancellationToken);
        }

        logger.EntityUpdated("Picture", picture.Id);
        return picture.ToResponse(imageStorage.GetReadUrl(picture.BlobName));
    }

    public async Task<int> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var picture = await pictureRepository.GetByIdAsync(id, cancellationToken)
            ?? throw NotFoundException.For("Picture", id);

        // Delete the row first, then best-effort delete the blob: a leftover blob is harmless (swept
        // by the demo scheduled-reset's DeleteAllAsync), but a row surviving with a missing blob would
        // serve a broken image URL.
        var rowsAffected = await pictureRepository.DeleteAsync(id, cancellationToken);
        await imageStorage.DeleteAsync(picture.BlobName, cancellationToken);

        logger.EntityDeleted("Picture", id, rowsAffected);
        return rowsAffected;
    }

    // Magic-byte sniff: confirms the uploaded bytes actually look like the declared content type,
    // rather than trusting the client-supplied Content-Type header. Reads a small header then rewinds
    // (ASP.NET Core's IFormFile.OpenReadStream() returns a seekable, buffered stream).
    private static async Task<bool> MatchesDeclaredContentTypeAsync(
        Stream content, string contentType, CancellationToken cancellationToken)
    {
        var header = new byte[12];
        var bytesRead = await content.ReadAsync(header, cancellationToken);
        content.Position = 0;

        return contentType switch
        {
            "image/jpeg" => bytesRead >= 3 && header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF,
            "image/png" => bytesRead >= 4 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47,
            "image/gif" => bytesRead >= 4 && header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x38,
            // RIFF....WEBP: bytes 0-3 "RIFF", bytes 8-11 "WEBP".
            "image/webp" => bytesRead == 12
                && header[0] == 'R' && header[1] == 'I' && header[2] == 'F' && header[3] == 'F'
                && header[8] == 'W' && header[9] == 'E' && header[10] == 'B' && header[11] == 'P',
            _ => false,
        };
    }
}
