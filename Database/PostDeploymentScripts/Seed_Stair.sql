-- Seed_Stair.sql
-- Stairs reference Rooms (Stair.FromRoomId = lower room; Stair.ToRoomId NULL = leads to an exterior
-- level), so Room seeds run first (see Script.PostDeployment.sql). Geometry is in inches.
-- Grandmas is a 5-level townhouse: one straight flight connects each pair of consecutive stair
-- halls/landings (all in the 60-wide west stair shaft). RiseInches is the floor-to-floor height.

SET IDENTITY_INSERT dbo.Stair ON;

MERGE INTO dbo.Stair AS target
USING (VALUES
    -- Id, Name,                 FromRoom, ToRoom, Shape,      PosX,   PosY,  Rot,   Run,    Width,  Rise,  Steps
    (1, 'Basement Stairs',        37, 41, 'Straight', 10.00, 130.00, 0.00, 120.00, 40.00,  96.00, 14),  -- Basement hall -> Foyer
    (2, 'First Floor Stairs',     41, 45, 'Straight', 10.00, 130.00, 0.00, 130.00, 40.00, 108.00, 16),  -- Foyer -> 2nd landing
    (3, 'Second Floor Stairs',    45, 49, 'Straight', 10.00, 130.00, 0.00, 130.00, 40.00, 108.00, 16),  -- 2nd landing -> 3rd landing
    (4, 'Third Floor Stairs',     49, 54, 'Straight', 10.00, 130.00, 0.00, 130.00, 40.00, 108.00, 16)   -- 3rd landing -> Attic landing
) AS source (Id, Name, FromRoomId, ToRoomId, Shape, PositionXInches, PositionYInches, Rotation, RunInches, WidthInches, RiseInches, StepCount)
ON target.Id = source.Id
WHEN MATCHED THEN UPDATE SET
    Name = source.Name,
    FromRoomId = source.FromRoomId,
    ToRoomId = source.ToRoomId,
    Shape = source.Shape,
    PositionXInches = source.PositionXInches,
    PositionYInches = source.PositionYInches,
    Rotation = source.Rotation,
    RunInches = source.RunInches,
    WidthInches = source.WidthInches,
    RiseInches = source.RiseInches,
    StepCount = source.StepCount
WHEN NOT MATCHED THEN
    INSERT (Id, Name, FromRoomId, ToRoomId, Shape, PositionXInches, PositionYInches, Rotation, RunInches, WidthInches, RiseInches, StepCount)
    VALUES (source.Id, source.Name, source.FromRoomId, source.ToRoomId, source.Shape, source.PositionXInches, source.PositionYInches, source.Rotation, source.RunInches, source.WidthInches, source.RiseInches, source.StepCount);

SET IDENTITY_INSERT dbo.Stair OFF;
