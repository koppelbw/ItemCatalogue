using Application.DTOs;

namespace Application.StoragePorts;

// Transport port for handing import chunks to a background processor — the ONE seam that differs
// between transports (Storage Queue today; Durable Functions / Service Bus are alternative
// adapters). Everything upstream (parsing, name resolution, job creation) and downstream
// (ProcessChunkAsync) is transport-agnostic; nothing transport-specific may leak through here.
public interface IImportDispatcher
{
    // Enqueues one message per chunk. The payload must already be persisted via
    // IImportPayloadStore before dispatch — a delivered message assumes its slice is readable.
    Task DispatchAsync(int jobId, IReadOnlyList<ChunkRef> chunks, CancellationToken cancellationToken = default);
}
