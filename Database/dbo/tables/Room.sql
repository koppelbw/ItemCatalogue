CREATE TABLE [Room] (
    [Id] INT NOT NULL IDENTITY(1, 1) CONSTRAINT [PK_Room] PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [FloorId] INT NOT NULL,

    -- RoomType stored as a string (Bedroom, Kitchen, …); nullable (uncategorised allowed).
    [RoomType] NVARCHAR(50) NULL,

    -- Footprint geometry (inches). Origin is the room's reference corner on the floor plane; the room
    -- is a Width x Depth rectangle rotated Rotation degrees about that corner. All nullable: a room
    -- may exist before it is measured/placed. Height overrides the floor's default ceiling height.
    [OriginXInches] DECIMAL(9, 2) NULL,
    [OriginYInches] DECIMAL(9, 2) NULL,
    [WidthInches] DECIMAL(9, 2) NULL,
    [DepthInches] DECIMAL(9, 2) NULL,
    [HeightInches] DECIMAL(9, 2) NULL,
    [Rotation] DECIMAL(6, 2) NULL,

    -- Surface colours as hex strings ("#RRGGBB" or "#RRGGBBAA").
    [WallColor] NVARCHAR(9) NULL,
    [FloorColor] NVARCHAR(9) NULL,
    [CeilingColor] NVARCHAR(9) NULL,
    [CreatedDate] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [LastModifiedDate] DATETIME2 NULL,
    [RowVersion] ROWVERSION NOT NULL,

    -- Foreign key to the owning Floor. EF maps this as OnDelete(Restrict), i.e. NO ACTION, so a
    -- Floor that still has Rooms cannot be deleted. Name matches the EF convention so the
    -- schema-drift test (SchemaDriftTests) verifies it by name.
    CONSTRAINT [FK_Room_Floor_FloorId]
        FOREIGN KEY ([FloorId])
        REFERENCES [Floor]([Id])
        ON DELETE NO ACTION,

    INDEX [IX_Room_FloorId] NONCLUSTERED ([FloorId])
);
