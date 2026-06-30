using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Persistence;

namespace ItemCatalogueAPI.ScheduledReset;

// Restores the database to its seed baseline: clears every table reachable through the API, then
// re-runs the same idempotent MERGE scripts the SSDT post-deployment step uses on every publish
// (Database/PostDeploymentScripts) — one source of truth for "what the baseline looks like".
// Used by ScheduledResetBackgroundService to keep a public demo environment from accumulating
// vandalism or junk data from anonymous visitors between resets.
public sealed class DatabaseResetService(ItemCatalogueDbContext dbContext, ILogger<DatabaseResetService> logger)
{
    private static readonly string ClearDataSql = """
        DELETE FROM dbo.ItemTag;
        DELETE FROM dbo.CollectionItem;
        DELETE FROM dbo.ItemEvent;
        DELETE FROM dbo.Item;
        DELETE FROM dbo.Door;
        DELETE FROM dbo.Stair;

        WHILE EXISTS (SELECT 1 FROM dbo.Container)
        BEGIN
            DELETE FROM dbo.Container
            WHERE Id NOT IN (SELECT ParentContainerId FROM dbo.Container WHERE ParentContainerId IS NOT NULL);
        END

        DELETE FROM dbo.Room;
        DELETE FROM dbo.Floor;
        DELETE FROM dbo.Location;
        DELETE FROM dbo.Person;
        DELETE FROM dbo.Tag;
        DELETE FROM dbo.Collection;
        """;

    // Forward FK order — mirrors Database/PostDeploymentScripts/Script.PostDeployment.sql.
    private static readonly string[] SeedScriptResourceNames =
    [
        "Seed_Location.sql",
        "Seed_Floor.sql",
        "Seed_Room.sql",
        "Seed_Container.sql",
        "Seed_Door.sql",
        "Seed_Stair.sql",
        "Seed_Person.sql",
        "Seed_Item.sql",
        "Seed_Tag.sql",
        "Seed_Collection.sql",
    ];

    public async Task ResetAsync(CancellationToken cancellationToken)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        await dbContext.Database.ExecuteSqlRawAsync(ClearDataSql, cancellationToken);

        foreach (var resourceName in SeedScriptResourceNames)
        {
            var script = ReadSeedScript(resourceName);
            await dbContext.Database.ExecuteSqlRawAsync(script, cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);

        logger.DatabaseResetCompleted();
    }

    private static string ReadSeedScript(string resourceName)
    {
        var assembly = Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException($"Embedded seed script '{resourceName}' was not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}
