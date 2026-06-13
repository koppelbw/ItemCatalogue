using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using Domain.Enums;
using ItemCatalogueAPI.Tests.Infrastructure;
using Shouldly;

namespace ItemCatalogueAPI.Tests;

public class ItemEventApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    // 21 params matching CreateItemRequest: Name,Desc,ItemTypes,PurchasePrice,CurrentValue,
    // Brand,Model,Serial,PurchasedFrom,Quantity,Condition,AcquisitionType,PurchaseDate,
    // WarrantyExpiry,IsStored,RoomId,ContainerId,OwnerId,ReleaseDate,ValuationDate,AcquisitionRef
    private static CreateItemRequest BaseItem() =>
        new("Bookshelf", null, [ItemType.Books], 80m, 60m,
            null, null, null, null, 1, Condition.Good, null,
            null, null, false, null, null, null, null, null, null);

    private async Task<ItemResponse> CreateItemAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/items", BaseItem());
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ItemResponse>())!;
    }

    private async Task<List<ItemEventResponse>> GetEventsAsync(int itemId)
    {
        var response = await Client.GetFromJsonAsync<List<ItemEventResponse>>($"/api/items/{itemId}/events");
        return response!;
    }

    // Builds an UpdateItemRequest mirroring the current item state, overriding CurrentValue.
    private static UpdateItemRequest PutRequest(ItemResponse item, decimal? currentValue = null) =>
        new(item.Id, item.Name, item.Description, item.ItemTypes,
            item.PurchasePrice, currentValue ?? item.CurrentValue,
            item.Brand, item.Model, item.SerialNumber, item.PurchasedFrom,
            item.Quantity, item.Condition, item.AcquisitionType,
            item.PurchaseDate, item.WarrantyExpiryDate, item.IsStored,
            item.RoomId, item.ContainerId, item.OwnerId,
            item.ReleaseDate, item.ValuationDate, item.AcquisitionReference,
            item.RowVersion);

    [Fact]
    public async Task GetEvents_AfterCreate_ReturnsCreatedEvent()
    {
        var item = await CreateItemAsync();

        var events = await GetEventsAsync(item.Id);

        events.ShouldContain(e => e.EventType == nameof(ItemEventType.Created));
    }

    [Fact]
    public async Task GetEvents_AfterValueUpdate_ReturnsValueChangedEvent()
    {
        var item = await CreateItemAsync();

        var put = await Client.PutAsJsonAsync($"/api/items/{item.Id}", PutRequest(item, currentValue: 120m));
        put.EnsureSuccessStatusCode();

        var events = await GetEventsAsync(item.Id);

        events.ShouldContain(e => e.EventType == nameof(ItemEventType.ValueChanged));
        var ev = events.First(e => e.EventType == nameof(ItemEventType.ValueChanged));
        ev.OldValue.ShouldBe("60");
        ev.NewValue.ShouldBe("120");
    }

    [Fact]
    public async Task GetEvents_AfterDelete_ReturnsSoftDeletedEvent()
    {
        var item = await CreateItemAsync();

        var del = await Client.DeleteAsync($"/api/items/{item.Id}?reason={DeletedReason.Lost}");
        del.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        var events = await GetEventsAsync(item.Id);

        events.ShouldContain(e => e.EventType == nameof(ItemEventType.SoftDeleted));
        var ev = events.First(e => e.EventType == nameof(ItemEventType.SoftDeleted));
        ev.Notes.ShouldBe(DeletedReason.Lost.ToString());
    }

    [Fact]
    public async Task GetEvents_ReturnsNewestFirst()
    {
        var item = await CreateItemAsync();

        // First update
        var put1 = await Client.PutAsJsonAsync($"/api/items/{item.Id}", PutRequest(item, currentValue: 70m));
        put1.EnsureSuccessStatusCode();
        var after1 = (await put1.Content.ReadFromJsonAsync<ItemResponse>())!;

        // Second update — needs the refreshed RowVersion from the first response
        await Client.PutAsJsonAsync($"/api/items/{item.Id}", PutRequest(after1, currentValue: 80m));

        var events = await GetEventsAsync(item.Id);

        for (var i = 0; i < events.Count - 1; i++)
            events[i].OccurredAt.ShouldBeGreaterThanOrEqualTo(events[i + 1].OccurredAt);
    }

    [Fact]
    public async Task GetEvents_ForNonExistentItem_ReturnsEmptyList()
    {
        var events = await GetEventsAsync(999_999);

        events.ShouldBeEmpty();
    }
}
