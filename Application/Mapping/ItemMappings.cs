using Application.DTOs;
using Domain.Entities;

namespace Application.Mapping;

public static class ItemMappings
{
    public static Item ToEntity(this CreateItemRequest request) => new()
    {
        Name = request.Name,
        Description = request.Description,
        ItemTypes = request.ItemTypes,
        PurchasePrice = request.PurchasePrice,
        CurrentValue = request.CurrentValue,
        Brand = request.Brand,
        Model = request.Model,
        SerialNumber = request.SerialNumber,
        PurchasedFrom = request.PurchasedFrom,
        Quantity = request.Quantity,
        Condition = request.Condition,
        AcquisitionType = request.AcquisitionType,
        PurchaseDate = request.PurchaseDate,
        WarrantyExpiryDate = request.WarrantyExpiryDate,
        IsStored = request.IsStored,
        RoomId = request.RoomId,
        ContainerId = request.ContainerId,
        OwnerId = request.OwnerId,
    };

    public static void ApplyTo(this UpdateItemRequest request, Item item)
    {
        item.Name = request.Name;
        item.Description = request.Description;
        item.ItemTypes = request.ItemTypes;
        item.PurchasePrice = request.PurchasePrice;
        item.CurrentValue = request.CurrentValue;
        item.Brand = request.Brand;
        item.Model = request.Model;
        item.SerialNumber = request.SerialNumber;
        item.PurchasedFrom = request.PurchasedFrom;
        item.Quantity = request.Quantity;
        item.Condition = request.Condition;
        item.AcquisitionType = request.AcquisitionType;
        item.PurchaseDate = request.PurchaseDate;
        item.WarrantyExpiryDate = request.WarrantyExpiryDate;
        item.IsStored = request.IsStored;
        item.RoomId = request.RoomId;
        item.ContainerId = request.ContainerId;
        item.OwnerId = request.OwnerId;
        // Carry the client's concurrency token onto the entity; the repository uses it as
        // the original value so a stale token is detected at save time.
        item.RowVersion = request.RowVersion;
    }

    public static ItemResponse ToResponse(this Item item) => new(
        item.Id,
        item.Name,
        item.Description,
        item.ItemTypes,
        item.PurchasePrice,
        item.CurrentValue,
        item.Brand,
        item.Model,
        item.SerialNumber,
        item.PurchasedFrom,
        item.Quantity,
        item.Condition,
        item.AcquisitionType,
        item.PurchaseDate,
        item.WarrantyExpiryDate,
        item.IsStored,
        item.IsDeleted,
        item.ReasonForDeletion,
        item.RoomId,
        item.ContainerId,
        item.OwnerId,
        item.CreatedDate,
        item.LastModifiedDate,
        item.RowVersion);
}
