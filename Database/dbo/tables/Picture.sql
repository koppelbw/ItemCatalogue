CREATE TABLE [Picture] (
    [Id] INT NOT NULL IDENTITY(1, 1) CONSTRAINT [PK_Picture] PRIMARY KEY,

    -- Key of the blob in Azure Blob Storage (e.g. "item/42/3f9c2b1a....jpg"); never a URL
    [BlobName] NVARCHAR(400) NOT NULL,
    [ContentType] NVARCHAR(100) NOT NULL,
    [SizeBytes] BIGINT NOT NULL,
    [OriginalFileName] NVARCHAR(255) NULL,
    [Caption] NVARCHAR(500) NULL,
    [IsPrimary] BIT NOT NULL DEFAULT 0,
    [SortOrder] INT NOT NULL DEFAULT 0,
    [WidthPixels] INT NULL,
    [HeightPixels] INT NULL,

    [LocationId] INT NULL,
    [RoomId] INT NULL,
    [ContainerId] INT NULL,
    [ItemId] INT NULL,

    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [LastModifiedDate] DATETIME2 NULL,
    [RowVersion] ROWVERSION NOT NULL,

    -- A picture belongs to exactly one owner (Location, Room, Container, or Item); all four FKs
    -- cascade so deleting the owner removes its pictures' rows (the blobs themselves are cleaned up
    -- by the application, not the database). Name matches the EF convention so the schema-drift
    -- test (SchemaDriftTests) verifies it by name.
    CONSTRAINT [FK_Picture_Location_LocationId]
        FOREIGN KEY ([LocationId])
        REFERENCES [Location]([Id])
        ON DELETE CASCADE,

    CONSTRAINT [FK_Picture_Room_RoomId]
        FOREIGN KEY ([RoomId])
        REFERENCES [Room]([Id])
        ON DELETE CASCADE,

    CONSTRAINT [FK_Picture_Container_ContainerId]
        FOREIGN KEY ([ContainerId])
        REFERENCES [Container]([Id])
        ON DELETE CASCADE,

    CONSTRAINT [FK_Picture_Item_ItemId]
        FOREIGN KEY ([ItemId])
        REFERENCES [Item]([Id])
        ON DELETE CASCADE,

    -- Ownership is exclusive: exactly one of the four owner columns is set. The application
    -- validators enforce this too; this is the database-level backstop.
    CONSTRAINT [CK_Picture_ExactlyOneOwner]
        CHECK (
            (CASE WHEN [LocationId] IS NOT NULL THEN 1 ELSE 0 END
           + CASE WHEN [RoomId] IS NOT NULL THEN 1 ELSE 0 END
           + CASE WHEN [ContainerId] IS NOT NULL THEN 1 ELSE 0 END
           + CASE WHEN [ItemId] IS NOT NULL THEN 1 ELSE 0 END) = 1
        ),

    INDEX [IX_Picture_LocationId] NONCLUSTERED ([LocationId]),
    INDEX [IX_Picture_RoomId] NONCLUSTERED ([RoomId]),
    INDEX [IX_Picture_ContainerId] NONCLUSTERED ([ContainerId]),
    INDEX [IX_Picture_ItemId] NONCLUSTERED ([ItemId])
);
