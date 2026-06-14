-- Join table for the Item <-> Tag many-to-many (a plain join: it carries no payload of its own,
-- only the two foreign keys). The composite primary key (ItemId, TagId) makes each pairing unique.
-- Both FKs CASCADE so deleting an Item or a Tag automatically removes its join rows; the two cascade
-- paths come from different parent tables, so there is no multiple-cascade-path cycle. Constraint and
-- index names follow EF's conventions so SchemaDriftTests can verify them by name.
CREATE TABLE [ItemTag] (
    [ItemId] INT NOT NULL,
    [TagId] INT NOT NULL,

    CONSTRAINT [PK_ItemTag] PRIMARY KEY ([ItemId], [TagId]),

    CONSTRAINT [FK_ItemTag_Item_ItemId]
        FOREIGN KEY ([ItemId])
        REFERENCES [Item]([Id])
        ON DELETE CASCADE,

    CONSTRAINT [FK_ItemTag_Tag_TagId]
        FOREIGN KEY ([TagId])
        REFERENCES [Tag]([Id])
        ON DELETE CASCADE,

    -- ItemId is covered by the leading column of the PK; TagId needs its own index for the reverse
    -- lookup (and the cascade from Tag). EF emits exactly this one index for the join entity.
    INDEX [IX_ItemTag_TagId] NONCLUSTERED ([TagId])
);
