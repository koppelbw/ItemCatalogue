using Domain.Enums;

namespace Domain.Entities;

public class Room : IEntity, IAuditable
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int FloorId { get; set; }

    public Floor? Floor { get; set; }

    public RoomType? RoomType { get; set; }

    public decimal? OriginXInches { get; set; }

    public decimal? OriginYInches { get; set; }

    public decimal? WidthInches { get; set; }

    public decimal? DepthInches { get; set; }

    public decimal? HeightInches { get; set; }

    public decimal? Rotation { get; set; }

    public string? WallColor { get; set; }

    public string? FloorColor { get; set; }

    public string? CeilingColor { get; set; }

    // Top-level Containers that sit directly in this Room (nested containers reference a parent
    // container instead). The owning FK lives on Container.RoomId.
    public List<Container> Containers { get; set; } = [];

    public List<Door> Doors { get; set; } = [];

    public List<Stair> Stairs { get; set; } = [];

    public DateTime CreatedDate { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    // Optimistic concurrency token maintained by SQL Server. See Item.RowVersion.
    public byte[] RowVersion { get; set; } = [];
}
