using Domain.Enums;

namespace Domain.Entities;

public class Door : IEntity, IAuditable
{
    public int Id { get; set; }

    public string? Name { get; set; }

    public DoorKind Kind { get; set; }

    public int FromRoomId { get; set; }

    public Room? FromRoom { get; set; }

    public int? ToRoomId { get; set; }

    public Room? ToRoom { get; set; }

    public Wall Wall { get; set; }

    public decimal OffsetInches { get; set; }

    public decimal WidthInches { get; set; }

    public decimal HeightInches { get; set; }

    public HingeSide? HingeSide { get; set; }

    public Swing? Swing { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
