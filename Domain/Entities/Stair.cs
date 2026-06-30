using Domain.Enums;

namespace Domain.Entities;

public class Stair : IEntity, IAuditable
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public int FromRoomId { get; set; }

    public Room? FromRoom { get; set; }

    public int? ToRoomId { get; set; }

    public Room? ToRoom { get; set; }

    public StairShape Shape { get; set; }

    public decimal? PositionXInches { get; set; }

    public decimal? PositionYInches { get; set; }

    public decimal? Rotation { get; set; }

    public decimal? RunInches { get; set; }

    public decimal? WidthInches { get; set; }

    public decimal? RiseInches { get; set; }

    public int? StepCount { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
