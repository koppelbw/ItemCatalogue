CREATE TABLE [Container] (
    [Id] INT NOT NULL IDENTITY(1, 1) CONSTRAINT [PK_Container] PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [RoomId] INT NULL,
    [ParentContainerId] INT NULL,
    
    -- ContainerType stored as a string (Box, Shelf, Cabinet, …); nullable.
    [ContainerType] NVARCHAR(50) NULL,
    
    -- Placement (inches) in room space (RoomId set) or parent-container space (ParentContainerId set).
    -- All nullable: a container may be catalogued before it is positioned/sized. PositionZ is height
    -- off the floor (wall shelves / stacked units).
    [PositionXInches] DECIMAL(9, 2) NULL,
    [PositionYInches] DECIMAL(9, 2) NULL,
    [PositionZInches] DECIMAL(9, 2) NULL,
    [Rotation] DECIMAL(6, 2) NULL,
    [WidthInches] DECIMAL(9, 2) NULL,
    [DepthInches] DECIMAL(9, 2) NULL,
    [HeightInches] DECIMAL(9, 2) NULL,
    [Color] NVARCHAR(9) NULL,

    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [LastModifiedDate] DATETIME2 NULL,
    [RowVersion] ROWVERSION NOT NULL,

    -- A top-level container is owned by a Room; EF maps this as OnDelete(Restrict), i.e. NO ACTION,
    -- so a Room that still has containers cannot be deleted. Name matches the EF convention so the
    -- schema-drift test (SchemaDriftTests) verifies it by name.
    CONSTRAINT [FK_Container_Room_RoomId]
        FOREIGN KEY ([RoomId])
        REFERENCES [Room]([Id])
        ON DELETE NO ACTION,

    -- A nested container is owned by a parent container (self-reference). NO ACTION because SQL
    -- Server forbids cascade on a self-referencing FK; a parent with children cannot be deleted.
    CONSTRAINT [FK_Container_Container_ParentContainerId]
        FOREIGN KEY ([ParentContainerId])
        REFERENCES [Container]([Id])
        ON DELETE NO ACTION,

    -- Ownership is exclusive: exactly one of RoomId / ParentContainerId is set (top-level vs nested).
    -- The application validators enforce this too; this is the database-level backstop.
    CONSTRAINT [CK_Container_RoomXorParent]
        CHECK (([RoomId] IS NOT NULL AND [ParentContainerId] IS NULL)
            OR ([RoomId] IS NULL AND [ParentContainerId] IS NOT NULL)),

    INDEX [IX_Container_RoomId] NONCLUSTERED ([RoomId]),
    INDEX [IX_Container_ParentContainerId] NONCLUSTERED ([ParentContainerId])
);
