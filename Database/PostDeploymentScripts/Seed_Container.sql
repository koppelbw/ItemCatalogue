-- Seed_Container.sql
-- A Container is owned by exactly one parent: RoomId (top-level) XOR ParentContainerId (nested).
-- Location/Room seeds run first; parent containers are listed before the children that reference
-- them (the self-referencing FK is NO ACTION, checked at statement end, so a single MERGE is fine).

SET IDENTITY_INSERT dbo.Container ON;

MERGE INTO dbo.Container AS target
USING (VALUES
    -- Top-level containers (RoomId set, ParentContainerId NULL)
    (1, 'Dresser',     'Bedroom dresser',        1,    NULL),
    (2, 'Closet',      'Bedroom closet',         1,    NULL),
    (3, 'Desk',        'Office desk',            4,    NULL),
    (4, 'Cabinet',     'Garage cabinet',         5,    NULL),
    -- Nested containers (ParentContainerId set, RoomId NULL)
    (5, 'Storage Bin', 'Bin inside the closet',  NULL, 2),
    (6, 'Box',         'Box inside the bin',     NULL, 5)
) AS source (Id, Name, Description, RoomId, ParentContainerId)
ON target.Id = source.Id
WHEN MATCHED THEN UPDATE SET
    Name = source.Name,
    Description = source.Description,
    RoomId = source.RoomId,
    ParentContainerId = source.ParentContainerId
WHEN NOT MATCHED THEN
    INSERT (Id, Name, Description, RoomId, ParentContainerId)
    VALUES (source.Id, source.Name, source.Description, source.RoomId, source.ParentContainerId);

SET IDENTITY_INSERT dbo.Container OFF;
