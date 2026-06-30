-- Seed_Container.sql
-- A Container is owned by exactly one parent: RoomId (top-level) XOR ParentContainerId (nested).
-- Location/Floor/Room seeds run first; parent containers are listed before the children that
-- reference them (the self-referencing FK is NO ACTION, checked at statement end, so one MERGE is
-- fine). Placement is in inches: top-level containers are positioned in room space, nested ones in
-- parent-container space. Rotation is 0.

SET IDENTITY_INSERT dbo.Container ON;

MERGE INTO dbo.Container AS target
USING (VALUES
    -- Id, Name,         Description,             RoomId, ParentId, Type,       PosX,  PosY, PosZ,  Rot,  Width, Depth, Height, Color
    -- Top-level containers (RoomId set, ParentContainerId NULL)
    (1, 'Dresser',     'Bedroom dresser',         1,    NULL, 'Cabinet',  12.00,  6.00, 0.00, 0.00, 60.00, 20.00, 34.00, '#8B5A2B'),
    (2, 'Closet',      'Bedroom closet',          1,    NULL, 'Wardrobe', 96.00,  0.00, 0.00, 0.00, 48.00, 24.00, 84.00, '#D2B48C'),
    (3, 'Desk',        'Office desk',             4,    NULL, 'Cabinet',   8.00, 60.00, 0.00, 0.00, 60.00, 30.00, 30.00, '#6B4423'),
    (4, 'Cabinet',     'Garage cabinet',          5,    NULL, 'Cabinet',  10.00, 10.00, 0.00, 0.00, 36.00, 18.00, 72.00, '#C0C0C0'),
    -- Nested containers (ParentContainerId set, RoomId NULL)
    (5, 'Storage Bin', 'Bin inside the closet', NULL,      2, 'Bin',       2.00,  2.00, 6.00, 0.00, 18.00, 14.00, 12.00, '#4682B4'),
    (6, 'Box',         'Box inside the bin',    NULL,      5, 'Box',       1.00,  1.00, 0.00, 0.00,  8.00,  6.00,  6.00, '#DEB887')
) AS source (Id, Name, Description, RoomId, ParentContainerId, ContainerType, PositionXInches, PositionYInches, PositionZInches, Rotation, WidthInches, DepthInches, HeightInches, Color)
ON target.Id = source.Id
WHEN MATCHED THEN UPDATE SET
    Name = source.Name,
    Description = source.Description,
    RoomId = source.RoomId,
    ParentContainerId = source.ParentContainerId,
    ContainerType = source.ContainerType,
    PositionXInches = source.PositionXInches,
    PositionYInches = source.PositionYInches,
    PositionZInches = source.PositionZInches,
    Rotation = source.Rotation,
    WidthInches = source.WidthInches,
    DepthInches = source.DepthInches,
    HeightInches = source.HeightInches,
    Color = source.Color
WHEN NOT MATCHED THEN
    INSERT (Id, Name, Description, RoomId, ParentContainerId, ContainerType, PositionXInches, PositionYInches, PositionZInches, Rotation, WidthInches, DepthInches, HeightInches, Color)
    VALUES (source.Id, source.Name, source.Description, source.RoomId, source.ParentContainerId, source.ContainerType, source.PositionXInches, source.PositionYInches, source.PositionZInches, source.Rotation, source.WidthInches, source.DepthInches, source.HeightInches, source.Color);

SET IDENTITY_INSERT dbo.Container OFF;
