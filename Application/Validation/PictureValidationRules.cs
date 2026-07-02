namespace Application.Validation;

// Upload policy for pictures: which content types are accepted and the max upload size. This is a
// business rule owned by Application, independent of Infrastructure/Storage/BlobStorageOptions
// (which only configures how to talk to the storage account). Public (unlike the other *Rules
// classes) because ItemCatalogueAPI needs MaxSizeBytes as a compile-time constant for [RequestSizeLimit].
public static class PictureValidationRules
{
    public const long MaxSizeBytes = 10 * 1024 * 1024;

    public static readonly string[] AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif",
    ];
}
