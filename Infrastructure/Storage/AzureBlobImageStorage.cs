using Application.StoragePorts;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using Microsoft.Extensions.Options;

namespace Infrastructure.Storage;

// Azure Blob Storage adapter for IImageStorage. The container is private (no public access level);
// callers get read access via a short-lived SAS URL (GetReadUrl) instead. GenerateSasUri works
// synchronously here because BlobServiceClient is constructed from a connection string, which
// carries the account's shared key credential.
public sealed class AzureBlobImageStorage(BlobServiceClient serviceClient, IOptions<BlobStorageOptions> options)
    : IImageStorage
{
    private readonly BlobStorageOptions _options = options.Value;
    private BlobContainerClient Container => serviceClient.GetBlobContainerClient(_options.ContainerName);

    public async Task<StoredImage> UploadAsync(NewImage upload, CancellationToken cancellationToken = default)
    {
        var blobName = BuildBlobName(upload);
        var blobClient = Container.GetBlobClient(blobName);

        var uploadInfo = await blobClient.UploadAsync(
            upload.Content,
            new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = upload.ContentType } },
            cancellationToken);

        var properties = await blobClient.GetPropertiesAsync(cancellationToken: cancellationToken);
        return new StoredImage(blobName, properties.Value.ContentLength);
    }

    public Uri GetReadUrl(string blobName, TimeSpan? ttl = null)
    {
        var blobClient = Container.GetBlobClient(blobName);
        var expiresOn = DateTimeOffset.UtcNow.Add(ttl ?? TimeSpan.FromMinutes(_options.ReadSasMinutes));
        return blobClient.GenerateSasUri(BlobSasPermissions.Read, expiresOn);
    }

    public Task DeleteAsync(string blobName, CancellationToken cancellationToken = default)
        => Container.DeleteBlobIfExistsAsync(blobName, cancellationToken: cancellationToken);

    public async Task DeleteAllAsync(CancellationToken cancellationToken = default)
    {
        await foreach (var blob in Container.GetBlobsAsync(cancellationToken: cancellationToken))
        {
            await Container.DeleteBlobIfExistsAsync(blob.Name, cancellationToken: cancellationToken);
        }
    }

    // {ownerType}/{ownerId}/{pictureId}{ext}, e.g. "item/42/3f9c2b1a....jpg". Human-browsable in the
    // storage explorer and naturally partitions blobs by owner without a separate index.
    private static string BuildBlobName(NewImage upload)
        => $"{upload.OwnerType.ToString().ToLowerInvariant()}/{upload.OwnerId}/{upload.PictureId:N}{upload.FileExtension}";
}
