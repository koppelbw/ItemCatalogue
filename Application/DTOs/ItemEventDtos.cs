namespace Application.DTOs;

public record ItemEventResponse(
    int Id,
    int ItemId,
    string EventType,
    DateTime OccurredAt,
    string? OldValue,
    string? NewValue,
    string? Notes);
