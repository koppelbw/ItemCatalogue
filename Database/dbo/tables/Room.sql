CREATE TABLE [Room] (
    [Id] INT NOT NULL IDENTITY(1, 1) CONSTRAINT [PK_Room] PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [LocationId] INT NOT NULL,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [LastModifiedDate] DATETIME2 NULL,
    [RowVersion] ROWVERSION NOT NULL,

    -- Foreign Key to the owning Location. EF maps this as OnDelete(Restrict), i.e. NO ACTION, so a
    -- Location that still has Rooms cannot be deleted. Name matches the EF convention so the
    -- schema-drift test (SchemaDriftTests) verifies it by name.
    CONSTRAINT [FK_Room_Location_LocationId]
        FOREIGN KEY ([LocationId])
        REFERENCES [Location]([Id])
        ON DELETE NO ACTION,

    INDEX [IX_Room_LocationId] NONCLUSTERED ([LocationId])
);
