namespace ItemCatalogueAPI.RateLimiting;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    // Policy name for the bulk-import upload endpoint (see AddApiRateLimiting). One import is a
    // single heavy request — a whole file parsed and dispatched — so it gets a much stricter
    // window than the global per-request limit.
    public const string ImportPolicy = "imports";

    public int PermitLimit { get; init; } = 100;
    public int WindowSeconds { get; init; } = 60;
    public int QueueLimit { get; init; } = 0;

    public int ImportPermitLimit { get; init; } = 5;
    public int ImportWindowSeconds { get; init; } = 60;
}
