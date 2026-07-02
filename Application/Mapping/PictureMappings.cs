using Application.DTOs;
using Domain.Entities;
using Domain.Enums;

namespace Application.Mapping;

public static class PictureMappings
{
    // Reads whichever of the four owner FKs is set. Exactly one is guaranteed by
    // CK_Picture_ExactlyOneOwner (see Database/dbo/tables/Picture.sql).
    public static (PictureOwnerType OwnerType, int OwnerId) GetOwner(this Picture picture) => picture switch
    {
        { LocationId: { } id } => (PictureOwnerType.Location, id),
        { RoomId: { } id } => (PictureOwnerType.Room, id),
        { ContainerId: { } id } => (PictureOwnerType.Container, id),
        { ItemId: { } id } => (PictureOwnerType.Item, id),
        _ => throw new InvalidOperationException($"Picture {picture.Id} has no owner set."),
    };

    public static void SetOwner(this Picture picture, PictureOwnerType ownerType, int ownerId)
    {
        switch (ownerType)
        {
            case PictureOwnerType.Location: picture.LocationId = ownerId; break;
            case PictureOwnerType.Room: picture.RoomId = ownerId; break;
            case PictureOwnerType.Container: picture.ContainerId = ownerId; break;
            case PictureOwnerType.Item: picture.ItemId = ownerId; break;
            default: throw new ArgumentOutOfRangeException(nameof(ownerType), ownerType, null);
        }
    }

    public static void ApplyTo(this UpdatePictureRequest request, Picture picture)
    {
        picture.Caption = request.Caption;
        picture.IsPrimary = request.IsPrimary;
        picture.SortOrder = request.SortOrder;
        picture.RowVersion = request.RowVersion;
    }

    // url is injected by the caller (a freshly-minted SAS read link) rather than stored on the
    // entity — see Application/StoragePorts/IImageStorage.cs.
    public static PictureResponse ToResponse(this Picture picture, Uri url)
    {
        var (ownerType, ownerId) = picture.GetOwner();

        return new PictureResponse(
            picture.Id,
            ownerType,
            ownerId,
            url,
            picture.ContentType,
            picture.SizeBytes,
            picture.OriginalFileName,
            picture.Caption,
            picture.IsPrimary,
            picture.SortOrder,
            picture.WidthPixels,
            picture.HeightPixels,
            picture.CreatedDate,
            picture.RowVersion);
    }
}
