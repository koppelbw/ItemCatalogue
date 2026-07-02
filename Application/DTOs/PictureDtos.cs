using Domain.Enums;

namespace Application.DTOs;

// Upload input assembled by the controller from the incoming multipart request. Kept
// framework-agnostic (no IFormFile) so Application has no ASP.NET Core dependency.
public sealed record UploadPictureRequest(
    PictureOwnerType OwnerType,
    int OwnerId,
    Stream Content,
    string ContentType,
    string? FileName,
    long SizeBytes,
    string? Caption,
    bool IsPrimary);

// Metadata-only update. Replacing the image bytes is not supported in v1 — delete and re-upload.
public sealed record UpdatePictureRequest(
    int Id,
    string? Caption,
    bool IsPrimary,
    int SortOrder,
    byte[] RowVersion);

public sealed record PictureResponse(
    int Id,
    PictureOwnerType OwnerType,
    int OwnerId,
    Uri Url,
    string ContentType,
    long SizeBytes,
    string? OriginalFileName,
    string? Caption,
    bool IsPrimary,
    int SortOrder,
    int? WidthPixels,
    int? HeightPixels,
    DateTime CreatedDate,
    byte[] RowVersion);
