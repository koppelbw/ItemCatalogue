# ItemCatalogue

A personal catalogue for the physical things you own — track **what** an item is, **where** it
lives (Location → Floor → Room → Container), **who** owns it, what it's worth, and whether it's
been disposed of and why.

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

A JSON REST API exposing CRUD for the full spatial hierarchy of a home plus the items inside it:

| Entity      | Route                        | Notes                                                        |
|-------------|------------------------------|--------------------------------------------------------------|
| `Location`  | `api/locations`              | A building/property (house, storage unit, …).                |
| `Floor`     | `api/floors`                 | A story within a location.                                   |
| `Room`      | `api/rooms`                  | A room on a floor, with real plan geometry (origin, footprint, rotation, colors — in inches). |
| `Container` | `api/containers`             | A storage container in a room; containers can nest.          |
| `Item`      | `api/items`                  | The thing you own. **Soft delete** (kept with a reason).     |
| `ItemEvent` | `api/items/{id}/events`      | Timeline events for an item.                                 |
| `Door`      | `api/doors`                  | A doorway between rooms (rendered in 3D).                    |
| `Stair`     | `api/stairs`                 | A staircase between floors (rendered in 3D).                 |
| `Person`    | `api/persons`                | An owner.                                                    |
| `Tag`       | `api/tags`                   | Free-form labels for items.                                  |
| `Collection`| `api/collections`            | Named groupings of items.                                    |
| `Picture`   | `api/{owner}/{id}/pictures`  | Photos for locations/rooms/containers/items, stored in Azure Blob Storage. |
| `Chat`      | `api/chat`                   | The AI assistant: an Anthropic tool-use agent loop over the inventory (stateless — the client sends the conversation). |

An `Item` has a type (stored as a JSON array — items can be more than one type), an optional price,
a container or room it lives in, and an owner. Geometry entities (rooms, doors, stairs, measured
containers) carry real-world dimensions in inches so the 3D UI can rebuild the house to scale.

---

## Front ends

### 🏠 Habitat (`houseview/`) — the catalogue, spatially

**Live at <https://purple-tree-02473b20f.7.azurestaticapps.net/#/index>**

An isometric 3D "dollhouse" view of the database, in the spirit of The Sims' cutaway camera. Every
`Location` is a building in the neighborhood; the active one opens as a cutaway dollhouse built
from its **Floors → Rooms → Containers**, laid out from the real plan geometry stored in the
database. Doors are cut into the walls, stairs climb between stories, and every item floats as a
holographic marker in the room it ultimately lives in. It also includes:

- **✳ Ask Habitat** — an AI assistant chat panel. Ask where things are ("where's the drill?") or
  make changes in plain English ("add a hammer to the garage toolbox") — the backend runs an
  **agentic tool-use loop** against Anthropic's Messages API, executing inventory tools through the
  same Application services the REST controllers use. Replies cite entities as `habitat://` deep
  links that fly the camera to the item, room, or container they name.
- **The Index** (`#/index`) — a searchable, filterable, sortable flat list of everything.
- **Manage** (`#/manage`) — CRUD tables for every entity, with Zod-validated forms mirroring the
  server's FluentValidation rules, `rowVersion` round-tripping for optimistic concurrency, and
  RFC 9457 ProblemDetails mapped onto form fields.
- **📷 Photos** — attach photos to any location, room, container, or item. Viewing stays quiet: a
  small camera icon next to entity names (Index rows, Manage tables, detail panels) reveals the
  cover shot in a hover popover, and clicking opens a lightbox with caption editing, a cover-photo
  star, and delete. Detail views carry the upload section — on phones a **"Take photo"** button
  opens the camera directly. Images are downscaled in the browser before upload, proxied through
  the API into **Azure Blob Storage**, and read back via short-lived SAS links.

Built with **React + Three.js (@react-three/fiber, drei) + GSAP + TanStack Query +
react-hook-form + Zod** on Vite. See [`houseview/README.md`](houseview/README.md) for how the
database maps to 3D and how to run it locally (`npm run dev` proxies to the API on port 5012;
without a live API it falls back to bundled demo data).

> The **public deployment runs demo-only by design** (a `VITE_FORCE_DEMO` build flag), so it
> never calls the API — this keeps the Free-tier App Service off the hot path. Run it locally
> against the API for the full live/editable experience.

### 📋 catalogue-ui (`catalogue-ui/`)

A conventional React + TypeScript CRUD front end, built from scratch as a guided React learning
exercise. Deployed to its own Azure Static Web App via its own pipeline.

---

## Tech stack

- **.NET 10 / C#** — ASP.NET Core Web API (controllers)
- **EF Core 10** over **SQL Server**
- **React + TypeScript + Vite** front ends (Three.js/R3F for the 3D view)
- **FluentValidation** for request validation
- **Anthropic Messages API** for the AI assistant — a raw typed `HttpClient` integration (no SDK),
  wire format and agent loop hand-rolled as a learning exercise
- **OpenTelemetry** (traces + metrics + logs) with auto-instrumentation, exported to
  **Application Insights** in Azure
- **OpenAPI** (built-in) for the API surface
- **SQL Server Database Project (`.sqlproj`)** — schema is source-controlled as raw SQL
- **xUnit v3 + NSubstitute + Shouldly + Testcontainers** for the test suite
- **Azure** — App Service (API), Azure SQL, Static Web Apps (both UIs), Blob Storage (pictures),
  deployed by **GitHub Actions**
- **Aspire Dashboard** (via Docker Compose) for viewing telemetry locally

---

## Architecture

Clean / Hexagonal (Ports & Adapters). Six projects, dependencies pointing **inward** toward the
domain:

```
ItemCatalogueAPI ──► Application ──► Domain ◄── Persistence
  (composition        (use cases,     (entities,    (EF Core adapters,
   root, HTTP)         DTOs, ports)    ports, rules)  DbContext)
                                         ▲
                                         └────────── Infrastructure
                                                      (Azure Blob Storage +
                                                       Anthropic API adapters)

Database (.sqlproj) ── owns the SQL Server schema (not EF migrations)
```

- **`Domain`** — Entities (`Location`, `Floor`, `Room`, `Container`, `Item`, `Door`, `Stair`,
  `Person`, `Tag`, `Collection`, `ItemEvent`, `Picture`), enums, domain exceptions, pagination
  primitives, and the **repository ports**. No framework dependencies.
- **`Application`** — Use-case services behind **service ports**, DTOs, manual mapping helpers,
  and FluentValidation validators. Orchestrates repositories; speaks DTOs in and out.
- **`Persistence`** — EF Core **repository adapters**, the `DbContext`, and a SaveChanges
  interceptor for auditing.
- **`Infrastructure`** — External-service adapters: the **Azure Blob Storage** adapter behind the
  picture-storage port (proxy upload, SAS-token reads), and the **Anthropic Messages API** adapter
  behind the chat port (raw typed `HttpClient`, snake_case wire JSON hand-serialized).
- **`ItemCatalogueAPI`** — The composition root: controllers, DI wiring, exception-handling
  middleware, observability setup, and the HTTP pipeline.
- **`Database`** — A SQL Server SSDT project holding the schema as raw `.sql` table definitions plus
  post-deployment seed scripts. **Schema is managed here, not via EF migrations.**

### Key architectural decisions

- **Ports & Adapters where it pays off.** The repository interface lives in `Domain`, its EF
  implementation in `Persistence` — so the core never depends on infrastructure. (The
  service-side interface is acknowledged as lighter-weight ceremony; kept for testability/mocking.)
- **Manual mapping, no AutoMapper.** Mapping between entities and DTOs is explicit static extension
  methods — fewer magic strings, easier to read, easier to learn from.
- **Schema-first via a SQL project**, not EF migrations — the database is the source of truth for
  the schema.
- **Per-layer DI modules.** Each layer owns an `AddXxx()` extension (`AddApplication`,
  `AddPersistence`, `AddInfrastructure`, `AddObservability`, `AddGlobalExceptionHandling`) so
  `Program.cs` stays a thin composition root.

### Design patterns

- **Repository pattern** with a generic base (`GenericRepository<T>`) plus per-entity adapters.
  Each repo splits **read** (`AsNoTracking`, with display includes) from **update** (tracked, no
  includes) access.
- **DTO / request–response** at the API boundary; entities never leak out of the Application layer.
- **Soft vs. hard delete strategies** — `Item` is soft-deleted (`IsDeleted` + `DeletedReason`);
  everything else is hard-deleted via `ExecuteDeleteAsync`.
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
- **Source-generated logging** (`LoggerMessage`) per layer (`ServiceLog`, `RepositoryLog`,
  `ChatLog` — the chat log lines include per-turn token counts, the feature's cost driver).
- **Agentic tool-use loop** — the AI assistant (`ChatService`) sends the conversation plus a
  six-tool catalog to the model and, while it answers `tool_use`, dispatches those calls to the
  existing Application services and feeds results back (capped iterations, output-token limits,
  conversation-size validation). Business errors return to the model as error tool-results so it
  can self-correct; the Anthropic client is a port, so the loop is unit-tested against a scripted
  fake with zero network calls.

---

## Testing

Five test projects mirror the five source layers (~430 tests): `Domain.Tests`,
`Application.Tests`, `Persistence.Tests`, `ItemCatalogueAPI.Tests`, and `Infrastructure.Tests`,
on **xUnit v3 + NSubstitute + Shouldly**.

- Domain and Application tests are pure unit tests.
- Persistence, API, and Infrastructure tests are integration tests running against real
  dependencies via **Testcontainers** (SQL Server + Azurite) — Docker required.
- A **schema drift gate** (`SchemaDriftTests`, using EfCore.SchemaCompare) fails the build if the
  EF model and the SSDT dacpac ever disagree, keeping the schema-first approach honest.

---

## CI/CD & Azure

Three independent GitHub Actions pipelines, each triggered only by its own paths:

- [`ci-cd.yml`](.github/workflows/ci-cd.yml) — builds the solution, runs the full test suite
  (Testcontainers on the Linux runner), then — on `master` only, after tests pass — publishes the
  dacpac to **Azure SQL** and the API to **Azure App Service**.
- [`houseview.yml`](.github/workflows/houseview.yml) — builds Habitat and deploys it to its own
  **Azure Static Web App** (PRs get automatic preview environments).
- [`frontend.yml`](.github/workflows/frontend.yml) — same treatment for catalogue-ui.

Telemetry from the deployed API flows to **Application Insights**.

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

## Running locally

See [`LOCAL-DATABASE.md`](LOCAL-DATABASE.md) for the full walk-through. The short version:

```bash
docker compose -f docker-compose.sqlserver.yml up -d   # SQL Server
dotnet run --project ItemCatalogueAPI --launch-profile http   # API on :5012
cd houseview && npm install && npm run dev             # Habitat on :5173
```

---

## Roadmap / TODO

- [x] **React UI** — two of them: Habitat (3D) and catalogue-ui.
- [x] **Unit & integration testing** — ~400 tests across five projects.
- [x] **Deploy to Azure** — API, database, and both UIs live via GitHub Actions CI/CD.
- [x] **Pictures** — photo upload and viewing for locations, rooms, containers, and items: Blob
      Storage behind an `IImageStorage` port, hover-thumbnail icons and an upload/camera section
      throughout Habitat, client-side downscaling, short-lived SAS reads. Storage account
      provisioned in Azure.
- [x] **AI assistant** — "Ask Habitat" chat agent that searches and edits the inventory via an
      Anthropic tool-use loop, with `habitat://` deep links into the 3D view. (API key via user
      secrets locally / `Anthropic__ApiKey` in Azure — weigh the cost exposure before enabling it
      on the public demo.)
- [ ] **Authentication & authorization** — secure the API (likely Entra ID / OIDC) and scope data
      to the owner.
- [ ] **Infrastructure as code** — Terraform for the Azure resources (currently provisioned
      manually).
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
