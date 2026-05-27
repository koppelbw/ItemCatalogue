-- Since post-deployment scripts run every time you publish, they must be "idempotent"—meaning they won't fail or create duplicate data if run multiple times.

SET IDENTITY_INSERT dbo.Location ON;

MERGE INTO dbo.[Location] AS target
USING (
    VALUES 
        (1, 'Bedroom', 'Upstairs', NULL),
        (2, 'Living Room', 'With TV', NULL),
        (3, 'Kitchen', 'only kitchen', NULL),
        (4, 'Office', 'Upstairs', NULL),
        (5, 'Garage', 'the garage', NULL)
) AS source (Id, Name, Description, RoomId)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET 
        Name = source.Name,
        Description = source.Description,
        RoomId = source.RoomId
WHEN NOT MATCHED THEN
    INSERT (Id, Name, Description, RoomId)
    VALUES (source.Id, source.Name, source.Description, source.RoomId);

SET IDENTITY_INSERT dbo.Location OFF;