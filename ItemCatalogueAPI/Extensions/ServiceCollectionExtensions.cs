using Application.ServicePorts;
using Domain.RepositoryPorts;
using Persistence.RepositoryAdapters;
using Service.ServiceAdapters;

namespace ItemCatalogueAPI.Extensions;

/// <summary>
/// Extension methods for configuring application services and repositories.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds repository and service registrations to the dependency injection container.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for method chaining.</returns>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Register repositories
        services.AddScoped<IItemRepository, ItemRepository>();

        // Register services
        services.AddScoped<IItemService, ItemService>();

        return services;
    }
}