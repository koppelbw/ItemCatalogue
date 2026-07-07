namespace Infrastructure.Storage;

// Storage settings for the bulk-import pipeline: the work/poison queues plus the claim-check
// payload container, all on one connection. Shares the storage account with BlobStorageOptions
// (same Azurite emulator locally, same account in Azure) but is bound separately so import
// traffic could move to its own account without touching image storage. Retry/visibility knobs
// live on the consumer side (the Functions host's host.json), not here.
public sealed class ImportStorageOptions
{
    public const string SectionName = "ImportStorage";

    public string ConnectionString { get; set; } = string.Empty;

    public string QueueName { get; set; } = "item-import";

    // After the host's maxDequeueCount is exhausted the runtime moves the message here; the poison
    // function records the chunk as failed so the job still reaches a terminal state.
    public string PoisonQueueName { get; set; } = "item-import-poison";

    // Blob container holding each job's normalized payload (import/{jobId}/payload.json).
    public string PayloadContainerName { get; set; } = "catalogue-imports";
}
