-- Seed_Door.sql
-- Doors reference Rooms (Door.FromRoomId required; Door.ToRoomId NULL = leads outside), so Room
-- seeds run first (see Script.PostDeployment.sql). A stair is a door whose FromRoom and ToRoom are
-- on different floors (Kind = Stairs). Geometry is in inches, relative to FromRoom's named wall.

SET IDENTITY_INSERT dbo.Door ON;

MERGE INTO dbo.Door AS target
USING (VALUES
    -- Id, Name,              Kind,      FromRoomId, ToRoomId, Wall,    Offset, Width, Height
    (1, 'Front Door',        'Door',      2,    NULL,    'South', 12.00, 36.00, 80.00),  -- Living Room -> outside
    (2, 'Bedroom Door',      'Doorway',   1,       2,    'East',   6.00, 32.00, 80.00),  -- Bedroom <-> Living Room (same floor)
    (3, 'Basement Stairs',   'Stairs',    7,       4,    'North', 24.00, 36.00, 84.00),  -- Basement -> First Floor (cross-level)
    (4, 'Bathroom Door',     'Door',      9,      10,    'West',   8.00, 28.00, 80.00),  -- Bathroom <-> Dining Room
    (5, 'Garage Side Door',  'Door',      5,    NULL,    'East',  10.00, 32.00, 80.00)   -- Garage -> outside
) AS source (Id, Name, Kind, FromRoomId, ToRoomId, Wall, OffsetInches, WidthInches, HeightInches)
ON target.Id = source.Id
WHEN MATCHED THEN UPDATE SET
    Name = source.Name,
    Kind = source.Kind,
    FromRoomId = source.FromRoomId,
    ToRoomId = source.ToRoomId,
    Wall = source.Wall,
    OffsetInches = source.OffsetInches,
    WidthInches = source.WidthInches,
    HeightInches = source.HeightInches
WHEN NOT MATCHED THEN
    INSERT (Id, Name, Kind, FromRoomId, ToRoomId, Wall, OffsetInches, WidthInches, HeightInches)
    VALUES (source.Id, source.Name, source.Kind, source.FromRoomId, source.ToRoomId, source.Wall, source.OffsetInches, source.WidthInches, source.HeightInches);

SET IDENTITY_INSERT dbo.Door OFF;
