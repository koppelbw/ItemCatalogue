namespace Domain.Enums;

// Which of Picture's four nullable owner FKs is set. Also drives the blob storage key layout
// (see Infrastructure/Storage/AzureBlobImageStorage.cs) and the nested picture routes.
public enum PictureOwnerType
{
    Location,
    Room,
    Container,
    Item,
}
