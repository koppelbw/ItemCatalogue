using Application.DTOs;
using Application.ServicePorts;
using Azure.Storage.Queues;
using Domain.Entities;
using Domain.Enums;
using ItemCatalogueAPI.Tests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Persistence;
using Shouldly;
using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ItemCatalogueAPI.Tests;

// Drives the async bulk-import pipeline end to end minus the Functions host: POST the CSV, read
// the chunk messages the API enqueued into Azurite, run each through IImportJobService (exactly
// what ImportChunkFunction does), then poll the status endpoint and verify the items landed.
public class ImportApiTests(ApiFactory factory) : ApiTestBase(factory)
{
    private readonly ApiFactory _factory = factory;

    private QueueClient ImportQueue() => new(
        _factory.AzuriteConnectionString,
        "item-import",
        new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });

    private static MultipartFormDataContent CsvForm(string csv, string fileName = "items.csv")
    {
        var file = new ByteArrayContent(Encoding.UTF8.GetBytes(csv));
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/csv");
        return new MultipartFormDataContent { { file, "file", fileName } };
    }

    // The queue outlives individual tests (only the database is wiped between them), so tests
    // that assert on messages drain it first.
    private async Task ClearQueueAsync()
    {
        var queue = ImportQueue();
        await queue.CreateIfNotExistsAsync();
        await queue.ClearMessagesAsync();
    }

    private async Task<(int RoomId, int OwnerId)> SeedRoomAndOwnerAsync()
    {
        await using var scope = _factory.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ItemCatalogueDbContext>();
        var room = new Room
        {
            Name = "Garage",
            Floor = new Floor { Name = "Main", LevelIndex = 0, Location = new Location { Name = "House" } },
        };
        var owner = new Person { Name = "Will" };
        db.Rooms.Add(room);
        db.People.Add(owner);
        await db.SaveChangesAsync();
        return (room.Id, owner.Id);
    }

    [Fact]
    public async Task Post_ValidCsv_Returns202WithLocation_AndEnqueuesChunkMessages()
    {
        await ClearQueueAsync();

        var response = await Client.PostAsync("/api/imports", CsvForm(
            "Name,ItemTypes\nDrill,Electronics\nLadder,Electronics\nVise,Electronics"));

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var job = (await response.Content.ReadFromJsonAsync<ImportJobResponse>())!;
        job.Id.ShouldBeGreaterThan(0);
        job.Status.ShouldBe(ImportJobStatus.Queued);
        job.TotalRows.ShouldBe(3);
        job.EnqueuedRows.ShouldBe(3);
        job.TotalChunks.ShouldBe(1);
        job.FileName.ShouldBe("items.csv");
        response.Headers.Location!.ToString().ShouldEndWith($"/api/imports/{job.Id}");

        var messages = (await ImportQueue().ReceiveMessagesAsync(maxMessages: 10)).Value;
        var message = messages.ShouldHaveSingleItem();
        var chunk = JsonSerializer.Deserialize<ImportChunkMessage>(message.MessageText)!;
        chunk.ShouldBe(new ImportChunkMessage(job.Id, 0, 0, 3));
    }

    [Fact]
    public async Task Post_ThenProcessChunks_CompletesJobAndInsertsItems()
    {
        await ClearQueueAsync();
        var (roomId, ownerId) = await SeedRoomAndOwnerAsync();

        // Row 4 fails business validation at chunk time (empty name) — the rest must survive.
        var response = await Client.PostAsync("/api/imports", CsvForm(
            "Name,ItemTypes,PurchasePrice,RoomId,OwnerId\n" +
            $"Drill,Electronics,49.99,{roomId},\n" +
            $"Ladder,Electronics,,{roomId},{ownerId}\n" +
            ",Electronics,,,\n" +
            "Vise,Electronics,,,"));

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var job = (await response.Content.ReadFromJsonAsync<ImportJobResponse>())!;
        job.EnqueuedRows.ShouldBe(4);

        // Play the Function's role: process every queued chunk through the same service it uses.
        var messages = (await ImportQueue().ReceiveMessagesAsync(maxMessages: 10)).Value;
        foreach (var message in messages)
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IImportJobService>();
            await service.ProcessChunkAsync(JsonSerializer.Deserialize<ImportChunkMessage>(message.MessageText)!);
        }

        var status = (await Client.GetFromJsonAsync<ImportJobResponse>($"/api/imports/{job.Id}"))!;
        status.Status.ShouldBe(ImportJobStatus.Completed);
        status.ProcessedChunks.ShouldBe(status.TotalChunks);
        status.Succeeded.ShouldBe(3);
        status.Failed.ShouldBe(1);
        status.Errors.ShouldHaveSingleItem().RowNumber.ShouldBe(4);

        // The survivors are real items with their referenced ids intact.
        await using var assertScope = _factory.Services.CreateAsyncScope();
        var db = assertScope.ServiceProvider.GetRequiredService<ItemCatalogueDbContext>();
        var items = await db.Items.AsNoTracking().OrderBy(i => i.Name).ToListAsync();
        items.Select(i => i.Name).ShouldBe(["Drill", "Ladder", "Vise"]);
        items[0].RoomId.ShouldNotBeNull();
        items[1].RoomId.ShouldBe(items[0].RoomId);
        items[1].OwnerId.ShouldNotBeNull();
    }

    [Fact]
    public async Task Post_NonNumericRoomId_RejectsThatRowAtIntake()
    {
        // A cell that isn't a number can't be an id, so the parser rejects it up front.
        var response = await Client.PostAsync("/api/imports", CsvForm(
            "Name,ItemTypes,RoomId\nDrill,Electronics,Atlantis\nLadder,Electronics,"));

        response.StatusCode.ShouldBe(HttpStatusCode.Accepted);
        var job = (await response.Content.ReadFromJsonAsync<ImportJobResponse>())!;
        job.TotalRows.ShouldBe(2);
        job.RejectedAtIntake.ShouldBe(1);
        job.EnqueuedRows.ShouldBe(1);
        var error = job.Errors.ShouldHaveSingleItem();
        error.RowNumber.ShouldBe(2);
        error.Messages.ShouldHaveSingleItem().ShouldContain("not a valid RoomId");
    }

    [Fact]
    public async Task Post_ThenProcessChunks_NonexistentRoomId_FailsRowAtChunkTime()
    {
        await ClearQueueAsync();

        // 999 is a well-formed id, so intake enqueues it; its non-existence is caught when the
        // chunk is processed (ItemBulkPreparer's FK check), not at intake.
        var response = await Client.PostAsync("/api/imports", CsvForm(
            "Name,ItemTypes,RoomId\nDrill,Electronics,999"));

        var job = (await response.Content.ReadFromJsonAsync<ImportJobResponse>())!;
        job.RejectedAtIntake.ShouldBe(0);
        job.EnqueuedRows.ShouldBe(1);

        var messages = (await ImportQueue().ReceiveMessagesAsync(maxMessages: 10)).Value;
        foreach (var message in messages)
        {
            await using var scope = _factory.Services.CreateAsyncScope();
            var service = scope.ServiceProvider.GetRequiredService<IImportJobService>();
            await service.ProcessChunkAsync(JsonSerializer.Deserialize<ImportChunkMessage>(message.MessageText)!);
        }

        var status = (await Client.GetFromJsonAsync<ImportJobResponse>($"/api/imports/{job.Id}"))!;
        status.Status.ShouldBe(ImportJobStatus.Completed);
        status.Succeeded.ShouldBe(0);
        status.Failed.ShouldBe(1);
        status.Errors.ShouldHaveSingleItem().Messages.ShouldContain("Room 999 does not exist.");
    }

    [Fact]
    public async Task Post_EmptyFile_Returns400()
    {
        var response = await Client.PostAsync("/api/imports", CsvForm(""));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_WithoutFileField_Returns400()
    {
        var response = await Client.PostAsync("/api/imports", new MultipartFormDataContent());

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Post_MoreRowsThanMaxRows_Returns400()
    {
        // The test host caps Import:MaxRows at 50.
        var csv = new StringBuilder("Name,ItemTypes");
        for (var i = 0; i < 51; i++) csv.Append($"\nItem {i},Electronics");

        var response = await Client.PostAsync("/api/imports", CsvForm(csv.ToString()));

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        (await response.Content.ReadAsStringAsync()).ShouldContain("maximum per import is 50");
    }

    [Fact]
    public async Task GetStatus_UnknownJob_Returns404()
    {
        var response = await Client.GetAsync("/api/imports/99999");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetTemplate_ReturnsCsvWithTheExpectedHeader()
    {
        var response = await Client.GetAsync("/api/imports/template");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType!.MediaType.ShouldBe("text/csv");
        var csv = await response.Content.ReadAsStringAsync();
        csv.ShouldStartWith("Name,Description,ItemTypes,");
        csv.ShouldContain("RoomId,ContainerId,OwnerId");
    }
}
