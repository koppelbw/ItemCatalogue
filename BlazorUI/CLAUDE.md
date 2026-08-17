# blazor-ui — Blazor WebAssembly Standalone UI

A Blazor WASM Standalone client for the ItemCatalogue API, built as a **guided tutorial**. Branch: `blazor_ui`.

## Teaching contract — read this first

**The user writes 100% of the code.** Claude explains concepts, writes specs and acceptance criteria, reviews what the user wrote, and debugs. Claude does **not** use Write/Edit on source files in this directory — not scaffolds, not "blanks to fill in", not trivial one-liners.

What Claude *should* do freely: read files, run `dotnet build`, run `dotnet run`, curl endpoints, inspect the served output, and report findings. Reverting or deleting agent-authored code is fine when asked.

Explain with C#/.NET analogies and React analogies — the user is very experienced in C#/.NET and has since learned React (see `catalogue-ui/`), but is new to Blazor.

Curriculum and phase plan: `C:\Users\koppe\.claude\plans\your-goal-is-to-playful-seal.md`

## Stack

- Blazor **WebAssembly Standalone**, `net10.0`. Not Blazor Server, not a hosted/Web App template.
- **No component library.** Hand-rolled CSS, using Blazor CSS isolation (`.razor.css`) deliberately as a teaching target.
- No MSAL, no OIDC client library. Auth is learned via `AuthenticationStateProvider`, `CascadingAuthenticationState` / `AuthorizeView`, and `DelegatingHandler` (Phase 4).

## Project facts

- **`RootNamespace` is `BlazorUI`** while the folder and assembly are `blazor-ui`. C# identifiers can't contain hyphens, so the default would have been `blazor_ui`. `AssemblyName` was deliberately left alone — changing it would rename the generated `blazor-ui.styles.css` that `index.html` references.
- Top-level statements in `Program.cs` compile into the *global* namespace regardless of `RootNamespace`, which is why it needs an explicit `using BlazorUI;` to see `App`.
- `.razor` component namespaces are `RootNamespace` + folder path — `Layout/MainLayout.razor` → `BlazorUI.Layout.MainLayout`.

## Ports and running

| | |
|---|---|
| Blazor dev | `https://localhost:7271` / `http://localhost:5016` |
| API | **`https://localhost:7072`** |

Do **not** point the client at `http://localhost:5012` — the API calls `UseHttpsRedirection()` unconditionally, so it 307-redirects, and a 307 during CORS preflight fails in every browser.

CORS origins live in `ItemCatalogueAPI/appsettings.Development.json` under `Cors:AllowedOrigins`; both Blazor ports are already listed. The dev HTTPS cert is trusted (valid to 2027-04-20).

Run: `dotnet run --project blazor-ui` (add `--launch-profile http` for the plain-HTTP profile).

## Repo constraints that bite here

- **`CA2016` is `error`** repo-wide via `.editorconfig`. Every async method must accept and forward a `CancellationToken`, or the build fails. Design API-client methods that way from the start.
- **`wasm-tools` workload is not installed.** Plain build/run works (IL interpreter). AOT and Brotli relinking on `publish -c Release` need `dotnet workload install wasm-tools` + elevation. Deferred to Phase 6.
- No `global.json` and no `Directory.Build.props` in the repo — this project sets its own properties and inherits nothing.
- Solution is `ItemCatalogue.slnx` (XML format); `blazor-ui` sits under `/External/Presentation/`.

## Talking to the API

DTOs are **hand-written** under `Contracts/`, mirroring `Application/DTOs/`. No shared project, no OpenAPI codegen — that's a deliberate teaching choice.

- **Enums serialize as integers.** `AddControllers()` on the API is bare, with no `JsonStringEnumConverter`. Matching C# enums round-trip for free, but the wire value is `2`, not `"Good"`.
- **`byte[] RowVersion`** rides on every Update DTO for optimistic concurrency and serializes as a base64 string. Round-trip it from the response or every PUT returns 409.
- **Errors are RFC 9457 `ProblemDetails`** with an `errors: { PascalCaseProp: [messages] }` dictionary (`ItemCatalogueAPI/ExceptionHandling/`). 400 validation, 404 not found, 409 for *both* concurrency conflict and FK-in-use — disambiguate on `title`/`type`.
- List endpoints take `[FromQuery] PaginationQuery` (`page`, `pageSize`) and return `PagedResponse<T>` with `TotalCount`/`TotalPages`/`HasNext`/`HasPrevious`.
- **Rate limit is 100 req/60s** globally. Don't build polling loops.

Entity model is **Location → Floor → Room → Container → Item**. Mirror `houseview/src/types.ts`, which is accurate. Do **not** mirror `catalogue-ui/src/api/types.ts` — it's stale and thinks `Room.locationId` when rooms actually hang off floors.

## .NET 10 specifics worth not re-learning

- There is **no `blazor.boot.json`** in .NET 10. It was replaced by the `<script type="importmap">` in `index.html` plus a parallel integrity manifest.
- Assemblies ship as **Webcil-wrapped `.wasm`**, not `.dll`, because proxies frequently block bulk `.dll` downloads.
- `#[.{fingerprint}]` placeholders in `index.html` are rewritten at build (enabled by `<OverrideHtmlAssetPlaceholders>`). The fingerprint is a **content** hash — stable across rebuilds when the bytes don't change, which is what makes the assets safe to cache indefinitely.

## Coding conventions

- Use **primary constructors** for any `.cs` file that has a constructor.
- Use **file-scoped namespaces** (`namespace BlazorUI.Services.Location;`), not the curly-brace block form.

## Git

Never `git commit`. The user reviews changes in the Changes pane and commits themselves. Pause and ask when work is ready.
