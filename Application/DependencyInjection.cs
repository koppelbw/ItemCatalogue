using Application.Services;
using Application.ServicePorts;
using Application.Validation;
using FluentValidation;

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
        services.AddScoped<IItemEventService, ItemEventService>();
        services.AddScoped<IFloorService, FloorService>();
        services.AddScoped<IRoomService, RoomService>();
        services.AddScoped<IContainerService, ContainerService>();
        services.AddScoped<IDoorService, DoorService>();
        services.AddScoped<IStairService, StairService>();
        services.AddScoped<ILocationService, LocationService>();
        services.AddScoped<IPersonService, PersonService>();
        services.AddScoped<ITagService, TagService>();
        services.AddScoped<ICollectionService, CollectionService>();
        services.AddScoped<IPictureService, PictureService>();

        // Discovers every AbstractValidator<T> in this assembly (see Application/Validation)
        // and registers it as IValidator<T> so services can take a validator by constructor injection.
        services.AddValidatorsFromAssemblyContaining<CreateItemRequestValidator>();

        return services;
    }
}
