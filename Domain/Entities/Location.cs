namespace Domain.Entities;

public class Location
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }


    public int RoomId { get; set; }

    public Room? Room { get; set; }
}
