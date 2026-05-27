CREATE TABLE Item (
    Id                INT             NOT NULL IDENTITY(1,1) PRIMARY KEY,
    Name              NVARCHAR(255)   NOT NULL,
    Description       NVARCHAR(MAX)   NULL,
    Price             DECIMAL(18,2)   NULL,
    IsStored          BIT             NULL,
    IsDeleted         BIT             NULL,
    ReasonForDeletion INT             NULL,
    LocationId        INT             NULL,
    OwnerId           INT             NULL,
    CreatedDate       DATETIME2       NOT NULL DEFAULT GETUTCDATE(),
    LastModifiedDate  DATETIME2       NULL,

    CONSTRAINT FK_Items_Location FOREIGN KEY (LocationId) REFERENCES Location(Id),
    CONSTRAINT FK_Items_Owner    FOREIGN KEY (OwnerId)    REFERENCES Person(Id)
);