-- Join table for the Collection <-> Item many-to-many. Unlike ItemTag this is a *rich* join: it
-- carries its own data describing the membership (Quantity, SortOrder for display, and an optional
-- Role such as "base game" vs "expansion"). The composite primary key (CollectionId, ItemId) makes
-- each membership unique. Both FKs CASCADE so deleting a Collection or an Item removes its membership
-- rows; the two cascade paths come from different parent tables, so there is no cascade cycle.
CREATE TABLE [CollectionItem] (
    [CollectionId] INT NOT NULL,
    [ItemId] INT NOT NULL,
    [Quantity] INT NOT NULL DEFAULT 1,
    [SortOrder] INT NOT NULL DEFAULT 0,
    [Role] NVARCHAR(100) NULL,

    CONSTRAINT [PK_CollectionItem] PRIMARY KEY ([CollectionId], [ItemId]),

    CONSTRAINT [FK_CollectionItem_Collection_CollectionId]
        FOREIGN KEY ([CollectionId])
        REFERENCES [Collection]([Id])
        ON DELETE CASCADE,

    CONSTRAINT [FK_CollectionItem_Item_ItemId]
        FOREIGN KEY ([ItemId])
        REFERENCES [Item]([Id])
        ON DELETE CASCADE,

    -- CollectionId is the leading PK column; ItemId needs its own index for the reverse lookup
    -- ("which collections is this item in?") and the cascade from Item.
    INDEX [IX_CollectionItem_ItemId] NONCLUSTERED ([ItemId])
);
