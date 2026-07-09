using Application.DTOs;
using Application.StoragePorts;
using Azure.Storage.Queues;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Infrastructure.Storage;

// Transport adapter: one Storage Queue message per chunk. The injected
// QueueServiceClient is configured with Base64 message encoding (see AddInfrastructure) to match
// what the Functions queue trigger expects — a mismatch there is the classic silent footgun.
// Durable Functions / Service Bus variants would be sibling IImportDispatcher implementations.
public sealed class StorageQueueImportDispatcher(QueueServiceClient queueServiceClient, IOptions<ImportStorageOptions> options) : IImportDispatcher
{
    public async Task DispatchAsync(int jobId, IReadOnlyList<ChunkRef> chunks, CancellationToken cancellationToken = default)
    {
        var queue = queueServiceClient.GetQueueClient(options.Value.QueueName);

        foreach (var chunk in chunks)
        {
            var message = new ImportChunkMessage(jobId, chunk.ChunkIndex, chunk.StartRow, chunk.Count);
            await queue.SendMessageAsync(JsonSerializer.Serialize(message), cancellationToken);
        }
    }
}
