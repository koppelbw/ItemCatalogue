using Application.DTOs;
using Domain.Enums;
using Infrastructure.Storage;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Infrastructure.Tests;

[Collection(AzuriteCollection.Name)]
public class BlobImportPayloadStoreTests(AzuriteFixture fixture)
{
    // Unique container per test so tests can't see each other's payloads (same pattern as
    // AzureBlobImageStorageTests).
    private BlobImportPayloadStore CreateStore()
    {
        var options = new ImportStorageOptions
        {
            ConnectionString = fixture.ConnectionString,
            PayloadContainerName = $"imports-{Guid.NewGuid():N}",
        };
        new Azure.Storage.Blobs.BlobContainerClient(options.ConnectionString, options.PayloadContainerName).CreateIfNotExists();
        return new BlobImportPayloadStore(Options.Create(options));
    }

    private static ImportPayloadRow Row(int rowNumber, string name) => new(
        rowNumber,
        new CreateItemRequest(name, null, [ItemType.Electronics], 5m, null, null, null, null, null, 1,
            Condition.Good, null, null, null, false, true, 7, null, 3, null, null, null));

    [Fact]
    public async Task WriteAsync_ThenReadChunkAsync_ReturnsExactlyTheRequestedSlice()
    {
        var store = CreateStore();
        await store.WriteAsync(42, [Row(2, "A"), Row(3, "B"), Row(4, "C"), Row(5, "D"), Row(6, "E")]);

        var slice = await store.ReadChunkAsync(42, startRow: 2, count: 2);

        slice.Count.ShouldBe(2);
        slice[0].RowNumber.ShouldBe(4);
        slice[0].Item.Name.ShouldBe("C");
        slice[1].RowNumber.ShouldBe(5);
        slice[1].Item.Name.ShouldBe("D");
    }

    [Fact]
    public async Task ReadChunkAsync_RoundTripsEveryRequestField()
    {
        var store = CreateStore();
        await store.WriteAsync(7, [Row(2, "Desk Lamp")]);

        var row = (await store.ReadChunkAsync(7, 0, 1)).ShouldHaveSingleItem();

        row.RowNumber.ShouldBe(2);
        row.Item.Name.ShouldBe("Desk Lamp");
        row.Item.ItemTypes.ShouldBe([ItemType.Electronics]);
        row.Item.PurchasePrice.ShouldBe(5m);
        row.Item.Condition.ShouldBe(Condition.Good);
        row.Item.RoomId.ShouldBe(7);
        row.Item.OwnerId.ShouldBe(3);
        row.Item.IsShownInUI.ShouldBeTrue();
    }

    [Fact]
    public async Task ReadChunkAsync_CountPastTheEnd_ReturnsTheRemainder()
    {
        var store = CreateStore();
        await store.WriteAsync(42, [Row(2, "A"), Row(3, "B"), Row(4, "C")]);

        var slice = await store.ReadChunkAsync(42, startRow: 2, count: 25);

        slice.ShouldHaveSingleItem().Item.Name.ShouldBe("C");
    }

    [Fact]
    public async Task WriteAsync_Twice_OverwritesThePayload()
    {
        var store = CreateStore();
        await store.WriteAsync(42, [Row(2, "Old")]);
        await store.WriteAsync(42, [Row(2, "New")]);

        (await store.ReadChunkAsync(42, 0, 10)).ShouldHaveSingleItem().Item.Name.ShouldBe("New");
    }
}
