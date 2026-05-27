CREATE TABLE ItemItemTypes (
    ItemId   INT NOT NULL,
    ItemType INT NOT NULL,  -- maps to your ItemType enum values

    CONSTRAINT PK_ItemItemTypes PRIMARY KEY (ItemId, ItemType),
    CONSTRAINT FK_ItemItemTypes_Item FOREIGN KEY (ItemId) REFERENCES Item(Id)
);