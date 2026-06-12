using Microsoft.SqlServer.Dac;

namespace ItemCatalogue.TestSupport;

// Shared by the integration-test fixtures (Persistence.Tests, ItemCatalogueAPI.Tests via a linked
// compile). Publishes the SSDT dacpac — the schema source of truth — into a target database so the
// tests run against the exact schema deployed to production, instead of an EF EnsureCreated
// approximation that can silently drift from the real schema.
public static class DacpacDeployer
{
    // Publishes the dacpac into <targetDatabase> on the server reached by <adminConnectionString>
    // (which targets master). Creates the database from scratch; also runs the dacpac's
    // post-deployment seed scripts, so callers that need empty tables should clear them afterwards.
    public static void Publish(string adminConnectionString, string targetDatabase)
    {
        using var package = DacPackage.Load(LocateDacpac());
        var services = new DacServices(adminConnectionString);
        services.Deploy(
            package,
            targetDatabase,
            upgradeExisting: true,
            // The SSDT project targets SQL Server 2025 (Sql170) but the test containers are 2022; the
            // schema uses no 2025-only features, so allow the cross-version publish.
            options: new DacDeployOptions { CreateNewDatabase = true, AllowIncompatiblePlatform = true });
    }

    // Walks up from the test output directory to the repo, where the Database SSDT project lives, and
    // returns its built dacpac. The dacpac is produced by building Database.sqlproj (Visual Studio, or
    // 'msbuild Database\Database.sqlproj') — it is not built by 'dotnet test'.
    private static string LocateDacpac()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var sqlproj = Path.Combine(dir.FullName, "Database", "Database.sqlproj");
            if (!File.Exists(sqlproj)) continue;

            foreach (var configuration in new[] { "Debug", "Release" })
            {
                var dacpac = Path.Combine(dir.FullName, "Database", "bin", configuration, "Database.dacpac");
                if (File.Exists(dacpac)) return dacpac;
            }

            throw new InvalidOperationException(
                $"Found '{sqlproj}' but no built dacpac under Database\\bin\\(Debug|Release). " +
                "Build the Database project in Visual Studio, or run 'msbuild Database\\Database.sqlproj'.");
        }

        throw new InvalidOperationException(
            $"Could not locate the Database SSDT project starting from '{AppContext.BaseDirectory}'.");
    }
}
