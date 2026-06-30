-- Seed_Room.sql
-- Each Room belongs to a Floor (Room.FloorId). Floor seeds run first (see Script.PostDeployment.sql).
-- Geometry is in inches; RoomType/colours are illustrative. Rotation is 0 (axis-aligned footprints).

SET IDENTITY_INSERT dbo.Room ON;

MERGE INTO dbo.Room AS target
USING (VALUES
    -- Id, Name,         Description,            FloorId, RoomType,     OX,    OY,  Width, Depth, Height, Rot,  WallColor,  FloorColor
    (1,  'Bedroom',     'Main bedroom',          1, 'Bedroom',      0.00,   0.00, 144.00, 120.00,  96.00, 0.00, '#E8E0D5', '#8B5A2B'),
    (2,  'Living Room', 'Main living area',      1, 'LivingRoom', 150.00,   0.00, 180.00, 144.00,  96.00, 0.00, '#F5F5DC', '#A0522D'),
    (3,  'Kitchen',     'Kitchen',               2, 'Kitchen',      0.00,   0.00, 120.00, 120.00,  96.00, 0.00, '#FFFFFF', '#D2B48C'),
    (4,  'Office',      'Home office',           4, 'Office',       0.00,   0.00, 120.00, 108.00, 108.00, 0.00, '#DDE6ED', '#6B4423'),
    (5,  'Garage',      'Garage',                4, 'Garage',     130.00,   0.00, 240.00, 240.00, 108.00, 0.00, '#C0C0C0', '#808080'),
    (6,  'Storage',     'Storage room',          7, 'Other',        0.00,   0.00,  96.00,  96.00, 120.00, 0.00, '#D3D3D3', '#A9A9A9'),
    (7,  'Basement',    'Basement',              3, 'Basement',     0.00,   0.00, 360.00, 300.00,  84.00, 0.00, '#B0B0B0', '#696969'),
    (8,  'Attic',       'Attic',                 6, 'Attic',        0.00,   0.00, 300.00, 240.00,  72.00, 0.00, '#C8B89A', '#8B7355'),
    (9,  'Bathroom',    'Bathroom',              4, 'Bathroom',     0.00, 120.00,  72.00,  96.00, 108.00, 0.00, '#E0F7FA', '#B0BEC5'),
    (10, 'Dining Room', 'Dining room',           4, 'DiningRoom', 130.00, 120.00, 144.00, 120.00, 108.00, 0.00, '#FFF8E1', '#8D6E63'),
    (11, 'Glove box',   'Glove box of the car',  8, 'Other',        0.00,   0.00,  18.00,  12.00,   6.00, 0.00, '#2F2F2F', '#1C1C1C'),
    (12, 'Trunk',       'Trunk of the car',      8, 'Other',       24.00,   0.00,  48.00,  36.00,  30.00, 0.00, '#2F2F2F', '#1C1C1C')
) AS source (Id, Name, Description, FloorId, RoomType, OriginXInches, OriginYInches, WidthInches, DepthInches, HeightInches, Rotation, WallColor, FloorColor)
ON target.Id = source.Id
WHEN MATCHED THEN UPDATE SET
    Name = source.Name,
    Description = source.Description,
    FloorId = source.FloorId,
    RoomType = source.RoomType,
    OriginXInches = source.OriginXInches,
    OriginYInches = source.OriginYInches,
    WidthInches = source.WidthInches,
    DepthInches = source.DepthInches,
    HeightInches = source.HeightInches,
    Rotation = source.Rotation,
    WallColor = source.WallColor,
    FloorColor = source.FloorColor
WHEN NOT MATCHED THEN
    INSERT (Id, Name, Description, FloorId, RoomType, OriginXInches, OriginYInches, WidthInches, DepthInches, HeightInches, Rotation, WallColor, FloorColor)
    VALUES (source.Id, source.Name, source.Description, source.FloorId, source.RoomType, source.OriginXInches, source.OriginYInches, source.WidthInches, source.DepthInches, source.HeightInches, source.Rotation, source.WallColor, source.FloorColor);

SET IDENTITY_INSERT dbo.Room OFF;
