using Application.StoragePorts;
using Azure.Storage.Blobs;
using Azure.Storage.Queues;
using Infrastructure.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

// Placed in the Microsoft.Extensions.DependencyInjection namespace (framework convention) so the
// composition root discovers AddInfrastructure() without an extra using, matching AddApplication()/AddPersistence().
namespace Microsoft.Extensions.DependencyInjection;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<BlobStorageOptions>(configuration.GetSection(BlobStorageOptions.SectionName));

        services.AddSingleton(sp => new BlobServiceClient(sp.GetRequiredService<IOptions<BlobStorageOptions>>().Value.ConnectionString));

        // AddHostedService<T>() Ensures the configured blob container exists before the app serves requests, so
        // AzureBlobImageStorage doesn't have to check on every call. Runs once at startup.
        services.AddHostedService<BlobContainerInitializer>();
        services.AddScoped<IImageStorage, AzureBlobImageStorage>();

        // Bulk-import storage: the chunk work queue, its poison twin, and the claim-check payload
        // container. Base64 message encoding is required — the Functions queue trigger decodes
        // Base64 by default, so a plain-text message would poison immediately.
        services.Configure<ImportStorageOptions>(configuration.GetSection(ImportStorageOptions.SectionName));
        services.AddSingleton(sp => new QueueServiceClient(
            sp.GetRequiredService<IOptions<ImportStorageOptions>>().Value.ConnectionString,
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }));
        services.AddHostedService<ImportStorageInitializer>();

        // Singletons: all three are stateless over thread-safe Azure clients.
        services.AddSingleton<ICsvItemParser, CsvItemParser>();
        services.AddSingleton<IImportPayloadStore, BlobImportPayloadStore>();
        services.AddSingleton<IImportDispatcher, StorageQueueImportDispatcher>();

        return services;
    }
}
