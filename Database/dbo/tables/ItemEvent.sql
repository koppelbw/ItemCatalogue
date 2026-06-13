CREATE TABLE ItemEvent (
    Id         INT            NOT NULL IDENTITY(1,1),
    ItemId     INT            NOT NULL,
    EventType  NVARCHAR(50)   NOT NULL,
    OccurredAt DATETIME2      NOT NULL,
    OldValue   NVARCHAR(500)  NULL,
    NewValue   NVARCHAR(500)  NULL,
    Notes      NVARCHAR(500)  NULL,

    CONSTRAINT PK_ItemEvent PRIMARY KEY (Id),

    -- Cascade so event rows are cleaned up if an Item row is ever hard-deleted.
    -- In practice Items use soft-delete (IsDeleted flag) and are never hard-deleted,
    -- but the cascade is a safety net against orphans.
    CONSTRAINT FK_ItemEvent_Item_ItemId FOREIGN KEY (ItemId) REFERENCES Item(Id) ON DELETE CASCADE,

    -- All reads are filtered by ItemId; index keeps history queries fast.
    INDEX IX_ItemEvent_ItemId NONCLUSTERED (ItemId)
);
