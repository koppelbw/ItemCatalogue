using Domain.Enums;

namespace Domain.Querying;

public sealed record ItemFilter(
    string? Query = null,
    int? RoomId = null,
    int? ContainerId = null,
    int? TagId = null,
    int? OwnerId = null,
    decimal? MinValue = null,
    decimal? MaxValue = null,
    Condition? Condition = null,
    bool? IsStored = null,
    bool IncludeDeleted = false
);
