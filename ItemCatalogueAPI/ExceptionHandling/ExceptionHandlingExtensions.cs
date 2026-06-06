using System.Diagnostics;

namespace ItemCatalogueAPI.ExceptionHandling;

// Wires up centralized exception handling: the IExceptionHandler chain plus the RFC 9457 problem-details response contract. 
public static class ExceptionHandlingExtensions
{
    // Runs before the builder.Build() and registers the IExceptionHandler chain and problem-details customization.
    // The actual middleware that routes exceptions through the handlers is registered separately in Program.cs so it can be terminal and wrap the entire pipeline.
    public static IServiceCollection AddGlobalExceptionHandling(this IServiceCollection services)
    {
        //Handlers are tried in registration order, so the specific handlers come first and GlobalExceptionHandler stays last as the catch-all fallback.
        //Handlers all inject IProblemDetailsService to write problem details responses
        services.AddExceptionHandler<NotFoundExceptionHandler>();
        services.AddExceptionHandler<ConflictExceptionHandler>();
        services.AddExceptionHandler<GlobalExceptionHandler>();


        // Registers the ProblemDetails middleware in the DI Container, which automatically maps exceptions to RFC 9457 problem details responses.
        // Each handler can customize the shape of the problem details as needed, and this customization runs for all handlers.
        // Web standard RFC 9457 that defines a JSON shape for HTTP error responses, with the media type application/problem+json.
        // Instead of every endpoint inventing its own error shape, errors look like this: (type/title/status/detail/instance)
        // + traceId which is an extension, that is very useful for correlating client errors with server logs
        services.AddProblemDetails(options =>
        {
            // Interceptor Hook, runs every time a ProblemDetails is written
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
