namespace Domain.Entities;

// A curated, named set of specific items that belong together logically but not physically
// ("Catan + expansions" stored in different places). Collections answer "what goes together?" and
// are distinct from Container (physical nesting). Collection <-> Item is a *rich* many-to-many: the
// membership itself carries data (Quantity, SortOrder, Role), modelled by the CollectionItem join
// entity. A Collection is a full aggregate with its own identity and concurrency token.
public class Collection : IEntity, IAuditable
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // The membership rows for this collection (each links an Item and carries the rich-join payload).
    public List<CollectionItem> Items { get; set; } = [];

    public DateTime CreatedDate { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    // Optimistic concurrency token maintained by SQL Server. See Item.RowVersion.
    public byte[] RowVersion { get; set; } = [];
}
