using Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ItemCatalogueAPI.ExceptionHandling;

// Maps the two domain-level "you can't do that right now" exceptions to HTTP 409 Conflict:
//   - ConcurrencyConflictException: the row changed under the client (optimistic concurrency).
//   - EntityInUseException: a restricted foreign key blocks the delete.
// Both are legitimate 409s; the title differs so clients can tell them apart.
internal sealed class ConflictExceptionHandler(IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var title = exception switch
        {
            ConcurrencyConflictException => "Concurrency conflict",
            EntityInUseException => "Resource in use",
            _ => null,
        };

        if (title is null)
        {
            return false;
        }

        httpContext.Response.StatusCode = StatusCodes.Status409Conflict;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = title,
                Detail = exception.Message,
            },
        });
    }
}
