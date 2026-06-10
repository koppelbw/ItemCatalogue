using Microsoft.Extensions.Logging;

namespace ItemCatalogueAPI.Observability;

// Logs which telemetry exporter(s) AddObservability wired up, once at host start. AddObservability
// runs during service registration (before the logger factory exists), so the decision is captured
// there and surfaced here via a hosted service — turning the otherwise-silent "no exporter" no-op
// into a visible startup line, which is exactly the case that makes telemetry look mysteriously empty.
internal sealed class TelemetryStartupLogger(
    ILogger<TelemetryStartupLogger> logger,
    TelemetryStartupLogger.ExporterStatus status) : IHostedService
{
    // The exporter decision, captured at registration time.
    public sealed record ExporterStatus(bool AzureMonitor, bool Otlp, string? OtlpEndpoint, bool IsProduction);

    public Task StartAsync(CancellationToken cancellationToken)
    {
        switch (status)
        {
            case { AzureMonitor: true, Otlp: true }:
                logger.ExportingToAzureMonitorAndOtlp(status.OtlpEndpoint);
                break;
            case { AzureMonitor: true }:
                logger.ExportingToAzureMonitor();
                break;
            case { Otlp: true }:
                logger.ExportingToOtlp(status.OtlpEndpoint);
                break;
            case { IsProduction: true }:
                // No exporter in Production is almost certainly a misconfiguration — warn loudly.
                logger.NoExporterInProduction();
                break;
            default:
                logger.NoExporter();
                break;
        }

        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

// Source-generated, matching the ServiceLog/RepositoryLog convention used elsewhere.
internal static partial class ObservabilityLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Telemetry exporting to Azure Monitor (Application Insights)")]
    public static partial void ExportingToAzureMonitor(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "Telemetry exporting to OTLP endpoint {OtlpEndpoint}")]
    public static partial void ExportingToOtlp(this ILogger logger, string? otlpEndpoint);

    [LoggerMessage(Level = LogLevel.Information, Message = "Telemetry exporting to Azure Monitor and OTLP endpoint {OtlpEndpoint}")]
    public static partial void ExportingToAzureMonitorAndOtlp(this ILogger logger, string? otlpEndpoint);

    [LoggerMessage(Level = LogLevel.Warning, Message = "No telemetry exporter configured; traces, metrics and logs will NOT be exported. Set ApplicationInsights:ConnectionString or the APPLICATIONINSIGHTS_CONNECTION_STRING environment variable.")]
    public static partial void NoExporterInProduction(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Information, Message = "No telemetry exporter configured; local collection only (set OTEL_EXPORTER_OTLP_ENDPOINT or an Application Insights connection string to export).")]
    public static partial void NoExporter(this ILogger logger);
}
