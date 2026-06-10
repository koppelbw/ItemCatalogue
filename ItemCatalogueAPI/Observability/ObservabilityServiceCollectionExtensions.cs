using System.Reflection;
using Azure.Monitor.OpenTelemetry.Exporter;
using ItemCatalogueAPI.Observability;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// Placed in the Microsoft.Extensions.DependencyInjection namespace (framework convention)
// so the composition root discovers AddObservability() without an extra using, matching the
// AddApplication()/AddPersistence()/AddGlobalExceptionHandling() per-layer DI modules.
namespace Microsoft.Extensions.DependencyInjection;

// Wires up OpenTelemetry for the three signals (traces, metrics, logs) with auto-instrumentation
// for ASP.NET Core requests, outgoing HttpClient calls, and SQL Server queries, plus .NET runtime
// metrics. The instrumentation is vendor-neutral; only the exporter is environment-specific:
//
//   * Application Insights (Azure Monitor) when a connection string is configured (Azure).
//   * OTLP when an OTEL endpoint is configured (e.g. the local .NET Aspire dashboard / a collector).
//   * Neither configured -> telemetry is collected but not exported (quiet no-op for plain local runs).
//
// ILogger<T> calls from every layer flow through the OpenTelemetry logging provider here, so no
// layer references OpenTelemetry or Azure directly — they depend only on Microsoft.Extensions.Logging.
public static class ObservabilityServiceCollectionExtensions
{
    private const string ServiceName = "ItemCatalogue.Api";

    public static IServiceCollection AddObservability(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Connection string drives Azure Monitor export. Supports both the appsettings key and the
        // conventional APPLICATIONINSIGHTS_CONNECTION_STRING environment variable (used in Azure).
        var azureMonitorConnectionString =
            configuration["ApplicationInsights:ConnectionString"]
            ?? configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"];

        // OTLP endpoint drives local export (e.g. http://localhost:4317 for the Aspire dashboard).
        // Gating on its presence avoids connection-refused noise when nothing is listening locally.
        var otlpEndpoint = configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

        var exportToAzureMonitor = !string.IsNullOrWhiteSpace(azureMonitorConnectionString);
        var exportToOtlp = !string.IsNullOrWhiteSpace(otlpEndpoint);

        // Fixed-rate sampling for cost control. 1.0 (keep everything) by default; override per
        // environment via OpenTelemetry:SamplingRatio. ParentBased keeps whole traces intact.
        var samplingRatio = configuration.GetValue<double?>("OpenTelemetry:SamplingRatio") ?? 1.0;

        var serviceVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        void ConfigureResource(ResourceBuilder resource) =>
            resource
                .AddService(serviceName: ServiceName, serviceVersion: serviceVersion)
                // Standard OTel attribute; lets telemetry from each environment (Development/Production)
                // be filtered apart in Application Insights instead of all landing in one bucket.
                .AddAttributes([new KeyValuePair<string, object>("deployment.environment", environment.EnvironmentName)]);

        services.AddOpenTelemetry()
            .ConfigureResource(ConfigureResource)
            .WithTracing(tracing =>
            {
                tracing
                    .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(samplingRatio)))
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation(options =>
                    {
                        // Record DB exceptions on the span. SQL parameter values are NOT captured by
                        // default (they can carry PII and inflate ingestion), which is the desired
                        // production behaviour; the parameterized command shape is still recorded.
                        options.RecordException = true;
                    });

                if (exportToAzureMonitor)
                {
                    tracing.AddAzureMonitorTraceExporter(o => o.ConnectionString = azureMonitorConnectionString);
                }

                if (exportToOtlp)
                {
                    tracing.AddOtlpExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (exportToAzureMonitor)
                {
                    metrics.AddAzureMonitorMetricExporter(o => o.ConnectionString = azureMonitorConnectionString);
                }

                if (exportToOtlp)
                {
                    metrics.AddOtlpExporter();
                }
            });

        // ILogger -> OpenTelemetry logs. Added alongside the host's default Console provider (so local
        // console output is unaffected); structured state (the {Named} placeholders from every layer)
        // is preserved and exported as log records to whichever exporter(s) are configured below.
        services.AddLogging(logging =>
        {
            logging.AddOpenTelemetry(options =>
            {
                var resource = ResourceBuilder.CreateDefault();
                ConfigureResource(resource);
                options.SetResourceBuilder(resource);

                options.IncludeScopes = true;
                options.IncludeFormattedMessage = true;

                if (exportToAzureMonitor)
                {
                    options.AddAzureMonitorLogExporter(o => o.ConnectionString = azureMonitorConnectionString);
                }

                if (exportToOtlp)
                {
                    options.AddOtlpExporter();
                }
            });
        });

        // Surface the exporter decision once at startup (AddObservability runs before the logger
        // factory exists, so the actual logging happens in the hosted service).
        var exporterStatus = new TelemetryStartupLogger.ExporterStatus(
            AzureMonitor: exportToAzureMonitor,
            Otlp: exportToOtlp,
            OtlpEndpoint: otlpEndpoint,
            IsProduction: environment.IsProduction());
        services.AddSingleton(exporterStatus);
        services.AddHostedService<TelemetryStartupLogger>();

        return services;
    }
}
