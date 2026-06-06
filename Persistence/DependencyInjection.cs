using Domain.RepositoryPorts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Persistence;
using Persistence.Interceptors;
using Persistence.RepositoryAdapters;

// Placed in the Microsoft.Extensions.DependencyInjection namespace (framework convention)
// so the composition root discovers AddPersistence() without an extra using.
namespace Microsoft.Extensions.DependencyInjection;

// Registers the Persistence layer: repositories, the DbContext, and the auditing infrastructure.
// Each layer owns its own DI wiring.
public static class DependencyInjection
{
    public static IServiceCollection AddPersistence(this IServiceCollection services, IConfiguration configuration)
    {
        // Repositories (ports -> adapters)
        services.AddScoped<IItemRepository, ItemRepository>();
        services.AddScoped<IRoomRepository, RoomRepository>();
        services.AddScoped<ILocationRepository, LocationRepository>();
        services.AddScoped<IPersonRepository, PersonRepository>();

        // Single clock source for all audit stamping (CreatedDate/LastModifiedDate). Injected into both the auditing interceptor and ItemRepository's ExecuteUpdate soft-delete
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<AuditingSaveChangesInterceptor>();

        services.AddDbContext<ItemCatalogueDbContext>((sp, options) =>
            options.UseSqlServer(configuration.GetConnectionString("local"))
                   .AddInterceptors(sp.GetRequiredService<AuditingSaveChangesInterceptor>()));

        return services;
    }
}
