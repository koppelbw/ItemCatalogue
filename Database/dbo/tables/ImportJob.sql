CREATE TABLE [ImportJob] (
    [Id] INT NOT NULL IDENTITY(1, 1) CONSTRAINT [PK_ImportJob] PRIMARY KEY,
    [FileName] NVARCHAR(255) NOT NULL,

    -- Data rows in the uploaded file; TotalRows = RejectedAtIntake + EnqueuedRows. Progress and
    -- status are deliberately NOT columns here — they are derived from the ImportChunk marker
    -- rows, so concurrent chunk processors never contend on this row.
    [TotalRows] INT NOT NULL,
    [RejectedAtIntake] INT NOT NULL,
    [EnqueuedRows] INT NOT NULL,
    [TotalChunks] INT NOT NULL,

    -- Rows rejected synchronously at intake, as serialized ImportRowError JSON.
    [IntakeErrorsJson] NVARCHAR(MAX) NULL,

    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [LastModifiedDate] DATETIME2 NULL,
    [RowVersion] ROWVERSION NOT NULL
);
