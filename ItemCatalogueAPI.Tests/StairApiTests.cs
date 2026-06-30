using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using Domain.Enums;
using ItemCatalogueAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ItemCatalogueAPI.Tests;

public class StairApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    private async Task<int> CreateRoomIdAsync(string name = "Cellar", int levelIndex = 0)
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

    private async Task<StairResponse> CreateStairAsync()
    {
        var fromRoomId = await CreateRoomIdAsync();
        var response = await Client.PostAsJsonAsync("/api/stairs",
            new CreateStairRequest("Basement Stairs", fromRoomId, null, StairShape.Straight));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<StairResponse>())!;
    }

    [Fact]
    public async Task Create_ConnectingRoomsOnDifferentFloors_Returns201()
    {
        var basementRoom = await CreateRoomIdAsync("Cellar", -1);
        var firstFloorRoom = await CreateRoomIdAsync("Hall", 0);

        var response = await Client.PostAsJsonAsync("/api/stairs",
            new CreateStairRequest("Basement Stairs", basementRoom, firstFloorRoom, StairShape.Straight,
                PositionXInches: 12, PositionYInches: 24, RunInches: 120, WidthInches: 36, RiseInches: 96, StepCount: 13));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
        var created = await response.Content.ReadFromJsonAsync<StairResponse>();
        created!.Id.ShouldBeGreaterThan(0);
        created.FromRoomId.ShouldBe(basementRoom);
        created.ToRoomId.ShouldBe(firstFloorRoom);
        created.Shape.ShouldBe(StairShape.Straight);
        created.StepCount.ShouldBe(13);
    }

    [Fact]
    public async Task Create_LeadsToExterior_Returns201WithNullToRoom()
    {
        var fromRoomId = await CreateRoomIdAsync();

        var response = await Client.PostAsJsonAsync("/api/stairs",
            new CreateStairRequest("Deck Stairs", fromRoomId, null, StairShape.Straight));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<StairResponse>();
        created!.ToRoomId.ShouldBeNull();
    }

    [Fact]
    public async Task Create_ConnectingRoomToItself_Returns400()
    {
        var roomId = await CreateRoomIdAsync();

        var response = await Client.PostAsJsonAsync("/api/stairs",
            new CreateStairRequest("Bad", roomId, roomId, StairShape.Spiral));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task GetById_WhenMissing_Returns404()
    {
        var response = await Client.GetAsync("/api/stairs/999999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe("Resource not found");
    }

    [Fact]
    public async Task Update_WithStaleRowVersion_Returns409Concurrency()
    {
        var created = await CreateStairAsync();

        var first = await Client.PutAsJsonAsync(
            $"/api/stairs/{created.Id}",
            new UpdateStairRequest(created.Id, "Renamed", created.FromRoomId, null, created.Shape, created.RowVersion));
        first.StatusCode.ShouldBe(HttpStatusCode.OK);

        var second = await Client.PutAsJsonAsync(
            $"/api/stairs/{created.Id}",
            new UpdateStairRequest(created.Id, "Again", created.FromRoomId, null, created.Shape, created.RowVersion));

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe("Concurrency conflict");
    }

    [Fact]
    public async Task Delete_WhenExists_Returns204()
    {
        var created = await CreateStairAsync();

        var response = await Client.DeleteAsync($"/api/stairs/{created.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Client.GetAsync($"/api/stairs/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
