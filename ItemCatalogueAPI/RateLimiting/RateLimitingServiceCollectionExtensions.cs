using System.Threading.RateLimiting;
using ItemCatalogueAPI.RateLimiting;
using Microsoft.Extensions.Options;

// Placed in the Microsoft.Extensions.DependencyInjection namespace (framework convention)
// so the composition root discovers AddApiRateLimiting() without an extra using.
namespace Microsoft.Extensions.DependencyInjection;

// Global, per-client-IP fixed-window rate limiter. There is no authentication in front of this API
// today, so this is the only throttle standing between a public deployment and an unbounded flood
// of requests against the F1/serverless-tier database behind it. Limits are config-driven (the
// RateLimiting section) so the public demo and the private deployment can use different values.
public static class RateLimitingServiceCollectionExtensions
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(RateLimitingOptions.SectionName).Get<RateLimitingOptions>()
            ?? new RateLimitingOptions();

        // Also bound as IOptions so per-request policies (below) read current values instead of
        // the eager registration-time capture the global limiter uses.
        services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));

        services.AddRateLimiter(limiterOptions =>
        {
            limiterOptions.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            limiterOptions.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(httpContext =>
            {
                var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = options.PermitLimit,
                    Window = TimeSpan.FromSeconds(options.WindowSeconds),
                    QueueLimit = options.QueueLimit,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                });
            });

            // Dedicated, stricter policy for POST /api/imports: one import request does a whole
            // file's work (parse, resolve, dispatch), so counting it like a normal request would
            // let a handful of uploads monopolize the F1/serverless backend. Applies on top of
            // the global limiter. Options resolve per request so the test host can override the
            // limit through configuration.
            limiterOptions.AddPolicy(RateLimitingOptions.ImportPolicy, httpContext =>
            {
                var currentOptions = httpContext.RequestServices
                    .GetRequiredService<IOptionsMonitor<RateLimitingOptions>>().CurrentValue;
                var partitionKey = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";

                return RateLimitPartition.GetFixedWindowLimiter(partitionKey, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = currentOptions.ImportPermitLimit,
                    Window = TimeSpan.FromSeconds(currentOptions.ImportWindowSeconds),
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                });
            });
        });

        return services;
    }
}
