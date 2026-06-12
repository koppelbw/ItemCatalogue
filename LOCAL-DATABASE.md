# Local database — SQL Server in Docker

Run the API against a real SQL Server locally, with the schema provisioned from the **SSDT project**
(`Database/Database.sqlproj` → `Database.dacpac`), not from EF. The SSDT project is the schema source
of truth — there are no EF migrations, and EF maps onto the existing schema (database-first).

A real engine (rather than the EF in-memory provider) is used on purpose: this project relies on
SQL Server-specific behaviour — FK-restrict (error 547) translation for graceful deletes, the
`ROWVERSION` concurrency token, `ExecuteUpdate`/`ExecuteDelete` soft-deletes — none of which the
in-memory provider enforces.

## Prerequisite (one time)

Install **Docker Desktop**: https://www.docker.com/products/docker-desktop/
SqlPackage is installed automatically by the script if missing (`dotnet tool install -g microsoft.sqlpackage`).

## Run

```powershell
# From the repo root — starts the container AND publishes the schema from the dacpac.
./init-db.ps1

# Then run the API normally (Development env uses the container connection string automatically).
dotnet run --project ItemCatalogueAPI
```

`init-db.ps1` is idempotent — re-run it any time to update or reset the schema. Useful flags:

```powershell
./init-db.ps1 -Rebuild      # rebuild the dacpac from the .sqlproj first (needs MSBuild / SSDT)
./init-db.ps1 -SaPassword '...' -Port 1433 -DatabaseName ItemCatalogue
```

## Stop

```powershell
docker compose -f docker-compose.sqlserver.yml down       # stop, keep the data
docker compose -f docker-compose.sqlserver.yml down -v     # stop AND wipe the database volume
```

## How it's wired

- **`docker-compose.sqlserver.yml`** — SQL Server 2022 on host port `1433`, data persisted in the
  `itemcatalogue-mssql-data` volume. Image is pinned to the same tag the integration tests use
  (`SqlServerFixture`), so dev engine == test engine == prod.
- **`init-db.ps1`** — `up -d`, waits for the engine, then `SqlPackage /Action:Publish` of the dacpac
  (creates the DB, tables, FKs, and runs the `PostDeploymentScripts/Seed_*.sql` seed data).
- **`appsettings.Development.json`** — overrides `ConnectionStrings:local` to point at the container
  (SQL auth, database `ItemCatalogue`). The base `appsettings.json` value is unchanged.
- Credentials (`sa` / `LocalDev!Pass123`) are **local-dev throwaway values** — never reuse them.
