namespace Domain.Entities;

// Join entity for the Collection <-> Item many-to-many. Unlike ItemTag this is a *rich* join: the
// membership carries its own data describing how the item participates in the collection. Its
// composite key is (CollectionId, ItemId). It is not an IEntity/IAuditable aggregate; it is managed
// through the owning Collection.
public class CollectionItem
{
    public int CollectionId { get; set; }

    public Collection? Collection { get; set; }

    public int ItemId { get; set; }

    public Item? Item { get; set; }

    public int Quantity { get; set; } = 1;

    public int SortOrder { get; set; }

    public string? Role { get; set; }
}
