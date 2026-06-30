-- Seed_Floor.sql
-- Each Floor belongs to a Location (Floor.LocationId). Location seeds run first (see
-- Script.PostDeployment.sql). Rooms reference floors (Room.FloorId), so floors seed before rooms.
-- LevelIndex is the signed vertical order (basement = -1, ground = 0, …); it is unique per Location.

SET IDENTITY_INSERT dbo.Floor ON;

MERGE INTO dbo.Floor AS target
USING (VALUES
    -- Id, LocationId, Name,          LevelIndex, ElevationInches, CeilingHeightInches
    (1,  1, 'Main',          0,  NULL,   96),   -- Apartment
    (2,  2, 'Main',          0,  NULL,   96),   -- Grandmas
    (3,  3, 'Basement',     -1, -96.00,  84),   -- House
    (4,  3, 'First Floor',   0,   0.00, 108),   -- House
    (5,  3, 'Second Floor',  1, 108.00,  96),   -- House
    (6,  3, 'Attic',         2, 216.00,  72),   -- House
    (7,  4, 'Main',          0,  NULL,  120),   -- Storage Unit
    (8,  5, 'Main',          0,  NULL,  NULL)   -- Car
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
