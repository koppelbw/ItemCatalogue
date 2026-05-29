CREATE TABLE [Location] (
    [Id] INT NOT NULL PRIMARY KEY IDENTITY(1, 1),
    [Name] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(500) NULL,
    [RoomId] INT NOT NULL,
    
    -- Foreign Key to Room
    CONSTRAINT [FK_Location_Room] 
        FOREIGN KEY ([RoomId]) 
        REFERENCES [Room]([Id])
        ON DELETE NO ACTION
);