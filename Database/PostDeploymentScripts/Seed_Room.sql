-- Seed_Room.sql

SET IDENTITY_INSERT dbo.Room ON;

MERGE INTO dbo.Room AS target
USING (VALUES
    (1, 'Bedroom',     'Main bedroom'),
    (2, 'Living Room', 'Main living area'),
    (3, 'Kitchen',     'Kitchen'),
    (4, 'Office',      'Home office'),
    (5, 'Garage',      'Garage'),
    (6, 'Storage',     'Storage room'),
    (7, 'Basement',    'Basement'),
    (8, 'Attic',       'Attic'),
    (9, 'Bathroom',    'Bathroom'),
    (10, 'Dining Room', 'Dining room'),
    (11, 'Glove box', 'Glove box of the car'),
    (12, 'Trunk', 'Trunk of the car')
) AS source (Id, Name, Description)
ON target.Id = source.Id
WHEN MATCHED THEN UPDATE SET Name = source.Name, Description = source.Description
WHEN NOT MATCHED THEN INSERT (Id, Name, Description) VALUES (source.Id, source.Name, source.Description);


SET IDENTITY_INSERT dbo.Room OFF;
