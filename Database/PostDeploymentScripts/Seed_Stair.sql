-- Seed_Stair.sql
-- Stairs reference Rooms (Stair.FromRoomId required; Stair.ToRoomId NULL = leads to an exterior
-- level), so Room seeds run first (see Script.PostDeployment.sql). FromRoom is the lower room.

SET IDENTITY_INSERT dbo.Stair ON;

MERGE INTO dbo.Stair AS target
USING (VALUES
    -- Id, Name,             FromRoomId, ToRoomId, Shape,      PosX,  PosY,  Rot,  Run,    Width, Rise,  Steps
    (1, 'Basement Stairs',   7,          4,        'Straight', 12.00, 24.00, 0.00, 120.00, 36.00, 96.00, 13)  -- Basement -> First Floor
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
