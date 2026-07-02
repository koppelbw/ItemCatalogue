-- Seed_Floor.sql
-- Each Floor belongs to a Location (Floor.LocationId). Location seeds run first (see
-- Script.PostDeployment.sql). Rooms reference floors (Room.FloorId), so floors seed before rooms.
-- LevelIndex is the signed vertical order (basement = -1, ground = 0, …); it is unique per Location.

SET IDENTITY_INSERT dbo.Floor ON;

MERGE INTO dbo.Floor AS target
USING (VALUES
    -- Id, LocationId, Name,          LevelIndex, ElevationInches, CeilingHeightInches
    (1,  1, 'Main',          0,  NULL,   96),   -- Apartment
    -- Grandmas is a 5-level townhouse (basement / first / second / third / attic); floor 2 is its
    -- first floor. Floors 9-12 add the rest. Stairs connect consecutive levels (see Seed_Stair.sql).
    (2,  2, 'First Floor',   0,   0.00,  96),   -- Grandmas
    -- House is single-story + small attic (no basement, no stairs; attic reached by a hatch).
    -- Ids 3 and 5 (Basement, Second Floor) are retired; Seed_Cleanup.sql deletes them.
    (4,  3, 'Main',          0,   0.00,  96),   -- House
    (6,  3, 'Attic',         2, 102.00,  48),   -- House (small attic; LevelIndex 2 kept for stable ids)
    (7,  4, 'Main',          0,  NULL,  120),   -- Storage Unit
    (8,  5, 'Main',          0,  NULL,  NULL),  -- Car
    (9,  2, 'Basement',     -1, -96.00,  84),   -- Grandmas
    (10, 2, 'Second Floor',  1, 108.00,  96),   -- Grandmas
    (11, 2, 'Third Floor',   2, 216.00,  96),   -- Grandmas
    (12, 2, 'Attic',         3, 324.00,  72)    -- Grandmas
) AS source (Id, LocationId, Name, LevelIndex, ElevationInches, CeilingHeightInches)
ON target.Id = source.Id
WHEN MATCHED THEN UPDATE SET
    LocationId = source.LocationId,
    Name = source.Name,
    LevelIndex = source.LevelIndex,
    ElevationInches = source.ElevationInches,
    CeilingHeightInches = source.CeilingHeightInches
WHEN NOT MATCHED THEN
    INSERT (Id, LocationId, Name, LevelIndex, ElevationInches, CeilingHeightInches)
    VALUES (source.Id, source.LocationId, source.Name, source.LevelIndex, source.ElevationInches, source.CeilingHeightInches);

SET IDENTITY_INSERT dbo.Floor OFF;
