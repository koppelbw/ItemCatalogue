namespace Domain.Entities;

public class Person : IEntity, IAuditable
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;


    public List<Item>? Items { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    // Optimistic concurrency token maintained by SQL Server. See Item.RowVersion.
    public byte[] RowVersion { get; set; } = [];
}
