using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using Domain.Enums;
using ItemCatalogueAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ItemCatalogueAPI.Tests;

public class DoorApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    private async Task<int> CreateRoomIdAsync(string name = "Bedroom", int levelIndex = 0)
    {
        var loc = await Client.PostAsJsonAsync("/api/locations", new CreateLocationRequest("House", null));
        loc.StatusCode.ShouldBe(HttpStatusCode.Created);
        var locationId = (await loc.Content.ReadFromJsonAsync<LocationResponse>())!.Id;

        var floor = await Client.PostAsJsonAsync("/api/floors", new CreateFloorRequest("Floor", locationId, levelIndex, null, null));
        floor.StatusCode.ShouldBe(HttpStatusCode.Created);
        var floorId = (await floor.Content.ReadFromJsonAsync<FloorResponse>())!.Id;

        var room = await Client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest(name, null, floorId));
        room.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await room.Content.ReadFromJsonAsync<RoomResponse>())!.Id;
    }

    private async Task<DoorResponse> CreateDoorAsync()
    {
        var fromRoomId = await CreateRoomIdAsync();
        var response = await Client.PostAsJsonAsync("/api/doors",
            new CreateDoorRequest("Front Door", DoorKind.Door, fromRoomId, null, Wall.South, 12, 36, 80, null, null));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<DoorResponse>())!;
    }

    [Fact]
    public async Task Create_LeadsOutside_Returns201WithNullToRoom()
    {
        var fromRoomId = await CreateRoomIdAsync();

        var response = await Client.PostAsJsonAsync("/api/doors",
            new CreateDoorRequest("Front Door", DoorKind.Door, fromRoomId, null, Wall.South, 12, 36, 80, null, null));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
        var created = await response.Content.ReadFromJsonAsync<DoorResponse>();
        created!.Id.ShouldBeGreaterThan(0);
        created.FromRoomId.ShouldBe(fromRoomId);
        created.ToRoomId.ShouldBeNull();
        created.Kind.ShouldBe(DoorKind.Door);
    }

    [Fact]
    public async Task Create_Stairs_ConnectingRoomsOnDifferentFloors_Returns201()
    {
        var basementRoom = await CreateRoomIdAsync("Cellar", -1);
        var firstFloorRoom = await CreateRoomIdAsync("Hall", 0);

        var response = await Client.PostAsJsonAsync("/api/doors",
            new CreateDoorRequest("Stairs", DoorKind.Stairs, basementRoom, firstFloorRoom, Wall.North, 24, 36, 84, null, null));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<DoorResponse>();
        created!.Kind.ShouldBe(DoorKind.Stairs);
        created.ToRoomId.ShouldBe(firstFloorRoom);
    }

    [Fact]
    public async Task Create_ConnectingRoomToItself_Returns400()
    {
        var roomId = await CreateRoomIdAsync();

        var response = await Client.PostAsJsonAsync("/api/doors",
            new CreateDoorRequest("Bad", DoorKind.Doorway, roomId, roomId, Wall.East, 6, 32, 80, null, null));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task GetById_WhenMissing_Returns404()
    {
        var response = await Client.GetAsync("/api/doors/999999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe("Resource not found");
    }

    [Fact]
    public async Task Update_WithStaleRowVersion_Returns409Concurrency()
    {
        var created = await CreateDoorAsync();

        var first = await Client.PutAsJsonAsync(
            $"/api/doors/{created.Id}",
            new UpdateDoorRequest(created.Id, "Renamed", created.Kind, created.FromRoomId, null, created.Wall, created.OffsetInches, created.WidthInches, created.HeightInches, null, null, created.RowVersion));
        first.StatusCode.ShouldBe(HttpStatusCode.OK);

        var second = await Client.PutAsJsonAsync(
            $"/api/doors/{created.Id}",
            new UpdateDoorRequest(created.Id, "Again", created.Kind, created.FromRoomId, null, created.Wall, created.OffsetInches, created.WidthInches, created.HeightInches, null, null, created.RowVersion));

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe("Concurrency conflict");
    }

    [Fact]
    public async Task Delete_WhenExists_Returns204()
    {
        var created = await CreateDoorAsync();

        var response = await Client.DeleteAsync($"/api/doors/{created.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Client.GetAsync($"/api/doors/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
