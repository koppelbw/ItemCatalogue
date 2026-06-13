using System.Net;
using System.Net.Http.Json;
using Application.DTOs;
using ItemCatalogueAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ItemCatalogueAPI.Tests;

// Drives the /api/rooms endpoints over real HTTP through the full pipeline (routing, model binding,
// validation, services, EF, SQL Server, and the RFC 9457 problem-details mapping). Room exercises
// the generic CRUD surface plus the optimistic-concurrency 409 path. (The FK-restrict 409 path now
// lives on Location, since Room owns the FK; see LocationApiTests.)
public class RoomApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    // Every Room needs a parent Location (Room.LocationId is a required FK).
    private async Task<int> CreateLocationIdAsync(string name = "House")
    {
        var response = await Client.PostAsJsonAsync("/api/locations", new CreateLocationRequest(name, null));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<LocationResponse>())!.Id;
    }

    private async Task<RoomResponse> CreateRoomAsync(string name = "Garage", string? description = "Out back", int? locationId = null)
    {
        locationId ??= await CreateLocationIdAsync();
        var response = await Client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest(name, description, locationId.Value));
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await response.Content.ReadFromJsonAsync<RoomResponse>())!;
    }

    [Fact]
    public async Task Create_WithValidBody_Returns201WithLocationHeaderAndBody()
    {
        var locationId = await CreateLocationIdAsync();
        var response = await Client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest("Garage", "Out back", locationId));

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();

        var created = await response.Content.ReadFromJsonAsync<RoomResponse>();
        created.ShouldNotBeNull();
        created.Id.ShouldBeGreaterThan(0);
        created.Name.ShouldBe("Garage");
        created.RowVersion.ShouldNotBeEmpty();

        // The Location header should resolve to the new resource.
        var followUp = await Client.GetAsync(response.Headers.Location);
        followUp.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Create_WithEmptyName_Returns400ValidationProblem()
    {
        // LocationId is positive so it passes its own rule; the empty Name is the rejection under test.
        var response = await Client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest("", null, 1));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("application/problem+json");
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("validation");
        body.ShouldContain("Name");
    }

    [Fact]
    public async Task GetById_WhenExists_Returns200()
    {
        var created = await CreateRoomAsync();

        var response = await Client.GetAsync($"/api/rooms/{created.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var room = await response.Content.ReadFromJsonAsync<RoomResponse>();
        room!.Name.ShouldBe("Garage");
    }

    [Fact]
    public async Task GetById_WhenMissing_Returns404ProblemDetails()
    {
        var response = await Client.GetAsync("/api/rooms/999999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe("Resource not found");
        problem.Status.ShouldBe(404);
    }

    [Fact]
    public async Task GetAll_ReturnsPagedResponseWithMetadata()
    {
        for (var i = 1; i <= 3; i++)
        {
            await CreateRoomAsync($"Room {i}");
        }

        var page = await Client.GetFromJsonAsync<PagedResponse<RoomResponse>>("/api/rooms?page=1&pageSize=10");

        page.ShouldNotBeNull();
        page.TotalCount.ShouldBe(3);
        page.Page.ShouldBe(1);
        page.PageSize.ShouldBe(10);
        page.Items.Count.ShouldBe(3);
        page.HasNext.ShouldBeFalse();
        page.HasPrevious.ShouldBeFalse();
    }

    [Fact]
    public async Task Update_WithValidBody_Returns200WithUpdatedRoom()
    {
        var created = await CreateRoomAsync();

        var response = await Client.PutAsJsonAsync(
            $"/api/rooms/{created.Id}",
            new UpdateRoomRequest(created.Id, "Shed", "Renamed", created.LocationId, created.RowVersion));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<RoomResponse>();
        updated!.Name.ShouldBe("Shed");
    }

    [Fact]
    public async Task Update_WithRouteBodyIdMismatch_Returns400()
    {
        var created = await CreateRoomAsync();

        // Route id and body id deliberately disagree; the controller maps this to a 400.
        var response = await Client.PutAsJsonAsync(
            $"/api/rooms/{created.Id}",
            new UpdateRoomRequest(created.Id + 1, "Shed", null, created.LocationId, created.RowVersion));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Update_WhenMissing_Returns404()
    {
        var response = await Client.PutAsJsonAsync(
            "/api/rooms/999999",
            new UpdateRoomRequest(999999, "Shed", null, 1, [0, 0, 0, 0, 0, 0, 0, 1]));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Update_WithStaleRowVersion_Returns409Concurrency()
    {
        var created = await CreateRoomAsync();

        // First update succeeds and bumps the server's rowversion.
        var first = await Client.PutAsJsonAsync(
            $"/api/rooms/{created.Id}",
            new UpdateRoomRequest(created.Id, "Shed", null, created.LocationId, created.RowVersion));
        first.StatusCode.ShouldBe(HttpStatusCode.OK);

        // Reusing the original (now stale) rowversion must be rejected as a concurrency conflict.
        var second = await Client.PutAsJsonAsync(
            $"/api/rooms/{created.Id}",
            new UpdateRoomRequest(created.Id, "Workshop", null, created.LocationId, created.RowVersion));

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe("Concurrency conflict");
    }

    [Fact]
    public async Task Delete_WhenExists_Returns204()
    {
        var created = await CreateRoomAsync();

        var response = await Client.DeleteAsync($"/api/rooms/{created.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Client.GetAsync($"/api/rooms/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_WhenMissing_Returns404()
    {
        var response = await Client.DeleteAsync("/api/rooms/999999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe("Resource not found");
    }
}
