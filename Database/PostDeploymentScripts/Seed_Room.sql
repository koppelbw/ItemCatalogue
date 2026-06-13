-- Seed_Room.sql
-- Each Room belongs to a Location (Room.LocationId). Location seeds run first (see Script.PostDeployment.sql).

SET IDENTITY_INSERT dbo.Room ON;

MERGE INTO dbo.Room AS target
USING (VALUES
    (1,  'Bedroom',     'Main bedroom',       1),
    (2,  'Living Room', 'Main living area',   1),
    (3,  'Kitchen',     'Kitchen',            2),
    (4,  'Office',      'Home office',        3),
    (5,  'Garage',      'Garage',             3),
    (6,  'Storage',     'Storage room',       4),
    (7,  'Basement',    'Basement',           3),
    (8,  'Attic',       'Attic',              3),
    (9,  'Bathroom',    'Bathroom',           3),
    (10, 'Dining Room', 'Dining room',        3),
    (11, 'Glove box',   'Glove box of the car', 5),
    (12, 'Trunk',       'Trunk of the car',   5)
) AS source (Id, Name, Description, LocationId)
ON target.Id = source.Id
WHEN MATCHED THEN UPDATE SET Name = source.Name, Description = source.Description, LocationId = source.LocationId
WHEN NOT MATCHED THEN INSERT (Id, Name, Description, LocationId) VALUES (source.Id, source.Name, source.Description, source.LocationId);


SET IDENTITY_INSERT dbo.Room OFF;
