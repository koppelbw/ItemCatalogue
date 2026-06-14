-- Seed_Tag.sql
-- Runs after Seed_Item (the ItemTag rows below reference seeded items). Idempotent via MERGE.

SET IDENTITY_INSERT dbo.Tag ON;

MERGE INTO dbo.Tag AS target
USING (VALUES
    (1, 'Fragile',     'Handle with care'),
    (2, 'Camping',     'Outdoor and camping gear'),
    (3, 'Electronics', 'Powered electronic devices')
) AS source (Id, Name, Description)
ON target.Id = source.Id
WHEN MATCHED THEN UPDATE SET
    Name = source.Name,
    Description = source.Description
WHEN NOT MATCHED THEN
    INSERT (Id, Name, Description)
    VALUES (source.Id, source.Name, source.Description);

SET IDENTITY_INSERT dbo.Tag OFF;

-- Tag a couple of the seeded items. Composite-key MERGE keeps this idempotent across re-publishes.
MERGE INTO dbo.ItemTag AS target
USING (VALUES
    (1, 3),   -- Laptop -> Electronics
    (5, 3),   -- Mechanical Keyboard -> Electronics
    (5, 1)    -- Mechanical Keyboard -> Fragile
) AS source (ItemId, TagId)
ON target.ItemId = source.ItemId AND target.TagId = source.TagId
WHEN NOT MATCHED THEN
    INSERT (ItemId, TagId)
    VALUES (source.ItemId, source.TagId);
