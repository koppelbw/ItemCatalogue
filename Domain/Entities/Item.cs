using Domain.Enums;

namespace Domain.Entities;

public class Item : IEntity, IAuditable
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<ItemType> ItemTypes { get; set; } = [];

    public decimal? Price { get; set; }

    public bool IsStored { get; set; }
    public bool IsDeleted { get; set; }
    public DeletedReason? ReasonForDeletion { get; set; }




    // An item lives either directly in a Room or inside a Container (at most one of the two).
    // Its location is derived through that link (Item -> Room/Container -> ... -> Location).
    public int? RoomId { get; set; }

    public Room? Room { get; set; }

    public int? ContainerId { get; set; }

    public Container? Container { get; set; }

    public int? OwnerId { get; set; }

    public Person? Owner { get; set; }

    
    public DateTime CreatedDate { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    // Optimistic concurrency token. SQL Server maintains this rowversion automatically,
    // incrementing it on every UPDATE. EF Core uses it in the WHERE clause so a stale
    // write (one based on an out-of-date copy) affects 0 rows and raises a conflict
    // instead of silently overwriting a concurrent edit.
    public byte[] RowVersion { get; set; } = [];
}
