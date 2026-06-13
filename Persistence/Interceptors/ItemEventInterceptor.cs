using Domain.Entities;
using Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using System.Globalization;

namespace Persistence.Interceptors;

// Detects meaningful field changes on tracked Item entities and appends ItemEvent rows
// within the same SaveChanges transaction. Covers all tracked writes; the soft-delete path
// (ExecuteUpdateAsync) bypasses the change tracker and is handled explicitly in ItemService.
public sealed class ItemEventInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        AppendEvents(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        AppendEvents(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void AppendEvents(DbContext? context)
    {
        if (context is null)
            return;

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var events = new List<ItemEvent>();

        foreach (var entry in context.ChangeTracker.Entries<Item>())
        {
            if (entry.State == EntityState.Added)
            {
                // Set Item nav prop rather than ItemId — EF resolves the FK after the identity
                // is assigned, so inserting in principal-first order works within one SaveChanges.
                events.Add(new ItemEvent { Item = entry.Entity, EventType = ItemEventType.Created, OccurredAt = now });
                continue;
            }

            if (entry.State != EntityState.Modified)
                continue;

            var itemId = entry.Entity.Id;

            if (entry.Property(nameof(Item.RoomId)).IsModified || entry.Property(nameof(Item.ContainerId)).IsModified)
            {
                events.Add(new ItemEvent
                {
                    ItemId = itemId,
                    EventType = ItemEventType.Moved,
                    OccurredAt = now,
                    OldValue = FormatLocation(
                        entry.Property(nameof(Item.RoomId)).OriginalValue,
                        entry.Property(nameof(Item.ContainerId)).OriginalValue),
                    NewValue = FormatLocation(
                        entry.Property(nameof(Item.RoomId)).CurrentValue,
                        entry.Property(nameof(Item.ContainerId)).CurrentValue),
                });
            }

            if (entry.Property(nameof(Item.CurrentValue)).IsModified)
            {
                events.Add(new ItemEvent
                {
                    ItemId = itemId,
                    EventType = ItemEventType.ValueChanged,
                    OccurredAt = now,
                    OldValue = FormatDecimal(entry.Property(nameof(Item.CurrentValue)).OriginalValue),
                    NewValue = FormatDecimal(entry.Property(nameof(Item.CurrentValue)).CurrentValue),
                });
            }

            if (entry.Property(nameof(Item.Condition)).IsModified)
            {
                events.Add(new ItemEvent
                {
                    ItemId = itemId,
                    EventType = ItemEventType.ConditionChanged,
                    OccurredAt = now,
                    OldValue = entry.Property(nameof(Item.Condition)).OriginalValue?.ToString(),
                    NewValue = entry.Property(nameof(Item.Condition)).CurrentValue?.ToString(),
                });
            }

            if (entry.Property(nameof(Item.OwnerId)).IsModified)
            {
                events.Add(new ItemEvent
                {
                    ItemId = itemId,
                    EventType = ItemEventType.OwnerChanged,
                    OccurredAt = now,
                    OldValue = FormatPersonId(entry.Property(nameof(Item.OwnerId)).OriginalValue),
                    NewValue = FormatPersonId(entry.Property(nameof(Item.OwnerId)).CurrentValue),
                });
            }
        }

        if (events.Count > 0)
            context.Set<ItemEvent>().AddRange(events);
    }

    private static string FormatLocation(object? roomId, object? containerId)
    {
        if (containerId is not null) return $"Container:{containerId}";
        if (roomId is not null) return $"Room:{roomId}";
        return "None";
    }

    private static string? FormatDecimal(object? value) =>
        // "0.####################" strips trailing zeros: 60.00 → "60", 60.50 → "60.5"
        value is null ? null : Convert.ToDecimal(value).ToString("0.####################", CultureInfo.InvariantCulture);

    private static string? FormatPersonId(object? value) =>
        value is null ? null : $"Person:{value}";
}
