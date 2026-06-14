using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using Domain.Enums;
using ItemCatalogueAPI.Tests.Infrastructure;
using Shouldly;

namespace ItemCatalogueAPI.Tests;

// Drives /api/collections plus the membership sub-resource /api/collections/{id}/items: building a
// collection, adding ordered members with the rich-join payload, updating and removing them, and the
// 404/400 edge cases (unknown item, duplicate membership).
public class CollectionApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    private async Task<CollectionResponse> CreateCollectionAsync(string name = "Office Kit")
    {
        var response = await Client.PostAsJsonAsync("/api/collections", new CreateCollectionRequest(name, "desc"));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<CollectionResponse>())!;
    }

    private async Task<ItemResponse> CreateItemAsync(string name)
    {
        var request = new CreateItemRequest(name, null, [ItemType.Electronics], null, null, null, null, null, null, 1,
            null, null, null, null, false, null, null, null, null, null, null);
        var response = await Client.PostAsJsonAsync("/api/items", request);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ItemResponse>())!;
    }

    [Fact]
    public async Task Create_Returns201WithEmptyMembers()
    {
        var response = await Client.PostAsJsonAsync("/api/collections", new CreateCollectionRequest("Office Kit", "Desk setup"));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<CollectionResponse>();
        created!.Id.ShouldBeGreaterThan(0);
        created.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task Create_WithDuplicateName_Returns409()
    {
        await CreateCollectionAsync("Office Kit");

        var response = await Client.PostAsJsonAsync("/api/collections", new CreateCollectionRequest("Office Kit", null));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task AddItems_thenGetById_ReturnsMembersOrderedBySortOrder()
    {
        var collection = await CreateCollectionAsync();
        var laptop = await CreateItemAsync("Laptop");
        var mouse = await CreateItemAsync("Mouse");

        // Add out of order; the response should come back ordered by SortOrder.
        (await Client.PostAsJsonAsync($"/api/collections/{collection.Id}/items",
            new AddCollectionItemRequest(mouse.Id, 1, 2, "Accessory"))).StatusCode.ShouldBe(HttpStatusCode.OK);
        (await Client.PostAsJsonAsync($"/api/collections/{collection.Id}/items",
            new AddCollectionItemRequest(laptop.Id, 1, 1, "Primary"))).StatusCode.ShouldBe(HttpStatusCode.OK);

        var fetched = await Client.GetFromJsonAsync<CollectionResponse>($"/api/collections/{collection.Id}");
        fetched!.Items.Select(i => i.ItemName).ShouldBe(["Laptop", "Mouse"]);
        fetched.Items.First().Role.ShouldBe("Primary");
    }

    [Fact]
    public async Task AddItem_WhenItemMissing_Returns404()
    {
        var collection = await CreateCollectionAsync();

        var response = await Client.PostAsJsonAsync($"/api/collections/{collection.Id}/items",
            new AddCollectionItemRequest(999_999, 1, null, null));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task AddItem_Duplicate_Returns400()
    {
        var collection = await CreateCollectionAsync();
        var laptop = await CreateItemAsync("Laptop");
        await Client.PostAsJsonAsync($"/api/collections/{collection.Id}/items", new AddCollectionItemRequest(laptop.Id, 1, null, null));

        var duplicate = await Client.PostAsJsonAsync($"/api/collections/{collection.Id}/items", new AddCollectionItemRequest(laptop.Id, 1, null, null));

        duplicate.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateItem_ChangesPayload()
    {
        var collection = await CreateCollectionAsync();
        var laptop = await CreateItemAsync("Laptop");
        await Client.PostAsJsonAsync($"/api/collections/{collection.Id}/items", new AddCollectionItemRequest(laptop.Id, 1, 0, "Old"));

        var put = await Client.PutAsJsonAsync($"/api/collections/{collection.Id}/items/{laptop.Id}",
            new UpdateCollectionItemRequest(5, 3, "New"));
        put.StatusCode.ShouldBe(HttpStatusCode.OK);

        var member = (await Client.GetFromJsonAsync<CollectionResponse>($"/api/collections/{collection.Id}"))!.Items.Single();
        member.Quantity.ShouldBe(5);
        member.SortOrder.ShouldBe(3);
        member.Role.ShouldBe("New");
    }

    [Fact]
    public async Task RemoveItem_Returns204AndDropsMember()
    {
        var collection = await CreateCollectionAsync();
        var laptop = await CreateItemAsync("Laptop");
        await Client.PostAsJsonAsync($"/api/collections/{collection.Id}/items", new AddCollectionItemRequest(laptop.Id, 1, null, null));

        var remove = await Client.DeleteAsync($"/api/collections/{collection.Id}/items/{laptop.Id}");
        remove.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        (await Client.GetFromJsonAsync<CollectionResponse>($"/api/collections/{collection.Id}"))!.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task RemoveItem_WhenNotMember_Returns404()
    {
        var collection = await CreateCollectionAsync();
        var laptop = await CreateItemAsync("Laptop");

        var response = await Client.DeleteAsync($"/api/collections/{collection.Id}/items/{laptop.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Returns204ThenGetIs404()
    {
        var collection = await CreateCollectionAsync();

        (await Client.DeleteAsync($"/api/collections/{collection.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Client.GetAsync($"/api/collections/{collection.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
