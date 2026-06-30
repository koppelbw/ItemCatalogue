namespace ItemCatalogueAPI.ScheduledReset;

internal static partial class ScheduledResetLog
{
    [LoggerMessage(Level = LogLevel.Information, Message = "Database reset to seed baseline completed")]
    public static partial void DatabaseResetCompleted(this ILogger logger);

    [LoggerMessage(Level = LogLevel.Error, Message = "Scheduled database reset failed")]
    public static partial void DatabaseResetFailed(this ILogger logger, Exception exception);
}
