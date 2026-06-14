CREATE TABLE [Tag] (
    [Id] INT NOT NULL IDENTITY(1, 1) CONSTRAINT [PK_Tag] PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [LastModifiedDate] DATETIME2 NULL,
    [RowVersion] ROWVERSION NOT NULL,

    -- A Tag's name is the user's vocabulary term, so it must be unique. EF models this as a unique
    -- index named by its convention (IX_<Table>_<Col>); declaring it the same way here lets the
    -- schema-drift test (SchemaDriftTests) verify it by name rather than as a UNIQUE constraint.
    INDEX [IX_Tag_Name] UNIQUE NONCLUSTERED ([Name])
);
