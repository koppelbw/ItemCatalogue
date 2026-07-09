using Application.DTOs;
using Azure.Storage.Queues;
using Infrastructure.Storage;
using Microsoft.Extensions.Options;
using Shouldly;
using System.Text;
using System.Text.Json;

namespace Infrastructure.Tests;

[Collection(AzuriteCollection.Name)]
public class StorageQueueImportDispatcherTests(AzuriteFixture fixture)
{
    // Unique queue per test; the dispatcher gets the same Base64-encoded QueueServiceClient that
    // AddInfrastructure registers.
    private (StorageQueueImportDispatcher Dispatcher, QueueClient RawQueue) CreateDispatcher()
    {
        var options = new ImportStorageOptions
        {
            ConnectionString = fixture.ConnectionString,
            QueueName = $"import-{Guid.NewGuid():N}",
        };
        var serviceClient = new QueueServiceClient(
            options.ConnectionString,
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 });
        serviceClient.GetQueueClient(options.QueueName).CreateIfNotExists();

        // The raw client deliberately has NO message encoding, so tests can assert what is
        // actually on the wire — the contract the Functions queue trigger depends on.
        var rawQueue = new QueueClient(options.ConnectionString, options.QueueName);
        return (new StorageQueueImportDispatcher(serviceClient, Options.Create(options)), rawQueue);
    }

    [Fact]
    public async Task DispatchAsync_SendsOneMessagePerChunk()
    {
        var (dispatcher, rawQueue) = CreateDispatcher();

        await dispatcher.DispatchAsync(42, [new ChunkRef(0, 0, 25), new ChunkRef(1, 25, 25), new ChunkRef(2, 50, 7)]);

        var messages = (await rawQueue.ReceiveMessagesAsync(maxMessages: 10)).Value;
        messages.Length.ShouldBe(3);
    }

    [Fact]
    public async Task DispatchAsync_MessagesAreBase64EncodedJson_MatchingTheFunctionsTriggerDefault()
    {
        var (dispatcher, rawQueue) = CreateDispatcher();

        await dispatcher.DispatchAsync(42, [new ChunkRef(1, 25, 25)]);

        // Read raw (no client-side decoding): the wire format must be Base64 so the Functions
        // host's default queue-trigger decoding accepts it.
        var raw = (await rawQueue.ReceiveMessagesAsync(maxMessages: 1)).Value.ShouldHaveSingleItem().MessageText;
        var json = Encoding.UTF8.GetString(Convert.FromBase64String(raw));

        var message = JsonSerializer.Deserialize<ImportChunkMessage>(json);
        message.ShouldBe(new ImportChunkMessage(42, 1, 25, 25));
    }

    [Fact]
    public async Task DispatchAsync_NoChunks_SendsNothing()
    {
        var (dispatcher, rawQueue) = CreateDispatcher();

        await dispatcher.DispatchAsync(42, []);

        (await rawQueue.PeekMessagesAsync(maxMessages: 10)).Value.ShouldBeEmpty();
    }
}
