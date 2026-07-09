using Application.DTOs;
using Application.ServicePorts;
using Microsoft.Azure.Functions.Worker;

namespace ItemCatalogueFunctions;

// After maxDequeueCount failed attempts the host moves a chunk message (body unchanged) to the
// poison queue. Recording those rows as failed keeps the job converging to Completed — without
// this, one permanently-bad chunk would strand its job in Processing forever.
public sealed class ImportPoisonFunction(IImportJobService importJobService)
{
    [Function(nameof(ImportPoisonFunction))]
    public Task Run(
        [QueueTrigger("item-import-poison", Connection = "ImportStorage:ConnectionString")] ImportChunkMessage message,
        CancellationToken cancellationToken)
        => importJobService.MarkChunkFailedAsync(
            message,
            "The chunk could not be processed after repeated retries.",
            cancellationToken);
}
