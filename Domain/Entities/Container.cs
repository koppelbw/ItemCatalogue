namespace Domain.Entities;

public class Container : IEntity, IAuditable
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }


    // A Container is owned by exactly one parent: either a Room (top-level) via RoomId, or another
    // Container (nested) via ParentContainerId. Exactly one of the two is set (enforced by the
    // request validators and the CK_Container_RoomXorParent check constraint in the database).
    public int? RoomId { get; set; }

    public Room? Room { get; set; }

    public int? ParentContainerId { get; set; }

    public Container? ParentContainer { get; set; }

    // Containers nested directly inside this one.
    public List<Container> Children { get; set; } = [];

    public DateTime CreatedDate { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    // Optimistic concurrency token maintained by SQL Server. See Item.RowVersion.
    public byte[] RowVersion { get; set; } = [];
}
