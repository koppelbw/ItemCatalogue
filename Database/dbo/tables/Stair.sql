CREATE TABLE [Stair] (
    [Id] INT NOT NULL IDENTITY(1, 1) CONSTRAINT [PK_Stair] PRIMARY KEY,
    [Name] NVARCHAR(100) NULL,
    [FromRoomId] INT NOT NULL,
    -- NULL = leads to an exterior level.
    [ToRoomId] INT NULL,
    [Shape] NVARCHAR(50) NOT NULL,
    [PositionXInches] DECIMAL(9, 2) NULL,
    [PositionYInches] DECIMAL(9, 2) NULL,
    [Rotation] DECIMAL(6, 2) NULL,
    [RunInches] DECIMAL(9, 2) NULL,
    [WidthInches] DECIMAL(9, 2) NULL,
    [RiseInches] DECIMAL(9, 2) NULL,
    [StepCount] INT NULL,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [LastModifiedDate] DATETIME2 NULL,
    [RowVersion] ROWVERSION NOT NULL,

    CONSTRAINT [FK_Stair_Room_FromRoomId]
        FOREIGN KEY ([FromRoomId])
        REFERENCES [Room]([Id])
        ON DELETE NO ACTION,

    -- SET NULL + NO ACTION (above): two FKs to Room without a multiple-cascade-path, which SQL Server forbids.
    CONSTRAINT [FK_Stair_Room_ToRoomId]
        FOREIGN KEY ([ToRoomId])
        REFERENCES [Room]([Id])
        ON DELETE SET NULL,

    CONSTRAINT [CK_Stair_FromNotEqualTo]
        CHECK ([ToRoomId] IS NULL OR [ToRoomId] <> [FromRoomId]),

    INDEX [IX_Stair_FromRoomId] NONCLUSTERED ([FromRoomId]),
    INDEX [IX_Stair_ToRoomId] NONCLUSTERED ([ToRoomId])
);
