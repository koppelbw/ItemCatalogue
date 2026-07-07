using Application.AnthropicPorts;
using Application.StoragePorts;
using Azure.Storage.Blobs;
using Infrastructure.Anthropic;
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

        services.Configure<AnthropicOptions>(configuration.GetSection(AnthropicOptions.SectionName));

        // Typed HttpClient: the factory manages handler lifetimes (connection pooling, DNS rotation)
        // and injects the configured HttpClient into AnthropicClient's constructor.
        services.AddHttpClient<IAnthropicClient, AnthropicClient>((sp, client) =>
        {
            var anthropicOptions = sp.GetRequiredService<IOptions<AnthropicOptions>>().Value;

            client.BaseAddress = new Uri(anthropicOptions.BaseUrl);
            client.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");

            // A missing key is reported by AnthropicClient at call time (with setup instructions),
            // not here: throwing during DI resolution would turn every /api/chat request into a 500
            // before request validation has run.
            if (!string.IsNullOrWhiteSpace(anthropicOptions.ApiKey))
            {
                client.DefaultRequestHeaders.Add("x-api-key", anthropicOptions.ApiKey);
            }

            // A single agent-loop iteration can take a while at large max_tokens; give each API call headroom beyond the 100s HttpClient default.
            client.Timeout = TimeSpan.FromMinutes(3);
        });

        return services;
    }
}
