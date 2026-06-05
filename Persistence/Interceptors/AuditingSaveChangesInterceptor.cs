using Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace Persistence.Interceptors;

// Centralises audit stamping for every change-tracked write. Stamps CreatedDate on insert
// and LastModifiedDate on update for all IAuditable entities from a single TimeProvider read,
// so app-driven writes share one clock instead of mixing the DB clock (GETUTCDATE()) with the
// app clock (DateTime.UtcNow).
//
// Note: ExecuteUpdate/ExecuteDelete bypass the change tracker and therefore this interceptor;
// those paths (e.g. the soft delete in ItemRepository) stamp explicitly using the same injected
// TimeProvider so the single-clock guarantee still holds.
public sealed class AuditingSaveChangesInterceptor(TimeProvider timeProvider) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        ApplyAudit(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void ApplyAudit(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        // One clock read per save, shared across every entity in this unit of work.
        var now = timeProvider.GetUtcNow().UtcDateTime;

        foreach (var entry in context.ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedDate = now;
                    break;
                case EntityState.Modified:
                    entry.Entity.LastModifiedDate = now;
                    break;
            }
        }
    }
}
