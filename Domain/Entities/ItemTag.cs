namespace Domain.Entities;

// Join entity for the Item <-> Tag many-to-many. This is a *plain* join: it carries no payload of
// its own, only the two foreign keys that form its composite key (ItemId, TagId). It is deliberately
// not an IEntity/IAuditable aggregate (no surrogate Id, no RowVersion) — it is maintained through the
// Item.Tags skip-navigation, not via its own repository.
public class ItemTag
{
    public int ItemId { get; set; }

    public Item? Item { get; set; }

    public int TagId { get; set; }

    public Tag? Tag { get; set; }
}
