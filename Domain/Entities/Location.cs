namespace Domain.Entities;

public class Location : IEntity, IAuditable
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }


    // A Location holds many Rooms (one-to-many). The owning FK lives on Room.LocationId.
    public List<Room> Rooms { get; set; } = [];

    public DateTime CreatedDate { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    // Optimistic concurrency token maintained by SQL Server. See Item.RowVersion.
    public byte[] RowVersion { get; set; } = [];
}
