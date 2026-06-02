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
        Price = request.Price,
        IsStored = request.IsStored,
        LocationId = request.LocationId,
        OwnerId = request.OwnerId,
    };

    public static void ApplyTo(this UpdateItemRequest request, Item item)
    {
        item.Name = request.Name;
        item.Description = request.Description;
        item.ItemTypes = request.ItemTypes;
        item.Price = request.Price;
        item.IsStored = request.IsStored;
        item.LocationId = request.LocationId;
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
        item.Price,
        item.IsStored,
        item.IsDeleted,
        item.ReasonForDeletion,
        item.LocationId,
        item.OwnerId,
        item.CreatedDate,
        item.LastModifiedDate,
        item.RowVersion);
}
