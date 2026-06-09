using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ItemCatalogueAPI.ExceptionHandling;

// Maps a FluentValidation ValidationException (thrown by the Application services' ValidateAndThrowAsync,
// and by the controllers' route/body id-mismatch guard) to an RFC 9457 HTTP 400 with a per-property
// "errors" dictionary. Registered first in the IExceptionHandler chain so input failures never fall
// through to the 500 catch-all. The traceId/instance enrichment from AddProblemDetails is applied
// automatically by IProblemDetailsService.
internal sealed class ValidationExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        // Collapse the flat failure list into property -> messages, the shape ValidationProblemDetails expects.
        var errors = validationException.Errors
            .GroupBy(failure => failure.PropertyName)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).Distinct().ToArray());

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ValidationProblemDetails(errors)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
            },
        });
    }
}
