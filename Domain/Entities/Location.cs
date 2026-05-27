namespace Domain.Entities;

public class Location
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }  = string.Empty;


    public required Room? Room { get; set; }
}
