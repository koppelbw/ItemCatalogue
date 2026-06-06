using System.Diagnostics;
using ItemCatalogueAPI.ExceptionHandling;

// Placed in the Microsoft.Extensions.DependencyInjection namespace (framework convention)
// so the composition root discovers AddGlobalExceptionHandling() without an extra using.
namespace Microsoft.Extensions.DependencyInjection;

// Wires up centralized exception handling: the IExceptionHandler chain plus the RFC 9457 problem-details response contract.
public static class ExceptionHandlingServiceCollectionExtensions
{
    public static IServiceCollection AddGlobalExceptionHandling(this IServiceCollection services)
    {
        //Handlers are tried in registration order, so the specific handlers come first and GlobalExceptionHandler stays last as the catch-all fallback.
        services.AddExceptionHandler<NotFoundExceptionHandler>();
        services.AddExceptionHandler<ConflictExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();

        // Standardizes the error body (type/title/status/detail/instance) and enriches every
        // problem-details response with a correlation id so a client-reported failure can be tied back to a specific log entry.
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance ??=
                    $"{context.HttpContext.Request.Method} {context.HttpContext.Request.Path}";
                context.ProblemDetails.Extensions["traceId"] =
                    Activity.Current?.Id ?? context.HttpContext.TraceIdentifier;
            };
        });

        return services;
    }
}
