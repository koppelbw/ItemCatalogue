-- Seed_Collection.sql
-- Runs after Seed_Item (the CollectionItem rows below reference seeded items). Idempotent via MERGE.

SET IDENTITY_INSERT dbo.Collection ON;

MERGE INTO dbo.Collection AS target
USING (VALUES
    (1, 'Office Kit', 'Desk setup grouped as one logical kit: laptop, mouse, keyboard')
) AS source (Id, Name, Description)
ON target.Id = source.Id
WHEN MATCHED THEN UPDATE SET
    Name = source.Name,
    Description = source.Description
WHEN NOT MATCHED THEN
    INSERT (Id, Name, Description)
    VALUES (source.Id, source.Name, source.Description);

SET IDENTITY_INSERT dbo.Collection OFF;

-- Membership rows carry the rich-join payload (Quantity, SortOrder, Role). Composite-key MERGE.
MERGE INTO dbo.CollectionItem AS target
USING (VALUES
    (1, 1, 1, 0, 'Primary'),    -- Laptop
    (1, 2, 1, 1, 'Accessory'),  -- Wireless Mouse
    (1, 5, 1, 2, 'Accessory')   -- Mechanical Keyboard
) AS source (CollectionId, ItemId, Quantity, SortOrder, Role)
ON target.CollectionId = source.CollectionId AND target.ItemId = source.ItemId
WHEN MATCHED THEN UPDATE SET
    Quantity = source.Quantity,
    SortOrder = source.SortOrder,
    Role = source.Role
WHEN NOT MATCHED THEN
    INSERT (CollectionId, ItemId, Quantity, SortOrder, Role)
    VALUES (source.CollectionId, source.ItemId, source.Quantity, source.SortOrder, source.Role);
