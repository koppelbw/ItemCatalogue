using Application.Services;
using Application.ServicePorts;

// Placed in the Microsoft.Extensions.DependencyInjection namespace (framework convention)
// so the composition root discovers AddApplication() without an extra using.
namespace Microsoft.Extensions.DependencyInjection;

// Registers the Application layer's services. Each layer owns its own DI wiring.
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        // Services (ports -> implementations)
        services.AddScoped<IItemService, ItemService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IPersonService, PersonService>();

        return services;
    }
}
