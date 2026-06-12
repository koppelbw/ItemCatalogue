# Habitat — the ItemCatalogue, spatially

An isometric 3D "dollhouse" view of the ItemCatalogue database, in the spirit of
The Sims' cutaway camera. Rooms from `dbo.Room` become furnished rooms in a
three-storey house (plus a car on the driveway for the *Glove box* and *Trunk*
rooms), and every non-deleted `dbo.Item` appears as a floating holographic
marker inside the room its location belongs to.

Built with **React + Three.js (@react-three/fiber, drei) + GSAP** and Vite.

## Running

Requires Node.js 18+.

```bash
cd houseview
npm install
npm run dev          # http://localhost:5173
```

The dev server proxies `/api` to the ItemCatalogue API at `http://localhost:5012`
(no CORS changes needed). Start the API with the SQL Server container running:

```bash
docker compose -f docker-compose.sqlserver.yml up -d
dotnet run --project ItemCatalogueAPI --launch-profile http
```

With the API up, the header shows a **live data** badge. Without it, the app
falls back to a bundled mirror of the seed data and shows **demo data** instead,
so the experience always works.

Set `VITE_API_TARGET` to point the proxy at a different API origin.

## Using it

- **Floor switcher** (right): slice the house Sims-style — Basement, Ground,
  Upper, or Attic & roof. The basement rises out of the lawn when selected.
- **Room dock** (bottom): fly the camera to any room. Badges show item counts.
- **Click a room floor** or a glowing **item marker** for the glass detail
  panel: price, owner, type chips, location breadcrumb, catalogue date.
- **Drag / scroll** to orbit and zoom; click empty lawn to deselect; ⟲ resets
  the view.

## How rooms map to 3D

Room records are matched by name (case-insensitive) onto footprints defined in
[`src/layout.ts`](src/layout.ts). Unmatched room names get generic cabins on the
west lawn; items whose location's room matched nothing (or with no location)
appear on a pallet by the curb. Item markers are shaped and coloured by the
item's first `ItemType`.
