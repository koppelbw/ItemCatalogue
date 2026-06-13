CREATE TABLE [Container] (
    [Id] INT NOT NULL IDENTITY(1, 1) CONSTRAINT [PK_Container] PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [RoomId] INT NULL,
    [ParentContainerId] INT NULL,
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
