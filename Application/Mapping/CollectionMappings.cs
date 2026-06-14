using Application.DTOs;
using Domain.Entities;

namespace Application.Mapping;

public static class CollectionMappings
{
    public static Collection ToEntity(this CreateCollectionRequest request) => new()
    {
        Name = request.Name,
        Description = request.Description,
    };

    public static void ApplyTo(this UpdateCollectionRequest request, Collection collection)
    {
        collection.Name = request.Name;
        collection.Description = request.Description;
        collection.RowVersion = request.RowVersion;
    }

    public static CollectionResponse ToResponse(this Collection collection) => new(
        collection.Id,
        collection.Name,
        collection.Description,
        collection.Items
            .OrderBy(ci => ci.SortOrder)
            .Select(ci => ci.ToResponse())
            .ToList(),
        collection.RowVersion);

    public static CollectionItemResponse ToResponse(this CollectionItem membership) => new(
        membership.ItemId,
        membership.Item?.Name ?? string.Empty,
        membership.Quantity,
        membership.SortOrder,
        membership.Role);

    public static CollectionItem ToEntity(this AddCollectionItemRequest request, int collectionId) => new()
    {
        CollectionId = collectionId,
        ItemId = request.ItemId,
        Quantity = request.Quantity,
        SortOrder = request.SortOrder ?? 0,
        Role = request.Role,
    };
}
