-- Since post-deployment scripts run every time you publish, they must be "idempotent"—meaning they won't fail or create duplicate data if run multiple times.
SET IDENTITY_INSERT dbo.Item ON;

-- RoomId XOR ContainerId per item (or neither): item 1 lives inside a Container (Desk), the rest sit
-- directly in a Room. Mirrors the "not both" rule the API validators enforce.
MERGE INTO dbo.Item AS target
USING (
    VALUES
        (1, 'Laptop', 'High-performance laptop with 16GB RAM', 1299.99, 800.00, 'Dell', 'XPS 15', 'DL-XPS15-001', 'Dell.com', 1, 'Good', 'Purchased', '2024-03-15', '2026-03-15', 1, 0, NULL, NULL, 3, 1),
        (2, 'Wireless Mouse', 'Ergonomic wireless mouse with USB receiver', 29.99, 18.00, 'Logitech', 'MX Master 3', 'LG-MX3-002', 'Best Buy', 1, 'LikeNew', 'Purchased', '2024-06-01', '2026-06-01', 1, 0, NULL, 4, NULL, 1),
        (3, 'USB-C Cable', '2-meter USB-C charging cable', 12.99, 10.00, 'Anker', 'PowerLine III', 'AK-PL3-003', 'Amazon', 2, 'New', 'Purchased', '2025-01-10', NULL, 1, 0, NULL, 1, NULL, 2),
        (4, 'Monitor Stand', 'Adjustable monitor stand for dual displays', 49.99, 35.00, 'VIVO', 'STAND-V001', 'VV-S001-004', 'Amazon', 1, 'Good', 'Purchased', '2023-11-20', NULL, 1, 0, NULL, 3, NULL, 3),
        (5, 'Mechanical Keyboard', 'RGB mechanical keyboard with Cherry MX switches', 149.99, 110.00, 'Keychron', 'K8', 'KC-K8-005', 'Keychron.com', 1, 'Good', 'Gift', '2024-12-25', '2025-12-25', 1, 0, NULL, 5, NULL, 4)
) AS source (Id, Name, Description, PurchasePrice, CurrentValue, Brand, Model, SerialNumber, PurchasedFrom, Quantity, Condition, AcquisitionType, PurchaseDate, WarrantyExpiryDate, IsStored, IsDeleted, ReasonForDeletion, RoomId, ContainerId, OwnerId)
ON target.Id = source.Id
WHEN MATCHED THEN
    UPDATE SET
        Name = source.Name,
        Description = source.Description,
        PurchasePrice = source.PurchasePrice,
        CurrentValue = source.CurrentValue,
        Brand = source.Brand,
        Model = source.Model,
        SerialNumber = source.SerialNumber,
        PurchasedFrom = source.PurchasedFrom,
        Quantity = source.Quantity,
        Condition = source.Condition,
        AcquisitionType = source.AcquisitionType,
        PurchaseDate = source.PurchaseDate,
        WarrantyExpiryDate = source.WarrantyExpiryDate,
        IsStored = source.IsStored,
        IsDeleted = source.IsDeleted,
        ReasonForDeletion = source.ReasonForDeletion,
        RoomId = source.RoomId,
        ContainerId = source.ContainerId,
        OwnerId = source.OwnerId
WHEN NOT MATCHED THEN
    INSERT (Id, Name, Description, PurchasePrice, CurrentValue, Brand, Model, SerialNumber, PurchasedFrom, Quantity, Condition, AcquisitionType, PurchaseDate, WarrantyExpiryDate, IsStored, IsDeleted, ReasonForDeletion, RoomId, ContainerId, OwnerId)
    VALUES (source.Id, source.Name, source.Description, source.PurchasePrice, source.CurrentValue, source.Brand, source.Model, source.SerialNumber, source.PurchasedFrom, source.Quantity, source.Condition, source.AcquisitionType, source.PurchaseDate, source.WarrantyExpiryDate, source.IsStored, source.IsDeleted, source.ReasonForDeletion, source.RoomId, source.ContainerId, source.OwnerId);

SET IDENTITY_INSERT dbo.Item OFF;
