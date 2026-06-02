namespace Domain.Exceptions;

// Raised when an optimistic-concurrency check fails: the row was modified (or removed)
// by someone else between the time it was read and the time the update was saved.
// Repositories translate EF Core's DbUpdateConcurrencyException into this domain-level
// type so the Application and API layers can react (e.g. map to HTTP 409 Conflict)
// without taking a dependency on Entity Framework.
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message) : base(message)
    {
    }

    public ConcurrencyConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
