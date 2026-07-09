using Application.DTOs;
using Application.ServicePorts;
using Microsoft.Azure.Functions.Worker;

namespace ItemCatalogueFunctions;

// One Storage Queue message = one chunk of 25 (ImportOptions.ChunkSize) items.

// Deserialization is the worker's.
// Retries/poison handling are the host's (host.json queues.maxDequeueCount).
// ALL import logic lives in the shared Application core, which is exactly what makes Durable/Service Bus triggers drop-in replacements later.

// The queue name matches ImportStorageOptions.QueueName; messages are Base64 JSON (the host's
// default decoding), which StorageQueueImportDispatcher emits.
public sealed class ImportChunkFunction(IImportJobService importJobService)
{
    [Function(nameof(ImportChunkFunction))]
    public Task Run(
        [QueueTrigger("item-import", Connection = "ImportStorage:ConnectionString")] ImportChunkMessage message,
        CancellationToken cancellationToken)
        => importJobService.ProcessChunkAsync(message, cancellationToken);
}
