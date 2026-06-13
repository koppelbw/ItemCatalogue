namespace Domain.Entities;

public class Room : IEntity, IAuditable
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }


    // Required FK to the owning Location. A Room belongs to exactly one Location.
    public int LocationId { get; set; }

    public Location? Location { get; set; }

    // Top-level Containers that sit directly in this Room (nested containers reference a parent
    // container instead). The owning FK lives on Container.RoomId.
    public List<Container> Containers { get; set; } = [];

    public DateTime CreatedDate { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    // Optimistic concurrency token maintained by SQL Server. See Item.RowVersion.
    public byte[] RowVersion { get; set; } = [];
}
