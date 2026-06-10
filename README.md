# ItemCatalogue

A personal catalogue for the physical things you own — track **what** an item is, **where** it's
stored (Room → Location), **who** owns it, what it's worth, and whether it's been disposed of and
why.

ItemCatalogue is a small, real-world domain used as a vehicle for practicing **Clean / Hexagonal
Architecture** and modern ASP.NET Core. It is a personal-scale application (a home inventory), so
the engineering choices are deliberately "production patterns at a hobby scale" rather than
big-system over-engineering.

---

## ⚠️ About this codebase

This project is being built **with AI tooling** (Claude Code) as a hands-on learning exercise.

You will notice the code is **heavily — sometimes excessively — commented**. That is intentional.
The comments are not there for a production audience; they are a **learning log and a record of
*why* each decision was made**

---

## What it does

A JSON REST API exposing CRUD for four entities:

| Entity     | Route            | Notes                                                         |
|------------|------------------|---------------------------------------------------------------|
| `Item`     | `api/items`      | The thing you own. **Soft delete** (kept with a reason).      |
| `Room`     | `api/rooms`      | A physical room. **Hard delete.**                             |
| `Location` | `api/locations`  | A storage spot within a room. **Hard delete.**                |
| `Person`   | `api/persons`    | An owner. **Hard delete.**                                     |

An `Item` has a type (stored as a JSON array — items can be more than one type), an optional price,
a storage location, and an owner.

---

## Tech stack

- **.NET 10 / C#** — ASP.NET Core Web API (controllers)
- **EF Core 10** over **SQL Server**
- **FluentValidation** for request validation
- **OpenTelemetry** (traces + metrics + logs) with auto-instrumentation
- **OpenAPI** (built-in) for the API surface
- **SQL Server Database Project (`.sqlproj`)** — schema is source-controlled as raw SQL
- **Aspire Dashboard** (via Docker Compose) for viewing telemetry locally

---

## Architecture

Clean / Hexagonal (Ports & Adapters). Five projects, dependencies pointing **inward** toward the
domain:

```
ItemCatalogueAPI  ──► Application ──► Domain ◄── Persistence
   (composition         (use cases,      (entities,        (EF Core adapters,
    root, HTTP)          DTOs, ports)     ports, rules)      DbContext)

Database (.sqlproj) ── owns the SQL Server schema (not EF migrations)
```

- **`Domain`** — Entities (`Item`, `Room`, `Location`, `Person`), enums (`ItemType`,
  `DeletedReason`), domain exceptions, pagination primitives, and the **repository ports**
  (`IItemRepository`, …). No framework dependencies.
- **`Application`** — Use-case services (`ItemService`, …) behind **service ports**
  (`IItemService`, …), DTOs, manual mapping helpers, and FluentValidation validators. Orchestrates
  repositories; speaks DTOs in and out.
- **`Persistence`** — EF Core **repository adapters**, the `DbContext`, and a SaveChanges
  interceptor for auditing.
- **`ItemCatalogueAPI`** — The composition root: controllers, DI wiring, exception-handling
  middleware, observability setup, and the HTTP pipeline.
- **`Database`** — A SQL Server SSDT project holding the schema as raw `.sql` table definitions plus
  post-deployment seed scripts. **Schema is managed here, not via EF migrations.** (Builds in Visual
  Studio / MSBuild, not the `dotnet` CLI.)

### Key architectural decisions

- **Ports & Adapters where it pays off.** The repository interface lives in `Domain`, its EF
  implementation in `Persistence` — so the core never depends on infrastructure. (The
  service-side interface is acknowledged as lighter-weight ceremony; kept for testability/mocking.)
- **Manual mapping, no AutoMapper.** Mapping between entities and DTOs is explicit static extension
  methods — fewer magic strings, easier to read, easier to learn from.
- **Schema-first via a SQL project**, not EF migrations — the database is the source of truth for
  the schema.
- **Per-layer DI modules.** Each layer owns an `AddXxx()` extension (`AddApplication`,
  `AddPersistence`, `AddObservability`, `AddGlobalExceptionHandling`) so `Program.cs` stays a thin
  composition root.

### Design patterns

- **Repository pattern** with a generic base (`GenericRepository<T>`) plus per-entity adapters.
  Each repo splits **read** (`AsNoTracking`, with display includes) from **update** (tracked, no
  includes) access.
- **DTO / request–response** at the API boundary; entities never leak out of the Application layer.
- **Soft vs. hard delete strategies** — `Item` is soft-deleted (`IsDeleted` + `DeletedReason`);
  `Room`/`Location`/`Person` are hard-deleted via `ExecuteDeleteAsync`.
- **Optimistic concurrency** via a SQL `ROWVERSION` token that round-trips through the DTOs; a stale
  write raises `ConcurrencyConflictException` → **HTTP 409** instead of silently clobbering.
- **Offset pagination envelope** — every list endpoint is paginated (`PageRequest` → `PagedResult`),
  clamped through a single factory; there is **no unbounded list query**. (Offset chosen over keyset
  for random-access simplicity at personal-catalog scale; keyset is the documented upgrade path.)
- **Centralised exception handling** — an `IExceptionHandler` chain maps domain exceptions to
  RFC 9457 `ProblemDetails`: not-found → 404, validation → 400, concurrency/in-use → 409. Replaces
  per-controller try/catch.
- **Graceful FK-restrict handling** — a blocked hard delete (`SqlException 547`) is translated to
  `EntityInUseException` → **HTTP 409** rather than a raw 500.
- **Auditing interceptor** — a SaveChanges interceptor stamps `CreatedDate` / `LastModifiedDate`
  on `IAuditable` entities.
- **Source-generated logging** (`LoggerMessage`) per layer (`ServiceLog`, `RepositoryLog`).

---

## Observability

Vendor-neutral OpenTelemetry instrumentation; only the **exporter** is environment-specific:

- **Azure Monitor / Application Insights** when a connection string is configured.
- **OTLP** when `OTEL_EXPORTER_OTLP_ENDPOINT` is set (local Aspire dashboard / collector).
- **Neither** → telemetry is collected but not exported (quiet local runs).

A local **Aspire Dashboard** is provided via `docker-compose.aspire.yml`; see
[`LOCAL-TELEMETRY.md`](LOCAL-TELEMETRY.md) for the walk-through. Use the `http (Aspire)` /
`https (Aspire)` launch profiles to point telemetry at it.


---

## Roadmap / TODO

- [ ] **React UI** — a front-end SPA for browsing and managing the catalogue.
- [ ] **Authentication & authorization** — secure the API (likely Entra ID / OIDC) and scope data
      to the owner.
- [ ] **Unit testing** — service- and repository-level tests (the interfaces and manual mapping are
      already test-friendly).
- [ ] **Deploy to Azure** — host the API + database in Azure, wire up Application Insights.
- [ ] **Bulk upload via Excel template** — upload an Excel sheet of items processed asynchronously:
  - Excel file lands in **Azure Blob Storage**
  - a message is dropped on a **Blob / Storage Queue**
  - an **Azure Function** picks it up, parses the template, validates, and inserts the items
- [ ] Custom business spans / domain metrics (ActivitySource + Meter) — currently deferred; the
      first pass is logs + auto-instrumentation only.
- [ ] Apply the concurrency token to the soft-delete path (the `ExecuteUpdateAsync` path currently
      bypasses it).

---

## Why this exists

This is a learning project. The goal is to internalise Clean Architecture trade-offs, modern
ASP.NET Core, EF Core, observability, and Azure-native async processing — and to use AI tooling
effectively while still understanding every decision.
