using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using ItemCatalogueAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ItemCatalogueAPI.Tests;

// Drives the /api/locations endpoints. Location owns the one-to-many to Room (a Location has many
// Rooms), so it exercises two behaviours specific to the new model: GetById embeds the child Rooms,
// and Delete is FK-restricted while any Room still references the Location (409 Resource in use).
public class LocationApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    private async Task<LocationResponse> CreateLocationAsync(string name = "House", string? description = null)
    {
        var response = await Client.PostAsJsonAsync("/api/locations", new CreateLocationRequest(name, description));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<LocationResponse>())!;
    }

    private async Task<RoomResponse> CreateRoomAsync(int locationId, string name = "Garage")
    {
        var response = await Client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest(name, null, locationId));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<RoomResponse>())!;
    }

    [Fact]
    public async Task Create_WithValidBody_Returns201WithEmptyRooms()
    {
        var response = await Client.PostAsJsonAsync("/api/locations", new CreateLocationRequest("House", "My house"));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<LocationResponse>();
        created.ShouldNotBeNull();
        created.Id.ShouldBeGreaterThan(0);
        created.Name.ShouldBe("House");
        created.Rooms.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetById_EmbedsItsRooms()
    {
        var location = await CreateLocationAsync();
        await CreateRoomAsync(location.Id, "Garage");
        await CreateRoomAsync(location.Id, "Kitchen");

        var fetched = await Client.GetFromJsonAsync<LocationResponse>($"/api/locations/{location.Id}");

        fetched.ShouldNotBeNull();
        fetched.Rooms.Count.ShouldBe(2);
        fetched.Rooms.Select(r => r.Name).ShouldBe(["Garage", "Kitchen"], ignoreOrder: true);
        fetched.Rooms.ShouldAllBe(r => r.LocationId == location.Id);
    }

    [Fact]
    public async Task Delete_WhileReferencedByRoom_Returns409InUse()
    {
        var location = await CreateLocationAsync();
        await CreateRoomAsync(location.Id);

        var response = await Client.DeleteAsync($"/api/locations/{location.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe("Resource in use");
    }

    [Fact]
    public async Task Delete_WhenNoRooms_Returns204()
    {
        var location = await CreateLocationAsync();

        var response = await Client.DeleteAsync($"/api/locations/{location.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Client.GetAsync($"/api/locations/{location.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
