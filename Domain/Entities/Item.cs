using Domain.Enums;

namespace Domain.Entities;

public class Item
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public List<ItemType>? ItemTypes { get; set; }

    public decimal? Price { get; set; }

    public bool IsStored { get; set; }
    public bool IsDeleted { get; set; }
    public DeletedReason? ReasonForDeletion { get; set; }




    public Location? Location { get; set; }
    
    public Person? Owner { get; set; }

    
    public DateTime CreatedDate { get; set; }

    public DateTime? LastModifiedDate { get; set; }
}
