CREATE TABLE [Floor] (
    [Id] INT NOT NULL IDENTITY(1, 1) CONSTRAINT [PK_Floor] PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [LocationId] INT NOT NULL,

    -- Signed vertical order so levels sort: basement = -1, ground = 0, second = 1, attic = 2.
    [LevelIndex] INT NOT NULL,
    [ElevationInches] DECIMAL(9, 2) NULL,
    [CeilingHeightInches] DECIMAL(9, 2) NULL,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [LastModifiedDate] DATETIME2 NULL,
    [RowVersion] ROWVERSION NOT NULL,

    -- Foreign key to the owning Location. EF maps this as OnDelete(Restrict), i.e. NO ACTION, so a
    -- Location that still has Floors cannot be deleted. Name matches the EF convention so the
    -- schema-drift test (SchemaDriftTests) verifies it by name.
    CONSTRAINT [FK_Floor_Location_LocationId]
        FOREIGN KEY ([LocationId])
        REFERENCES [Location]([Id])
        ON DELETE NO ACTION,

    -- One floor per level per location. EF declares this via HasIndex(...).IsUnique(); its leading
    -- column (LocationId) also covers the foreign key, so EF does NOT emit a separate IX_Floor_LocationId.
    INDEX [IX_Floor_LocationId_LevelIndex] UNIQUE NONCLUSTERED ([LocationId], [LevelIndex])
);
