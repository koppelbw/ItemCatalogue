using EfSchemaCompare;
using Microsoft.Extensions.Time.Testing;
using Persistence.Tests.Infrastructure;
using Shouldly;

namespace Persistence.Tests;

// Drift gate between the two hand-maintained definitions of the database schema:
//   1. the SSDT project (Database/, the source of truth, deployed in production via the dacpac), and
//   2. the EF Core model (ItemCatalogueDbContext.OnModelCreating).
// Nothing in the build forces those two to agree, so without this test they can silently diverge
// (a column type widened in SSDT but not in the Fluent config, a new nullable column, a changed FK
// delete rule, etc.) and the mismatch only shows up at runtime against production.
//
// The fixture publishes the same dacpac into the container, so comparing the EF model against that
// database answers exactly one question: "does the Fluent configuration still describe the schema
// SSDT produces?" When it fails, GetAllErrors lists the precise object/column that diverged. This
// test never rewrites either side — it only detects. Decide which side is authoritative (usually
// SSDT) and bring the other into line.
[Collection(SqlServerCollection.Name)]
public class SchemaDriftTests(SqlServerFixture fixture)
{
    [Fact]
    public void EfModel_matches_DacpacDeployedSchema()
    {
        using var context = fixture.CreateContext(new FakeTimeProvider());

        // The SSDT tables name their PK/FK constraints and FK indexes after EF Core's conventions
        // (PK_<Table>, FK_<Table>_<Principal>_<Col>, IX_<Table>_<Col>), so those now match and are
        // checked by the test. The only residual mismatch is default-value SQL *formatting*: SQL
        // Server normalises the stored text (GETUTCDATE() -> getutcdate(), 0 -> stored as 0 while EF
        // emits CAST(0 AS bit)) so the strings differ though the value is identical. That single,
        // irreducible category is waived; default *presence* drift still surfaces as a ValueGenerated
        // mismatch, which is NOT ignored.
        var config = new CompareEfSqlConfig();
        config.AddIgnoreCompareLog(new CompareLog(
            CompareType.MatchAnything, CompareState.Different, null, CompareAttributes.DefaultValueSql, null, null));

        // CompareEfWithDb reverse-engineers the live database (the dacpac-published schema) over the
        // context's own connection and diffs it against the EF model.
        var comparer = new CompareEfSql(config);
        var hasErrors = comparer.CompareEfWithDb(context);

        // On failure, GetAllErrors is the human-readable list of every mismatch.
        hasErrors.ShouldBeFalse(comparer.GetAllErrors);
    }
}
