using ItemCatalogueAPI.ScheduledReset;

// Placed in the Microsoft.Extensions.DependencyInjection namespace (framework convention)
// so the composition root discovers AddScheduledReset() without an extra using, matching the
// AddApplication()/AddPersistence()/AddObservability()/AddGlobalExceptionHandling() per-feature DI modules.
namespace Microsoft.Extensions.DependencyInjection;

// Wires up the scheduled database reset feature. Only active when ScheduledReset:Enabled is true
// (set via appsettings, or the ScheduledReset__Enabled / ScheduledReset__IntervalHours Azure app
// settings) — intended for a public demo environment so vandalism/junk data added by visitors
// doesn't accumulate. The private deployment with real data should leave this off.
public static class ScheduledResetServiceCollectionExtensions
{
    public static IServiceCollection AddScheduledReset(this IServiceCollection services, IConfiguration configuration)
    {
        var section = configuration.GetSection(ScheduledResetOptions.SectionName);
        services.Configure<ScheduledResetOptions>(section);
        services.AddScoped<DatabaseResetService>();

        var options = section.Get<ScheduledResetOptions>() ?? new ScheduledResetOptions();
        if (options.Enabled)
        {
            services.AddHostedService<ScheduledResetBackgroundService>();
        }

        return services;
    }
}
