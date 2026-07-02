using Testcontainers.Azurite;

namespace Infrastructure.Tests;

// Spins up a real Azurite (the Azure Storage emulator) in Docker for the whole test run, shared via
// the [Collection] below. A real emulator is used rather than mocking BlobServiceClient because
// AzureBlobImageStorage.GetReadUrl exercises SAS generation, which depends on the emulator's
// well-known shared-key credential behaving like a real storage account.
public sealed class AzuriteFixture : IAsyncLifetime
{
    // Pin the image so the test run is reproducible and independent of the local Docker cache
    // (mirrors SqlServerFixture's SqlServerImage constant).
    private const string AzuriteImage = "mcr.microsoft.com/azure-storage/azurite:3.28.0";

    // The pinned image predates the API version the current Azure.Storage.Blobs SDK sends;
    // --skipApiVersionCheck tells Azurite to serve the request anyway instead of rejecting it.
    private readonly AzuriteContainer _container = new AzuriteBuilder(AzuriteImage)
        .WithCommand("--skipApiVersionCheck")
        .Build();

    public string ConnectionString { get; private set; } = string.Empty;

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = _container.GetConnectionString();
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}

// Single shared collection so the container starts once for all Infrastructure tests.
[CollectionDefinition(Name)]
public sealed class AzuriteCollection : ICollectionFixture<AzuriteFixture>
{
    public const string Name = "azurite";
}
