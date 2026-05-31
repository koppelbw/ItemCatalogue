using Application.Services;
using Application.ServicePorts;
using Domain.RepositoryPorts;
using Persistence.RepositoryAdapters;

namespace ItemCatalogueAPI.Extensions;

// Extension methods for configuring application services and repositories.
public static class ServiceCollectionExtensions
{
    // Adds repository and service registrations to the dependency injection container.
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        // Repositories (ports -> adapters)
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IPersonRepository, PersonRepository>();

        // Services (ports -> implementations)
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IPersonService, PersonService>();

        return services;
    }
}
