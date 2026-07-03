using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using Domain.Enums;
using ItemCatalogueAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ItemCatalogueAPI.Tests;

// Drives /api/tags plus the item-tags sub-resource on /api/items/{id}/tags end to end: creating the
// tag vocabulary and assigning a set of tags to an item over real HTTP.
public class TagApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    private async Task<TagResponse> CreateTagAsync(string name = "Fragile")
    {
        var response = await Client.PostAsJsonAsync("/api/tags", new CreateTagRequest(name, "desc"));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<TagResponse>())!;
    }

    private async Task<ItemResponse> CreateItemAsync(string name = "Laptop")
    {
        var request = new CreateItemRequest(name, null, [ItemType.Electronics], null, null, null, null, null, null, 1,
            null, null, null, null, false, true, null, null, null, null, null, null);
        var response = await Client.PostAsJsonAsync("/api/items", request);
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ItemResponse>())!;
    }

    [Fact]
    public async Task Create_Returns201WithLocation()
    {
        var response = await Client.PostAsJsonAsync("/api/tags", new CreateTagRequest("Fragile", "Handle with care"));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
        var created = await response.Content.ReadFromJsonAsync<TagResponse>();
        created!.Id.ShouldBeGreaterThan(0);
        created.Name.ShouldBe("Fragile");
    }

    [Fact]
    public async Task Create_WithDuplicateName_Returns409()
    {
        await CreateTagAsync("Fragile");

        var response = await Client.PostAsJsonAsync("/api/tags", new CreateTagRequest("Fragile", null));

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe("Duplicate value");
    }

    [Fact]
    public async Task GetById_AfterCreate_Returns200()
    {
        var created = await CreateTagAsync();

        var fetched = await Client.GetFromJsonAsync<TagResponse>($"/api/tags/{created.Id}");

        fetched!.Name.ShouldBe("Fragile");
    }

    [Fact]
    public async Task Delete_Returns204ThenGetIs404()
    {
        var created = await CreateTagAsync();

        (await Client.DeleteAsync($"/api/tags/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Client.GetAsync($"/api/tags/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task SetItemTags_AssignsTags_thenGetItemTagsReturnsThem()
    {
        var item = await CreateItemAsync();
        var fragile = await CreateTagAsync("Fragile");
        var electronics = await CreateTagAsync("Electronics");

        var put = await Client.PutAsJsonAsync($"/api/items/{item.Id}/tags",
            new SetItemTagsRequest([fragile.Id, electronics.Id]));
        put.StatusCode.ShouldBe(HttpStatusCode.OK);
        var afterPut = await put.Content.ReadFromJsonAsync<ItemTagsResponse>();
        afterPut!.Tags.Count.ShouldBe(2);

        var tags = await Client.GetFromJsonAsync<ItemTagsResponse>($"/api/items/{item.Id}/tags");
        tags!.ItemId.ShouldBe(item.Id);
        tags.Tags.Select(t => t.Name).OrderBy(n => n).ShouldBe(["Electronics", "Fragile"]);
    }

    [Fact]
    public async Task SetItemTags_WithEmptyList_ClearsTags()
    {
        var item = await CreateItemAsync();
        var fragile = await CreateTagAsync("Fragile");
        await Client.PutAsJsonAsync($"/api/items/{item.Id}/tags", new SetItemTagsRequest([fragile.Id]));

        var cleared = await Client.PutAsJsonAsync($"/api/items/{item.Id}/tags", new SetItemTagsRequest([]));
        cleared.StatusCode.ShouldBe(HttpStatusCode.OK);

        (await Client.GetFromJsonAsync<ItemTagsResponse>($"/api/items/{item.Id}/tags"))!.Tags.ShouldBeEmpty();
    }

    [Fact]
    public async Task SetItemTags_WithUnknownTag_Returns404()
    {
        var item = await CreateItemAsync();

        var response = await Client.PutAsJsonAsync($"/api/items/{item.Id}/tags", new SetItemTagsRequest([999_999]));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
