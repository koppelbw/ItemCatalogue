using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using ItemCatalogueAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ItemCatalogueAPI.Tests;

public class FloorApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    private async Task<int> CreateLocationIdAsync(string name = "House")
    {
        var response = await Client.PostAsJsonAsync("/api/locations", new CreateLocationRequest(name, null));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<LocationResponse>())!.Id;
    }

    private async Task<FloorResponse> CreateFloorAsync(string name = "First Floor", int levelIndex = 0, int? locationId = null)
    {
        locationId ??= await CreateLocationIdAsync();
        var response = await Client.PostAsJsonAsync("/api/floors", new CreateFloorRequest(name, locationId.Value, levelIndex, null, null));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<FloorResponse>())!;
    }

    [Fact]
    public async Task Create_WithValidBody_Returns201WithLocationHeaderAndBody()
    {
        var locationId = await CreateLocationIdAsync();
        var response = await Client.PostAsJsonAsync("/api/floors", new CreateFloorRequest("Basement", locationId, -1, -96, 84));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();

        var created = await response.Content.ReadFromJsonAsync<FloorResponse>();
        created.ShouldNotBeNull();
        created.Id.ShouldBeGreaterThan(0);
        created.Name.ShouldBe("Basement");
        created.LevelIndex.ShouldBe(-1);
        created.RowVersion.ShouldNotBeEmpty();

        var followUp = await Client.GetAsync(response.Headers.Location);
        followUp.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithEmptyName_Returns400ValidationProblem()
    {
        var response = await Client.PostAsJsonAsync("/api/floors", new CreateFloorRequest("", 1, 0, null, null));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Create_DuplicateLevelForSameLocation_Returns409()
    {
        var locationId = await CreateLocationIdAsync();
        await CreateFloorAsync("First Floor", 0, locationId);

        // Same (LocationId, LevelIndex) violates the unique index -> 409 Conflict.
        var dup = await Client.PostAsJsonAsync("/api/floors", new CreateFloorRequest("Repeat", locationId, 0, null, null));

        dup.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task GetById_WhenMissing_Returns404ProblemDetails()
    {
        var response = await Client.GetAsync("/api/floors/999999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe("Resource not found");
    }

    [Fact]
    public async Task Update_WithStaleRowVersion_Returns409Concurrency()
    {
        var created = await CreateFloorAsync();

        var first = await Client.PutAsJsonAsync(
            $"/api/floors/{created.Id}",
            new UpdateFloorRequest(created.Id, "Ground", created.LocationId, created.LevelIndex, null, null, created.RowVersion));
        first.StatusCode.ShouldBe(HttpStatusCode.OK);

        var second = await Client.PutAsJsonAsync(
            $"/api/floors/{created.Id}",
            new UpdateFloorRequest(created.Id, "Main", created.LocationId, created.LevelIndex, null, null, created.RowVersion));

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe("Concurrency conflict");
    }

    [Fact]
    public async Task Delete_WhileReferencedByRoom_Returns409InUse()
    {
        var floor = await CreateFloorAsync();
        var room = await Client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest("Bedroom", null, floor.Id));
        room.StatusCode.ShouldBe(HttpStatusCode.Created);

        var response = await Client.DeleteAsync($"/api/floors/{floor.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe("Resource in use");
    }

    [Fact]
    public async Task Delete_WhenNoRooms_Returns204()
    {
        var floor = await CreateFloorAsync();

        var response = await Client.DeleteAsync($"/api/floors/{floor.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Client.GetAsync($"/api/floors/{floor.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
