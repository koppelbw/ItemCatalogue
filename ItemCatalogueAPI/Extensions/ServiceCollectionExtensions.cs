using Application.Services;
using Domain.RepositoryPorts;
using Persistence.RepositoryAdapters;

namespace ItemCatalogueAPI.Extensions;

// Extension methods for configuring application services and repositories.
public static class ServiceCollectionExtensions
{
    // Adds repository and service registrations to the dependency injection container.
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<ItemService>();

        return services;
    }
}