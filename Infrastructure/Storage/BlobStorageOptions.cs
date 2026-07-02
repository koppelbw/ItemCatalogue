namespace Infrastructure.Storage;

public sealed class BlobStorageOptions
{
    public const string SectionName = "BlobStorage";

    public string ConnectionString { get; set; } = string.Empty;

    public string ContainerName { get; set; } = "catalogue-images";

    public int ReadSasMinutes { get; set; } = 15;
}
