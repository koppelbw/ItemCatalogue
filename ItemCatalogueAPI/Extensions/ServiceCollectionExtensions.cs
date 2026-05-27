using Application.Services;
using Domain.RepositoryPorts;
using Persistence.RepositoryAdapters;

namespace ItemCatalogueAPI.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<ItemService>();

        return services;
    }
}