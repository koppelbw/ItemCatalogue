using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.DTOs;
using Domain.Enums;
using ItemCatalogueAPI.Tests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Shouldly;

namespace ItemCatalogueAPI.Tests;

// Drives the /api/{owner}/{id}/pictures and /api/pictures/{id} endpoints end to end, including the
// real Azurite-backed upload/read/delete round trip (see ApiFactory, which now also starts an
// Azurite container alongside the SQL Server one).
public class PictureApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    // Minimal valid JFIF header: enough for the magic-byte sniff (PictureService) to accept it as image/jpeg.
    private static readonly byte[] JpegBytes = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46];

    private async Task<int> CreateItemIdAsync()
    {
        var loc = await Client.PostAsJsonAsync("/api/locations", new CreateLocationRequest("House", null));
        loc.StatusCode.ShouldBe(HttpStatusCode.Created);
        var locationId = (await loc.Content.ReadFromJsonAsync<LocationResponse>())!.Id;

        var floor = await Client.PostAsJsonAsync("/api/floors", new CreateFloorRequest("Main", locationId, 0, null, null));
        floor.StatusCode.ShouldBe(HttpStatusCode.Created);
        var floorId = (await floor.Content.ReadFromJsonAsync<FloorResponse>())!.Id;

        var room = await Client.PostAsJsonAsync("/api/rooms", new CreateRoomRequest("Bedroom", null, floorId));
        room.StatusCode.ShouldBe(HttpStatusCode.Created);
        var roomId = (await room.Content.ReadFromJsonAsync<RoomResponse>())!.Id;

        var item = await Client.PostAsJsonAsync("/api/items", new CreateItemRequest(
            "Lamp", null, [ItemType.Electronics], null, null, null, null, null, null, 1, null, null,
            null, null, false, true, roomId, null, null, null, null, null));
        item.StatusCode.ShouldBe(HttpStatusCode.Created);
        return (await item.Content.ReadFromJsonAsync<ItemResponse>())!.Id;
    }

    private static MultipartFormDataContent UploadContent(
        byte[]? bytes = null, string contentType = "image/jpeg", string fileName = "photo.jpg",
        string? caption = "A caption", bool isPrimary = false)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(bytes ?? JpegBytes);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);

        if (caption is not null)
        {
            content.Add(new StringContent(caption), "caption");
        }

        content.Add(new StringContent(isPrimary.ToString()), "isPrimary");
        return content;
    }

    [Fact]
    public async Task Upload_ValidImage_Returns201WithReadableUrl()
    {
        var itemId = await CreateItemIdAsync();

        var response = await Client.PostAsync($"/api/items/{itemId}/pictures", UploadContent());

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        response.Headers.Location.ShouldNotBeNull();
        var created = await response.Content.ReadFromJsonAsync<PictureResponse>();
        created!.OwnerId.ShouldBe(itemId);
        created.OwnerType.ShouldBe(PictureOwnerType.Item);
        created.Caption.ShouldBe("A caption");

        // The Url is a live SAS link into the Azurite container started by ApiFactory; fetch it back
        // to prove the round trip through IImageStorage actually persisted the bytes.
        using var blobClient = new HttpClient();
        var downloaded = await blobClient.GetByteArrayAsync(created.Url);
        downloaded.ShouldBe(JpegBytes);
    }

    [Fact]
    public async Task Upload_UnsupportedContentType_Returns400()
    {
        var itemId = await CreateItemIdAsync();

        var response = await Client.PostAsync(
            $"/api/items/{itemId}/pictures", UploadContent(contentType: "application/pdf"));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Upload_ContentDoesNotMatchDeclaredType_Returns400()
    {
        var itemId = await CreateItemIdAsync();

        var response = await Client.PostAsync(
            $"/api/items/{itemId}/pictures", UploadContent(bytes: [0x00, 0x01, 0x02, 0x03]));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetForOwner_AfterUpload_ListsThePicture()
    {
        var itemId = await CreateItemIdAsync();
        await Client.PostAsync($"/api/items/{itemId}/pictures", UploadContent());

        var page = await Client.GetFromJsonAsync<PagedResponse<PictureResponse>>($"/api/items/{itemId}/pictures");

        page!.TotalCount.ShouldBe(1);
        page.Items.Single().OwnerId.ShouldBe(itemId);
    }

    [Fact]
    public async Task GetById_WhenMissing_Returns404()
    {
        var response = await Client.GetAsync("/api/pictures/999999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe("Resource not found");
    }

    [Fact]
    public async Task Update_ChangesCaptionAndSortOrder_Returns200()
    {
        var itemId = await CreateItemIdAsync();
        var uploadResponse = await Client.PostAsync($"/api/items/{itemId}/pictures", UploadContent());
        var created = (await uploadResponse.Content.ReadFromJsonAsync<PictureResponse>())!;

        var response = await Client.PutAsJsonAsync(
            $"/api/pictures/{created.Id}",
            new UpdatePictureRequest(created.Id, "New caption", true, 1, created.RowVersion));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<PictureResponse>();
        updated!.Caption.ShouldBe("New caption");
        updated.SortOrder.ShouldBe(1);
    }

    [Fact]
    public async Task Update_WithStaleRowVersion_Returns409Concurrency()
    {
        var itemId = await CreateItemIdAsync();
        var uploadResponse = await Client.PostAsync($"/api/items/{itemId}/pictures", UploadContent());
        var created = (await uploadResponse.Content.ReadFromJsonAsync<PictureResponse>())!;

        var first = await Client.PutAsJsonAsync(
            $"/api/pictures/{created.Id}",
            new UpdatePictureRequest(created.Id, "First", false, 0, created.RowVersion));
        first.StatusCode.ShouldBe(HttpStatusCode.OK);

        var second = await Client.PutAsJsonAsync(
            $"/api/pictures/{created.Id}",
            new UpdatePictureRequest(created.Id, "Second", false, 0, created.RowVersion));

        second.StatusCode.ShouldBe(HttpStatusCode.Conflict);
        var problem = await second.Content.ReadFromJsonAsync<ProblemDetails>();
        problem!.Title.ShouldBe("Concurrency conflict");
    }

    [Fact]
    public async Task Delete_ExistingPicture_Returns204()
    {
        var itemId = await CreateItemIdAsync();
        var uploadResponse = await Client.PostAsync($"/api/items/{itemId}/pictures", UploadContent());
        var created = (await uploadResponse.Content.ReadFromJsonAsync<PictureResponse>())!;

        var response = await Client.DeleteAsync($"/api/pictures/{created.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        (await Client.GetAsync($"/api/pictures/{created.Id}")).StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_Missing_Returns404()
    {
        var response = await Client.DeleteAsync("/api/pictures/999999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
