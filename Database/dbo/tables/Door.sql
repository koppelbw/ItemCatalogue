CREATE TABLE [Door] (
    [Id] INT NOT NULL IDENTITY(1, 1) CONSTRAINT [PK_Door] PRIMARY KEY,
    [Name] NVARCHAR(100) NULL,
    [Kind] NVARCHAR(50) NOT NULL,
    [FromRoomId] INT NOT NULL,
    -- NULL = the opening leads outside.
    [ToRoomId] INT NULL,
    -- Wall of FromRoom (North/East/South/West) the opening is cut into.
    [Wall] NVARCHAR(50) NOT NULL,
    [OffsetInches] DECIMAL(9, 2) NOT NULL,
    [WidthInches] DECIMAL(9, 2) NOT NULL,
    [HeightInches] DECIMAL(9, 2) NOT NULL,
    [HingeSide] NVARCHAR(50) NULL,
    [Swing] NVARCHAR(50) NULL,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [LastModifiedDate] DATETIME2 NULL,
    [RowVersion] ROWVERSION NOT NULL,

    -- The room the opening is placed in. EF maps this as OnDelete(Restrict), i.e. NO ACTION, so a
    -- Room with doors placed in it cannot be deleted until they are removed.
    CONSTRAINT [FK_Door_Room_FromRoomId]
        FOREIGN KEY ([FromRoomId])
        REFERENCES [Room]([Id])
        ON DELETE NO ACTION,

    -- The room the opening connects to. EF maps this as OnDelete(SetNull): deleting the far room
    -- turns the door into a leads-outside opening. SET NULL + NO ACTION (above) are two FKs to the
    -- same table without creating a multiple-cascade-path, which SQL Server forbids.
    CONSTRAINT [FK_Door_Room_ToRoomId]
        FOREIGN KEY ([ToRoomId])
        REFERENCES [Room]([Id])
        ON DELETE SET NULL,

    -- A door cannot connect a room to itself. Backstops the request validators; EF does not model it.
    CONSTRAINT [CK_Door_FromNotEqualTo]
        CHECK ([ToRoomId] IS NULL OR [ToRoomId] <> [FromRoomId]),

    -- EF emits an index per foreign key column (FromRoomId, ToRoomId). Names follow the EF
    -- convention so the schema-drift test verifies them by name.
    INDEX [IX_Door_FromRoomId] NONCLUSTERED ([FromRoomId]),
    INDEX [IX_Door_ToRoomId] NONCLUSTERED ([ToRoomId])
);
