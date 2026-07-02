namespace Domain.Entities;

// A picture belongs to exactly one owner: a Location, Room, Container, or Item. Exactly one of the
// four owner FKs below is set (enforced by CK_Picture_ExactlyOneOwner and by the request
// validators). The binary bytes live in Azure Blob Storage, keyed by BlobName; this row is only
// the metadata (see Application/StoragePorts/IImageStorage.cs).
public class Picture : IEntity, IAuditable
{
    public int Id { get; set; }

    public string BlobName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public string? OriginalFileName { get; set; }

    public string? Caption { get; set; }

    public bool IsPrimary { get; set; }

    public int SortOrder { get; set; }

    public int? WidthPixels { get; set; }

    public int? HeightPixels { get; set; }

    public int? LocationId { get; set; }

    public Location? Location { get; set; }

    public int? RoomId { get; set; }

    public Room? Room { get; set; }

    public int? ContainerId { get; set; }

    public Container? Container { get; set; }

    public int? ItemId { get; set; }

    public Item? Item { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
