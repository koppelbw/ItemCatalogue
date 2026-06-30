using Domain.Enums;

namespace Domain.Entities;

public class Container : IEntity, IAuditable
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int? RoomId { get; set; }

    public Room? Room { get; set; }

    public int? ParentContainerId { get; set; }

    public Container? ParentContainer { get; set; }

    // Containers nested directly inside this one.
    public List<Container> Children { get; set; } = [];

    public ContainerType? ContainerType { get; set; }

    public decimal? PositionXInches { get; set; }

    public decimal? PositionYInches { get; set; }

    public decimal? PositionZInches { get; set; }

    public decimal? Rotation { get; set; }

    public decimal? WidthInches { get; set; }

    public decimal? DepthInches { get; set; }

    public decimal? HeightInches { get; set; }

    public string? Color { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    // Optimistic concurrency token maintained by SQL Server. See Item.RowVersion.
    public byte[] RowVersion { get; set; } = [];
}
