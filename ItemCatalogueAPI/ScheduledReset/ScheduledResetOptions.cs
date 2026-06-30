namespace ItemCatalogueAPI.ScheduledReset;

public sealed class ScheduledResetOptions
{
    public const string SectionName = "ScheduledReset";

    public bool Enabled { get; init; }
    public double IntervalHours { get; init; } = 24;
}
