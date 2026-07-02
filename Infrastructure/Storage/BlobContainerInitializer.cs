using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Infrastructure.Storage;

public sealed class BlobContainerInitializer(BlobServiceClient serviceClient, IOptions<BlobStorageOptions> options) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken)
        => serviceClient.GetBlobContainerClient(options.Value.ContainerName).CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
