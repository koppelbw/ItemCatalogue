using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Queues;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Infrastructure.Storage;

// Ensures the import work/poison queues and the payload container exist before the host serves
// requests, so the dispatcher and payload store never have to check per call. Runs once at
// startup in BOTH hosts (API and Functions) — CreateIfNotExists makes that race-free.
// Mirrors BlobContainerInitializer.
public sealed class ImportStorageInitializer(QueueServiceClient queueServiceClient, IOptions<ImportStorageOptions> options) : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await queueServiceClient.GetQueueClient(options.Value.QueueName)
            .CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        await queueServiceClient.GetQueueClient(options.Value.PoisonQueueName)
            .CreateIfNotExistsAsync(cancellationToken: cancellationToken);
        await new BlobContainerClient(options.Value.ConnectionString, options.Value.PayloadContainerName)
            .CreateIfNotExistsAsync(PublicAccessType.None, cancellationToken: cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
