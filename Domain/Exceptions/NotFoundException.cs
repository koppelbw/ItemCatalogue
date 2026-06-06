namespace Domain.Exceptions;

// Raised when a requested entity does not exist (e.g. a GET or UPDATE by id that finds no row).
// Services throw this domain-level type instead of the ambiguous BCL InvalidOperationException so
// the API layer can map it to HTTP 404 in one central place, without callers guessing why an
// InvalidOperationException was thrown.
public sealed class NotFoundException : Exception
{
    public NotFoundException(string message) : base(message)
    {
    }

    public NotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    // Convenience factory for the common "<Entity> with id <n> not found." message shape.
    public static NotFoundException For(string entityName, object id) =>
        new($"{entityName} with id {id} not found.");
}
