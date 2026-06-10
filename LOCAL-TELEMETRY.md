# Local telemetry — Aspire Dashboard

View the API's OpenTelemetry traces, structured logs, and metrics locally in a web UI, with no
Application Insights / Azure dependency. The API exports OTLP to a standalone
[.NET Aspire Dashboard](https://learn.microsoft.com/dotnet/aspire/fundamentals/dashboard/standalone)
running in a container.

## Prerequisite (one time)

Install **Docker Desktop** (or Podman): https://www.docker.com/products/docker-desktop/
The dashboard is only distributed as a container image, so a container runtime is required.

## Run

```bash
# 1. Start the dashboard (from the repo root)
docker compose -f docker-compose.aspire.yml up -d

# 2. Run the API with an Aspire launch profile (sets OTEL_EXPORTER_OTLP_ENDPOINT)
dotnet run --project ItemCatalogueAPI --launch-profile "http (Aspire)"
#   (in Visual Studio / Rider: pick the "http (Aspire)" or "https (Aspire)" profile)

# 3. Generate some traffic
#    e.g. GET http://localhost:5012/api/rooms , create/update/delete a few entities

# 4. Open the dashboard
#    http://localhost:18888   (anonymous access — no login token needed)
```

In the dashboard:
- **Traces** — one span per HTTP request, with nested SQL Server dependency spans (EF Core queries).
- **Structured logs** — the `ILogger` output from every layer (e.g. `Created Room 5`, `Concurrency
  conflict updating Item 3`), each correlated to its trace via TraceId.
- **Metrics** — ASP.NET Core, HttpClient, and .NET runtime metrics.

## Stop

```bash
docker compose -f docker-compose.aspire.yml down
```

## How it's wired

- `docker-compose.aspire.yml` — runs the dashboard. UI on `18888`; OTLP gRPC ingestion published on
  host port `4317` (mapped to the container's `18889`).
- The **`http (Aspire)` / `https (Aspire)` launch profiles** set
  `OTEL_EXPORTER_OTLP_ENDPOINT=http://localhost:4317`. The default `http` / `https` profiles leave it
  unset, so ordinary runs don't attempt to export (no connection errors when the dashboard is down).
- `AddObservability()` only adds the OTLP exporter when that variable is present, and only adds the
  Azure Monitor exporter when an Application Insights connection string is configured — so the same
  build runs quietly with no collector, exports to Aspire locally, or to App Insights in Azure.
