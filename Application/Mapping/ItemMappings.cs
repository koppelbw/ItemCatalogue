using Application.DTOs;
using Domain.Entities;
using Domain.Querying;

namespace Application.Mapping;

public static class ItemMappings
{
    public static ItemFilter ToFilter(this ItemSearchQuery query) => new(
        Query: query.Query,
        RoomId: query.RoomId,
        ContainerId: query.ContainerId,
        TagId: query.TagId,
        OwnerId: query.OwnerId,
        MinValue: query.MinValue,
        MaxValue: query.MaxValue,
        Condition: query.Condition,
        IsStored: query.IsStored,
        IncludeDeleted: query.IncludeDeleted);

    public static ItemLocationPathResponse ToLocationPathResponse(this Item item)
    {
        var containers = new List<ContainerPathStep>();
        Room room;

        if (item.RoomId.HasValue)
        {
            room = item.Room!;
        }
        else
        {
            // Traverse up the container chain (innermost → outermost) to find the enclosing room.
            var current = item.Container!;
            while (true)
            {
                containers.Add(new ContainerPathStep(current.Id, current.Name));
                if (current.RoomId.HasValue)
                {
                    room = current.Room!;
                    break;
                }
                current = current.ParentContainer!;
            }
            // Reverse so the path reads outermost → innermost (e.g. [Wardrobe, Top Shelf]).
            containers.Reverse();
        }

        var floor = room.Floor!;
        var location = floor.Location!;

        return new ItemLocationPathResponse(
            ItemId: item.Id,
            ItemName: item.Name,
            LocationId: location.Id,
            LocationName: location.Name,
            FloorId: floor.Id,
            FloorName: floor.Name,
            RoomId: room.Id,
            RoomName: room.Name,
            ContainerPath: containers);
    }

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
        ReleaseDate = request.ReleaseDate,
        ValuationDate = request.ValuationDate,
        AcquisitionReference = request.AcquisitionReference,
        IsStored = request.IsStored,
        IsShownInUI = request.IsShownInUI,
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
        item.ReleaseDate = request.ReleaseDate;
        item.ValuationDate = request.ValuationDate;
        item.AcquisitionReference = request.AcquisitionReference;
        item.IsStored = request.IsStored;
        item.IsShownInUI = request.IsShownInUI;
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
        item.IsShownInUI,
        item.IsDeleted,
        item.ReasonForDeletion,
        item.RoomId,
        item.ContainerId,
        item.OwnerId,
        item.ReleaseDate,
        item.ValuationDate,
        item.AcquisitionReference,
        item.CreatedDate,
        item.LastModifiedDate,
        item.RowVersion);
}
