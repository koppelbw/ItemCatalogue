using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using ItemCatalogueAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ItemCatalogueAPI.Tests;

public class ContainerApiTests(ApiFactory factory) : ApiTestBase(factory)
{    
    private async Task<int> CreateRoomIdAsync()
    {
        var loc = await Client.PostAsJsonAsync("/api/locations", new CreateLocationRequest("House", null));
        loc.StatusCode.ShouldBe(HttpStatusCode.Created);
        var locationId = (await loc.Content.ReadFromJsonAsync<LocationResponse>())!.Id;

        var floor = await Client.PostAsJsonAsync("/api/floors", new CreateFloorRequest("Main", locationId, 0, null, null));
        floor.StatusCode.ShouldBe(HttpStatusCode.Created);
        var floorId = (await floor.Content.ReadFromJsonAsync<FloorResponse>())!.Id;

        var room = await Client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest("Bedroom", null, floorId));
        room.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await room.Content.ReadFromJsonAsync<RoomResponse>())!.Id;
    }

    private async Task<ContainerResponse> CreateTopLevelAsync(string name = "Dresser")
    {
        var roomId = await CreateRoomIdAsync();
        var response = await Client.PostAsJsonAsync("/api/containers", new CreateContainerRequest(name, null, roomId, null));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<ContainerResponse>())!;
    }

    [Fact]
    public async Task Create_TopLevelInRoom_Returns201WithRoomId()
    {
        var roomId = await CreateRoomIdAsync();

        var response = await Client.PostAsJsonAsync("/api/containers", new CreateContainerRequest("Dresser", "Bedroom dresser", roomId, null));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
        var created = await response.Content.ReadFromJsonAsync<ContainerResponse>();
        created!.Id.ShouldBeGreaterThan(0);
        created.RoomId.ShouldBe(roomId);
        created.ParentContainerId.ShouldBeNull();
    }

    [Fact]
    public async Task Create_NestedInContainer_Returns201WithParentContainerId()
    {
        var parent = await CreateTopLevelAsync("Closet");

        var response = await Client.PostAsJsonAsync("/api/containers", new CreateContainerRequest("Storage Bin", null, null, parent.Id));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<ContainerResponse>();
        created!.ParentContainerId.ShouldBe(parent.Id);
        created.RoomId.ShouldBeNull();
    }

    [Fact]
    public async Task Create_WithBothRoomAndParent_Returns400()
    {
        var roomId = await CreateRoomIdAsync();
        var parent = await CreateTopLevelAsync("Closet");

        var response = await Client.PostAsJsonAsync("/api/containers", new CreateContainerRequest("Box", null, roomId, parent.Id));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
    }

    [Fact]
    public async Task Create_WithNeitherRoomNorParent_Returns400()
    {
        var response = await Client.PostAsJsonAsync("/api/containers", new CreateContainerRequest("Box", null, null, null));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetById_WhenExists_Returns200()
    {
        var created = await CreateTopLevelAsync();

        var fetched = await Client.GetFromJsonAsync<ContainerResponse>($"/api/containers/{created.Id}");

        fetched!.Name.ShouldBe("Dresser");
    }

    [Fact]
    public async Task GetById_WhenMissing_Returns404()
    {
        var response = await Client.GetAsync("/api/containers/999999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe("Resource not found");
    }

    [Fact]
    public async Task Update_WithStaleRowVersion_Returns409Concurrency()
    {
        var created = await CreateTopLevelAsync();

        var first = await Client.PutAsJsonAsync(
            $"/api/containers/{created.Id}",
            new UpdateContainerRequest(created.Id, "Wardrobe", null, created.RoomId, null, created.RowVersion));
        first.StatusCode.ShouldBe(HttpStatusCode.OK);

        var second = await Client.PutAsJsonAsync(
            $"/api/containers/{created.Id}",
            new UpdateContainerRequest(created.Id, "Armoire", null, created.RoomId, null, created.RowVersion));

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe("Concurrency conflict");
    }

    [Fact]
    public async Task Delete_WhileReferencedByChild_Returns409InUse()
    {
        var parent = await CreateTopLevelAsync("Closet");
        var child = await Client.PostAsJsonAsync("/api/containers", new CreateContainerRequest("Storage Bin", null, null, parent.Id));
        child.StatusCode.ShouldBe(HttpStatusCode.Created);

        var response = await Client.DeleteAsync($"/api/containers/{parent.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe("Resource in use");
    }

    [Fact]
    public async Task Delete_LeafContainer_Returns204()
    {
        var created = await CreateTopLevelAsync();

        var response = await Client.DeleteAsync($"/api/containers/{created.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Client.GetAsync($"/api/containers/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
