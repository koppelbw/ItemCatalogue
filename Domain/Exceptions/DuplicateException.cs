namespace Domain.Exceptions;

// Raised when an insert or update violates a unique constraint/index (e.g. two Tags with the same
// Name). Repositories translate the provider-level unique-violation error into this domain-level type
// so the Application and API layers can react (e.g. map to HTTP 409 Conflict) without taking a
// dependency on Entity Framework or the database provider.
public sealed class DuplicateException : Exception
{
    public DuplicateException(string message) : base(message)
    {
    }

    public DuplicateException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
