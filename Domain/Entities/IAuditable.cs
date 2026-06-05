namespace Domain.Entities;

// Shared audit shape for persisted entities. The AuditingSaveChangesInterceptor stamps
// CreatedDate on insert and LastModifiedDate on update from a single clock (TimeProvider),
// so every app-driven write uses one consistent time source instead of mixing the DB clock
// (GETUTCDATE()) with the app clock (DateTime.UtcNow).
public interface IAuditable
{
    DateTime CreatedDate { get; set; }

    DateTime? LastModifiedDate { get; set; }
}
