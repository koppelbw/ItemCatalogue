namespace Domain.Enums;

// Lifecycle of a bulk-import job. Queued is written at creation; Completed is stamped when the
// final chunk marker is recorded. Processing is a derived, user-facing state (some but not all
// chunk markers exist) — it is computed at poll time and never persisted, so a crashed worker
// can't strand a job in a stale "Processing" column.
public enum ImportJobStatus
{
    Queued,
    Processing,
    Completed,
}
