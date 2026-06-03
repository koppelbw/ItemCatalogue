namespace Domain.Exceptions;

// Raised when a hard delete is blocked because the entity is still referenced by another
// record through a restricted foreign key (e.g. deleting a Room that still has Locations).
// Repositories translate the provider-level FK-violation error into this domain-level type
// so the Application and API layers can react (e.g. map to HTTP 409 Conflict) without taking
// a dependency on Entity Framework or the database provider.
public sealed class EntityInUseException : Exception
{
    public EntityInUseException(string message) : base(message)
    {
    }

    public EntityInUseException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
