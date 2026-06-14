CREATE TABLE [Collection] (
    [Id] INT NOT NULL IDENTITY(1, 1) CONSTRAINT [PK_Collection] PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [LastModifiedDate] DATETIME2 NULL,
    [RowVersion] ROWVERSION NOT NULL,

    -- A Collection's name identifies the curated set (e.g. "Catan + expansions"), so keep it unique.
    -- Unique index named after EF's convention so SchemaDriftTests verifies it by name.
    INDEX [IX_Collection_Name] UNIQUE NONCLUSTERED ([Name])
);
