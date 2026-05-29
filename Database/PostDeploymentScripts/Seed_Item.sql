-- Since post-deployment scripts run every time you publish, they must be "idempotent"—meaning they won't fail or create duplicate data if run multiple times.
SET IDENTITY_INSERT dbo.Item ON;

MERGE INTO dbo.Item AS target
USING (
    VALUES 
        (1, 'Laptop', 'High-performance laptop with 16GB RAM', 1299.99, 1, 0, NULL, 1, 1),
        (2, 'Wireless Mouse', 'Ergonomic wireless mouse with USB receiver', 29.99, 1, 0, NULL, 1, 1),
        (3, 'USB-C Cable', '2-meter USB-C charging cable', 12.99, 1, 0, NULL, 2, 2),
        (4, 'Monitor Stand', 'Adjustable monitor stand for dual displays', 49.99, 1, 0, NULL, 3, 3),
        (5, 'Mechanical Keyboard', 'RGB mechanical keyboard with Cherry MX switches', 149.99, 1, 0, NULL, 4, 4)
) AS source (Id, Name, Description, Price, IsStored, IsDeleted, ReasonForDeletion, LocationId, OwnerId)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET 
        Name = source.Name,
        Description = source.Description,
        Price = source.Price,
        IsStored = source.IsStored,
        IsDeleted = source.IsDeleted,
        ReasonForDeletion = source.ReasonForDeletion,
        LocationId = source.LocationId,
        OwnerId = source.OwnerId
WHEN NOT MATCHED THEN
    INSERT (Id, Name, Description, Price, IsStored, IsDeleted, ReasonForDeletion, LocationId, OwnerId)
    VALUES (source.Id, source.Name, source.Description, source.Price, source.IsStored, source.IsDeleted, source.ReasonForDeletion, source.LocationId, source.OwnerId);

SET IDENTITY_INSERT dbo.Item OFF;