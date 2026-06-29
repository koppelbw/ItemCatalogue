namespace Domain.Entities;

public class Floor : IEntity, IAuditable
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int LocationId { get; set; }

    public Location? Location { get; set; }

    public int LevelIndex { get; set; }

    public decimal? ElevationInches { get; set; }

    public decimal? CeilingHeightInches { get; set; }

    public List<Room> Rooms { get; set; } = [];

    public DateTime CreatedDate { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
