# Habitat — the ItemCatalogue, spatially

An isometric 3D "dollhouse" view of the ItemCatalogue database, in the spirit of
The Sims' cutaway camera. Every `dbo.Location` is a building in the
neighborhood; the active one opens up as a cutaway dollhouse built from its
**Floors → Rooms → Containers**, laid out from the real plan geometry stored in
the database (origins, footprints, wall colors — all in inches). Doors become
openings cut into the walls, stairs climb between stories, measured containers
stand where they were measured, and every non-deleted `dbo.Item` floats as a
holographic marker in the room it ultimately lives in (walking nested container
chains up to their room).

Built with **React + Three.js (@react-three/fiber, drei) + GSAP + TanStack
Query + react-hook-form + Zod** and Vite.

## Running

Requires Node.js 18+.

```bash
cd houseview
npm install
npm run dev          # http://localhost:5173
```

The dev server proxies `/api` to the ItemCatalogue API at `http://localhost:5012`.
Start the API with the SQL Server container running:

```bash
docker compose -f docker-compose.sqlserver.yml up -d
dotnet run --project ItemCatalogueAPI --launch-profile http
```

With the API up, the header shows a **live data** badge. Without it, the app
falls back to a bundled mirror of the seed data and shows **demo data** instead,
so the experience always works. Editing is only enabled against live data.

Set `VITE_API_TARGET` to point the proxy at a different API origin.

### Demo-only mode

Two ways to force demo data and skip the API entirely (no fetch, no timeout):

- **`?demo`** in the URL — an ad-hoc dev toggle for previewing demo-mode UI.
- **`VITE_FORCE_DEMO=true`** at build time — a permanent demo-only build. The
  deployed site sets this via the `HOUSEVIEW_FORCE_DEMO` GitHub repo variable
  (see [`houseview.yml`](../.github/workflows/houseview.yml)). Because the app
  then never calls the API, the API's App Service can be **stopped** to save cost
  (`az webapp stop -n itemcatalogue-api-ufocqt -g rg-itemcatalogue`). Clear the
  variable and redeploy to go live again (`az webapp start …`).

The **public deployment runs demo-only by design** — it's on the Azure Free (F1)
tier, whose daily CPU quota a live API would exhaust, so it ships as a
self-contained showcase.

## Using it

- **Location dock** (bottom): click any Location to bring it onto the central
  stage; the others wait as satellite buildings around the lawn. ← → keys hop
  between them.
- **Floor switcher** (right): the active location's stories, straight from
  `dbo.Floor` (basements rise out of the lawn when selected; upper floors ghost).
  ↑ ↓ keys step floors.
- **Click a room floor**, a **container box**, or a glowing **item marker** for
  the glass detail panel: price, owner, type chips, the full
  *Location › Floor › Room › Container* breadcrumb, and (live) edit/delete/add
  affordances.
- **The Index** (`#/index`): the searchable, filterable, sortable flat list.
- **Manage** (`#/manage`): CRUD tables for every entity — Items, Locations,
  Floors, Rooms, Containers, Doors, Stairs, People, Tags and Collections —
  with Zod-validated forms that mirror the server's FluentValidation rules,
  rowVersion round-tripping for optimistic concurrency, and RFC 9457
  ProblemDetails mapped onto form fields.
- **Drag / scroll** to orbit and zoom; click empty lawn to deselect; ⟲ resets
  the view.

## How the database maps to 3D

- Plan scale is **1 scene unit = 24 inches**; wall heights are squashed harder
  (1 unit = 40 in) to keep the cutaway look — see [`src/layout.ts`](src/layout.ts).
- Rooms with measured geometry (`OriginX/Y`, `Width/Depth`, `Rotation`) are
  placed exactly; their stored `WallColor`/`FloorColor` win over the palette.
  Unmeasured rooms get auto-placed footprints east of the measured ones.
- Doors (`dbo.Door`) on the two cutaway walls (north/west) are cut out of the
  wall meshes with a header above; doors on the open south/east sides render as
  thresholds with jambs.
- Stairs (`dbo.Stair`) rise from their `FromRoom` position one story.
- Top-level containers with positions render as clickable boxes; nested ones
  are reachable through the detail panel.
- Items with no resolvable room appear on a pallet by the curb. Markers are
  shaped and colored by the item's first `ItemType`.
