using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using Domain.Enums;
using ItemCatalogueAPI.Tests.Infrastructure;
using Shouldly;

namespace ItemCatalogueAPI.Tests;

// Drives the /api/items endpoints. Item adds behaviour the other controllers don't have: the JSON
// ItemTypes collection over the wire and a soft delete keyed by a DeletedReason query parameter.
public class ItemApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    private static CreateItemRequest ValidItem() =>
        new("Desk Lamp", "A small lamp", [ItemType.Electronics, ItemType.Books], 19.99m, false, null, null);

    private async Task<ItemResponse> CreateItemAsync()
    {
        var response = await Client.PostAsJsonAsync("/api/items", ValidItem());
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ItemResponse>())!;
    }

    [Fact]
    public async Task Create_WithValidBody_Returns201AndRoundTripsItemTypes()
    {
        var response = await Client.PostAsJsonAsync("/api/items", ValidItem());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ItemResponse>();
        created.ShouldNotBeNull();
        created.Id.ShouldBeGreaterThan(0);
        created.ItemTypes.ShouldBe([ItemType.Electronics, ItemType.Books]);
        created.Price.ShouldBe(19.99m);
        created.IsDeleted.ShouldBeFalse();
    }

    [Fact]
    public async Task Create_WithEmptyItemTypes_Returns400()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/items",
            ValidItem() with { ItemTypes = [] });

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("ItemTypes");
    }

    [Fact]
    public async Task GetById_AfterCreate_ReturnsItemWithTypes()
    {
        var created = await CreateItemAsync();

        var item = await Client.GetFromJsonAsync<ItemResponse>($"/api/items/{created.Id}");

        item.ShouldNotBeNull();
        item.Name.ShouldBe("Desk Lamp");
        item.ItemTypes.ShouldBe([ItemType.Electronics, ItemType.Books]);
    }

    [Fact]
    public async Task Delete_WithReason_Returns204AndMarksItemDeleted()
    {
        var created = await CreateItemAsync();

        var response = await Client.DeleteAsync($"/api/items/{created.Id}?reason={DeletedReason.Broken}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Soft delete flags the row rather than removing it: it is still retrievable, now marked deleted.
        var afterDelete = await Client.GetFromJsonAsync<ItemResponse>($"/api/items/{created.Id}");
        afterDelete!.IsDeleted.ShouldBeTrue();
        afterDelete.ReasonForDeletion.ShouldBe(DeletedReason.Broken);
    }
}
