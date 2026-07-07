namespace Domain.Entities;

// One bulk-import upload. Deliberately carries NO mutable status/progress columns: progress is
// derived at read time from the ImportChunk marker rows (status is a projection of facts, not a
// flag), so concurrent chunk processors never contend on this row.
public class ImportJob : IEntity, IAuditable
{
    public int Id { get; set; }

    public string FileName { get; set; } = string.Empty;

    // Data rows found in the uploaded file (excluding the header).
    public int TotalRows { get; set; }

    // Rows rejected synchronously at intake (parse failures, unknown room/owner names) and
    // therefore never enqueued. TotalRows == RejectedAtIntake + EnqueuedRows.
    public int RejectedAtIntake { get; set; }

    public int EnqueuedRows { get; set; }

    // How many chunk markers must exist for the job to be complete.
    public int TotalChunks { get; set; }

    // Intake rejections as serialized ImportRowError JSON, surfaced in the status response.
    public string? IntakeErrorsJson { get; set; }

    public List<ImportChunk> Chunks { get; set; } = [];

    public DateTime CreatedDate { get; set; }

    public DateTime? LastModifiedDate { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
