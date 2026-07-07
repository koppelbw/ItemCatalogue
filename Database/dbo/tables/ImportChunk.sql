CREATE TABLE [ImportChunk] (
    [Id] INT NOT NULL IDENTITY(1, 1) CONSTRAINT [PK_ImportChunk] PRIMARY KEY,
    [JobId] INT NOT NULL,

    -- 0-based position of the chunk within the job's payload.
    [ChunkIndex] INT NOT NULL,
    [Succeeded] INT NOT NULL,
    [Failed] INT NOT NULL,
    [ProcessedAt] DATETIME2 NOT NULL,

    -- Row-level failures for this chunk as serialized ImportRowError JSON.
    [ErrorsJson] NVARCHAR(MAX) NULL,

    -- Cascade so deleting a job removes its markers. Name matches the EF convention so the
    -- schema-drift test (SchemaDriftTests) verifies it by name.
    CONSTRAINT [FK_ImportChunk_ImportJob_JobId]
        FOREIGN KEY ([JobId])
        REFERENCES [ImportJob]([Id])
        ON DELETE CASCADE,

    -- The bulk-import idempotency guard: the marker and its items are written in one transaction,
    -- so a redelivered queue message violates this index and rolls back without duplicating items.
    -- Leading column (JobId) also covers the FK, so EF does NOT emit a separate IX_ImportChunk_JobId.
    INDEX [IX_ImportChunk_JobId_ChunkIndex] UNIQUE NONCLUSTERED ([JobId], [ChunkIndex])
);
