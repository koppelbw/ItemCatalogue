using Microsoft.Extensions.Options;

namespace ItemCatalogueAPI.ScheduledReset;

// Periodically restores the database to its seed baseline. Only registered when
// ScheduledReset:Enabled is true (see ScheduledResetServiceCollectionExtensions) — intended for a
// public demo environment, not the private deployment holding real data. Resets once immediately
// on startup (so a redeploy/restart always begins from a clean slate), then on a fixed interval.
public sealed class ScheduledResetBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ScheduledResetOptions> options,
    ILogger<ScheduledResetBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(options.Value.IntervalHours));

        do
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var resetService = scope.ServiceProvider.GetRequiredService<DatabaseResetService>();
                await resetService.ResetAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // A failed reset (e.g. transient DB connectivity) should not crash the host or stop
                // future attempts — log and try again on the next tick.
                logger.DatabaseResetFailed(ex);
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
