namespace Application.Options;

// Tuning knobs for bulk import, bound from the "Import" config section (see appsettings.json).
// ChunkSize is read only by the intake splitter — queue messages carry explicit start/count, so
// changing it never affects chunks already in flight.
public sealed class ImportOptions
{
    public const string SectionName = "Import";

    // Rows per queue message / per processing transaction. Keep chunk duration comfortably under
    // the queue visibility timeout, or a slow chunk reappears and is processed twice.
    public int ChunkSize { get; set; } = 25;

    // Upper bound on data rows per uploaded file; intake rejects anything larger outright.
    public int MaxRows { get; set; } = 1000;
}
