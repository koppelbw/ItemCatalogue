CREATE TABLE Item (
    Id                INT             NOT NULL IDENTITY(1,1),
    Name              NVARCHAR(255)   NOT NULL,
    Description       NVARCHAR(MAX)   NULL,
    Price             DECIMAL(18,2)   NULL,
    IsStored          BIT             NOT NULL DEFAULT 0,
    IsDeleted         BIT             NOT NULL DEFAULT 0,
    ReasonForDeletion NVARCHAR(255)   NULL,
    ItemTypes         NVARCHAR(MAX)   NOT NULL DEFAULT '[]',
    RoomId            INT             NULL,
    OwnerId           INT             NULL,
    CreatedDate       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    LastModifiedDate  DATETIME2       NULL,
    RowVersion        ROWVERSION      NOT NULL,

    CONSTRAINT PK_Item PRIMARY KEY (Id),

    -- ON DELETE SET NULL: deleting a referenced Room/Person clears the item's FK rather than
    -- blocking the delete. Matches the EF model (ItemCatalogueDbContext: OnDelete(SetNull)).
    -- Constraint and index names follow EF Core's conventions (FK_<Table>_<Principal>_<Col>,
    -- IX_<Table>_<Col>) so the schema-drift test (SchemaDriftTests) can verify them by name.
    CONSTRAINT FK_Item_Room_RoomId    FOREIGN KEY (RoomId)  REFERENCES Room(Id)   ON DELETE SET NULL,
    CONSTRAINT FK_Item_Person_OwnerId FOREIGN KEY (OwnerId) REFERENCES Person(Id) ON DELETE SET NULL,

    -- Supporting indexes on the FK columns. EF would emit these in a migration; under SSDT they are
    -- declared explicitly so unindexed FKs don't hurt join/cascade performance.
    INDEX IX_Item_RoomId  NONCLUSTERED (RoomId),
    INDEX IX_Item_OwnerId NONCLUSTERED (OwnerId)
);