CREATE TABLE [ImportJob] (
    [Id] INT NOT NULL IDENTITY(1, 1) CONSTRAINT [PK_ImportJob] PRIMARY KEY,
    [FileName] NVARCHAR(255) NOT NULL,
    [TotalRows] INT NOT NULL,
    [RejectedAtIntake] INT NOT NULL,
    [EnqueuedRows] INT NOT NULL,
    [TotalChunks] INT NOT NULL,
    [IntakeErrorsJson] NVARCHAR(MAX) NULL,

    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [LastModifiedDate] DATETIME2 NULL,
    [RowVersion] ROWVERSION NOT NULL
);
