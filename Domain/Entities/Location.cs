namespace Domain.Entities;

public class Location : IEntity
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }


    public int RoomId { get; set; }

    public Room? Room { get; set; }

    // Optimistic concurrency token maintained by SQL Server. See Item.RowVersion.
    public byte[] RowVersion { get; set; } = [];
}
