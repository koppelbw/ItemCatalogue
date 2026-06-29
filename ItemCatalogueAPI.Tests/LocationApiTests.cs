using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using Domain.Enums;
using ItemCatalogueAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ItemCatalogueAPI.Tests;

public class LocationApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    private async Task<LocationResponse> CreateLocationAsync(string name = "House", string? description = null)
    {
        var response = await Client.PostAsJsonAsync("/api/locations", new CreateLocationRequest(name, description));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<LocationResponse>())!;
    }

    private async Task<FloorResponse> CreateFloorAsync(int locationId, string name = "Main", int levelIndex = 0)
    {
        var response = await Client.PostAsJsonAsync("/api/floors", new CreateFloorRequest(name, locationId, levelIndex, null, null));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<FloorResponse>())!;
    }

    private async Task<RoomResponse> CreateRoomAsync(int floorId, string name = "Garage")
    {
        var response = await Client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest(name, null, floorId));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<RoomResponse>())!;
    }

    [Fact]
    public async Task Create_WithValidBody_Returns201WithEmptyFloors()
    {
        var response = await Client.PostAsJsonAsync("/api/locations", new CreateLocationRequest("House", "My house"));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<LocationResponse>();
        created.ShouldNotBeNull();
        created.Id.ShouldBeGreaterThan(0);
        created.Name.ShouldBe("House");
        created.Floors.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetById_EmbedsItsFloors()
    {
        var location = await CreateLocationAsync();
        await CreateFloorAsync(location.Id, "Basement", -1);
        await CreateFloorAsync(location.Id, "First Floor", 0);

        var fetched = await Client.GetFromJsonAsync<LocationResponse>($"/api/locations/{location.Id}");

        fetched.ShouldNotBeNull();
        fetched.Floors.Count.ShouldBe(2);
        fetched.Floors.Select(f => f.Name).ShouldBe(["Basement", "First Floor"], ignoreOrder: true);
        fetched.Floors.ShouldAllBe(f => f.LocationId == location.Id);
    }

    [Fact]
    public async Task Delete_WhileReferencedByFloor_Returns409InUse()
    {
        var location = await CreateLocationAsync();
        await CreateFloorAsync(location.Id);

        var response = await Client.DeleteAsync($"/api/locations/{location.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe("Resource in use");
    }

    [Fact]
    public async Task Delete_WhenNoFloors_Returns204()
    {
        var location = await CreateLocationAsync();

        var response = await Client.DeleteAsync($"/api/locations/{location.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Client.GetAsync($"/api/locations/{location.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetMap_WhenMissing_Returns404()
    {
        var response = await Client.GetAsync("/api/locations/999999/map");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe("Resource not found");
    }

    [Fact]
    public async Task GetMap_ReturnsFullBuildingGraph()
    {
        // Build a small two-floor building: basement + first floor, a room on each, a nested
        // container tree in one room, an exterior door, and stairs connecting the two floors.
        var location = await CreateLocationAsync("House", "My house");
        var basement = await CreateFloorAsync(location.Id, "Basement", -1);
        var first = await CreateFloorAsync(location.Id, "First Floor", 0);

        var basementRoom = await CreateRoomAsync(basement.Id, "Cellar");
        var livingRoom = await CreateRoomAsync(first.Id, "Living Room");

        // Top-level container with a nested child, both in the living room.
        var dresser = await Client.PostAsJsonAsync("/api/containers",
            new CreateContainerRequest("Dresser", null, livingRoom.Id, null, ContainerType.Cabinet));
        dresser.StatusCode.ShouldBe(HttpStatusCode.Created);
        var dresserId = (await dresser.Content.ReadFromJsonAsync<ContainerResponse>())!.Id;

        var drawer = await Client.PostAsJsonAsync("/api/containers",
            new CreateContainerRequest("Drawer", null, null, dresserId, ContainerType.Drawer));
        drawer.StatusCode.ShouldBe(HttpStatusCode.Created);

        // Front door (leads outside) and stairs (cross-floor: basement <-> first).
        (await Client.PostAsJsonAsync("/api/doors",
            new CreateDoorRequest("Front Door", DoorKind.Door, livingRoom.Id, null, Wall.South, 12, 36, 80, null, null)))
            .StatusCode.ShouldBe(HttpStatusCode.Created);
        (await Client.PostAsJsonAsync("/api/doors",
            new CreateDoorRequest("Stairs", DoorKind.Stairs, basementRoom.Id, livingRoom.Id, Wall.North, 24, 36, 84, null, null)))
            .StatusCode.ShouldBe(HttpStatusCode.Created);

        var map = await Client.GetFromJsonAsync<LocationMapResponse>($"/api/locations/{location.Id}/map");

        map.ShouldNotBeNull();
        map.Name.ShouldBe("House");
        // Floors are ordered by LevelIndex (basement first).
        map.Floors.Count.ShouldBe(2);
        map.Floors.Select(f => f.Name).ShouldBe(["Basement", "First Floor"]);

        var firstFloor = map.Floors.Single(f => f.Name == "First Floor");
        var living = firstFloor.Rooms.Single(r => r.Name == "Living Room");

        // Nested container tree round-trips: Dresser -> Drawer.
        living.Containers.Count.ShouldBe(1);
        living.Containers[0].Name.ShouldBe("Dresser");
        living.Containers[0].Children.Count.ShouldBe(1);
        living.Containers[0].Children[0].Name.ShouldBe("Drawer");

        // The living room has the front door; the basement room owns the stairs.
        living.Doors.ShouldContain(d => d.Name == "Front Door" && d.ToRoomId == null);
        var cellar = map.Floors.Single(f => f.Name == "Basement").Rooms.Single();
        cellar.Doors.ShouldContain(d => d.Kind == DoorKind.Stairs && d.ToRoomId == living.Id);
    }
}
