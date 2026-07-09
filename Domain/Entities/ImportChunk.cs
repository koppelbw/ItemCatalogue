namespace Domain.Entities;

// Processed-chunk marker: one row per completed queue message, written in the SAME transaction as
// the chunk's items. The unique (JobId, ChunkIndex) index makes that transaction the idempotency
// guard — a redelivered message violates it and rolls back atomically, so items are never
// duplicated. Insert-only (like ItemEvent), hence no IEntity/IAuditable/RowVersion.
public class ImportChunk
{
    public int Id { get; set; }

    public int JobId { get; set; }

    public ImportJob? Job { get; set; }

    // 0-based position of the chunk within the job's payload.
    public int ChunkIndex { get; set; }

    public int Succeeded { get; set; }

    public int Failed { get; set; }

    // Stamped by the processor from TimeProvider (same clock convention as ItemEvent.OccurredAt).
    public DateTime ProcessedAt { get; set; }

    // Row-level failures for this chunk as serialized ImportRowError JSON.
    public string? ErrorsJson { get; set; }
}
