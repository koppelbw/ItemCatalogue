using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ItemCatalogueAPI.ExceptionHandling;

// Catch-all fallback in the IExceptionHandler chain. Registered last, so it only runs for exceptions no specific handler claimed.
// Logs the full exception once at the pipeline edge and returns a sanitized HTTP 500: internal detail is exposed only in Development, never in
// Production (where stack traces / SQL text must not leak to clients).
internal sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger, IHostEnvironment environment) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        logger.LogError(
            exception,
            "Unhandled exception while processing {Method} {Path}",
            httpContext.Request.Method,
            httpContext.Request.Path);

        httpContext.Response.StatusCode = StatusCodes.Status500InternalServerError;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Detail = environment.IsDevelopment()
                    ? exception.ToString()
                    : "An unexpected error occurred. Please contact support if the problem persists.",
            },
        });
    }
}
