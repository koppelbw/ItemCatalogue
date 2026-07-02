using Application.StoragePorts;
using Azure.Storage.Blobs;
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

        return services;
    }
}
