-- Since post-deployment scripts run every time you publish, they must be "idempotent"—meaning they won't fail or create duplicate data if run multiple times.

SET IDENTITY_INSERT dbo.Person ON;

MERGE INTO dbo.Person AS target
USING (
    VALUES
        (1, 'Bill'),
        (2, 'Jen'),
        (3, 'Oscar'),
        (4, 'Bowie')
) AS source (Id, Name)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET Name = source.Name
WHEN NOT MATCHED THEN
    INSERT (Id, Name)
    VALUES (source.Id, source.Name);

SET IDENTITY_INSERT dbo.Person OFF;
