using Application.DTOs;
using Domain.Entities;

namespace Application.Mapping;

public static class ItemEventMappings
{
    public static ItemEventResponse ToResponse(this ItemEvent e) =>
        new(e.Id, e.ItemId, e.EventType.ToString(), e.OccurredAt, e.OldValue, e.NewValue, e.Notes);
}
