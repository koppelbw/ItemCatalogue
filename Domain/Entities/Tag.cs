namespace Domain.Entities;

// A user-defined, cross-cutting label ("fragile", "camping", "electronics"). Tags answer
// "what is this like?" and are shared across many items, so Item <-> Tag is a many-to-many
// (the ItemTag join). A Tag is a full aggregate: it has its own identity and concurrency token
// and is managed through its own CRUD surface.
public class Tag : IEntity, IAuditable
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<Item> Items { get; set; } = [];

    public DateTime CreatedDate { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    // Optimistic concurrency token maintained by SQL Server. See Item.RowVersion.
    public byte[] RowVersion { get; set; } = [];
}
