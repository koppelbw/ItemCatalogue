CREATE TABLE [Location] (
    [Id] INT NOT NULL IDENTITY(1, 1) CONSTRAINT [PK_Location] PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [RoomId] INT NOT NULL,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [LastModifiedDate] DATETIME2 NULL,
    [RowVersion] ROWVERSION NOT NULL,

    -- Foreign Key to Room. EF maps this as OnDelete(Restrict), i.e. NO ACTION. Name matches the EF
    -- convention so the schema-drift test (SchemaDriftTests) verifies it by name.
    CONSTRAINT [FK_Location_Room_RoomId]
        FOREIGN KEY ([RoomId])
        REFERENCES [Room]([Id])
        ON DELETE NO ACTION,

    INDEX [IX_Location_RoomId] NONCLUSTERED ([RoomId])
);