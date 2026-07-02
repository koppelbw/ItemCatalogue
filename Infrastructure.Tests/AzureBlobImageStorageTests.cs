using Application.StoragePorts;
using Azure.Storage.Blobs;
using Domain.Enums;
using Infrastructure.Storage;
using Microsoft.Extensions.Options;
using Shouldly;

namespace Infrastructure.Tests;

[Collection(AzuriteCollection.Name)]
public class AzureBlobImageStorageTests(AzuriteFixture fixture)
{
    // Each test uses its own uniquely-named blob container so tests can't see each other's blobs,
    // without needing a fresh Azurite container per test.
    private AzureBlobImageStorage CreateStorage()
    {
        var serviceClient = new BlobServiceClient(fixture.ConnectionString);
        var options = Options.Create(new BlobStorageOptions
        {
            ConnectionString = fixture.ConnectionString,
            ContainerName = $"pics-{Guid.NewGuid():N}",
            ReadSasMinutes = 15,
        });
        serviceClient.GetBlobContainerClient(options.Value.ContainerName).CreateIfNotExists();

        return new AzureBlobImageStorage(serviceClient, options);
    }

    [Fact]
    public async Task UploadAsync_ThenGetReadUrl_RoundTripsTheUploadedBytes()
    {
        var storage = CreateStorage();
        var pictureId = Guid.NewGuid();
        byte[] bytes = [1, 2, 3, 4, 5];

        var stored = await storage.UploadAsync(
            new NewImage(PictureOwnerType.Item, 1, pictureId, new MemoryStream(bytes), "image/jpeg", ".jpg"));

        stored.BlobName.ShouldBe($"item/1/{pictureId:N}.jpg");
        stored.SizeBytes.ShouldBe(bytes.Length);

        using var httpClient = new HttpClient();
        var downloaded = await httpClient.GetByteArrayAsync(storage.GetReadUrl(stored.BlobName));

        downloaded.ShouldBe(bytes);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheBlob()
    {
        var storage = CreateStorage();
        var stored = await storage.UploadAsync(
            new NewImage(PictureOwnerType.Item, 1, Guid.NewGuid(), new MemoryStream([1, 2, 3]), "image/png", ".png"));

        await storage.DeleteAsync(stored.BlobName);

        using var httpClient = new HttpClient();
        var response = await httpClient.GetAsync(storage.GetReadUrl(stored.BlobName));
        response.IsSuccessStatusCode.ShouldBeFalse();
    }

    [Fact]
    public async Task DeleteAllAsync_RemovesEveryBlobInTheContainer()
    {
        var storage = CreateStorage();
        var first = await storage.UploadAsync(
            new NewImage(PictureOwnerType.Item, 1, Guid.NewGuid(), new MemoryStream([1]), "image/png", ".png"));
        var second = await storage.UploadAsync(
            new NewImage(PictureOwnerType.Room, 2, Guid.NewGuid(), new MemoryStream([2]), "image/png", ".png"));

        await storage.DeleteAllAsync();

        using var httpClient = new HttpClient();
        (await httpClient.GetAsync(storage.GetReadUrl(first.BlobName))).IsSuccessStatusCode.ShouldBeFalse();
        (await httpClient.GetAsync(storage.GetReadUrl(second.BlobName))).IsSuccessStatusCode.ShouldBeFalse();
    }
}
