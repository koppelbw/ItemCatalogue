-- Since post-deployment scripts run every time you publish, they must be "idempotent"—meaning they won't fail or create duplicate data if run multiple times.

SET IDENTITY_INSERT dbo.Location ON;

MERGE INTO dbo.[Location] AS target
USING (
    VALUES
        (1, 'Apartment',    'My apartment - 2 bed / 1 bath, ~1,000 sq ft'),
        (2, 'Grandmas',     'Grandmas house'),
        (3, 'House',        'My house'),
        (4, 'Storage Unit', '#223'),
        (5, 'Car',          'Subaru Forester')
) AS source (Id, Name, Description)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        Name = source.Name,
        Description = source.Description
WHEN NOT MATCHED THEN
    INSERT (Id, Name, Description)
    VALUES (source.Id, source.Name, source.Description);

SET IDENTITY_INSERT dbo.Location OFF;
