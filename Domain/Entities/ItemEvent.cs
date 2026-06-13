using Domain.Enums;

namespace Domain.Entities;

public class ItemEvent
{
    public int Id { get; set; }

    public int ItemId { get; set; }

    public Item? Item { get; set; }

    public ItemEventType EventType { get; set; }

    public DateTime OccurredAt { get; set; }

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    public string? Notes { get; set; }
}
