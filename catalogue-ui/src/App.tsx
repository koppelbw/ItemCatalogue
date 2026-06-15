import { useEffect, useState } from 'react'
import type { LocationResponse, PagedResponse } from './api/types'

function App() {
  // ── State ───────────────────────────────────────────────────────────────
  // Holds the locations we fetch. Starts as an empty array, so the first render
  // shows an empty table. The <LocationResponse[]> tells TS what's in the array.
  const [locations, setLocations] = useState<LocationResponse[]>([])

  // ── Side-effect: fetch once after the first render ───────────────────────
  useEffect(() => {
    // 👉 BLANK 1: fetch the data.
    // Call fetch('/api/locations'), turn the Response into JSON, then call
    // setLocations(...) with the array of items. Remember the API returns a
    // PagedResponse<LocationResponse>, so the locations live on `.items`.
    //
    // Pattern (fetch returns Promises, so we chain .then):
    //   fetch('/api/locations')
    //     .then((res) => res.json() as Promise<PagedResponse<LocationResponse>>)
    //     .then((page) => setLocations(/* the items */))

    fetch('/api/locations')
      .then((response) => response.json() as Promise<PagedResponse<LocationResponse>>)
      .then((page) => setLocations(page.items))
  }, []) // empty deps = run exactly once, on mount

  // ── Render ──────────────────────────────────────────────────────────────
  return (
    <main>
      <h1>Locations</h1>
      <table>
        <thead>
          <tr>
            <th>Id</th>
            <th>Name</th>
            <th>Description</th>
            <th>Rooms</th>
          </tr>
        </thead>
        <tbody>
          {
            locations.map((loc) => (
              <tr key={loc.id}>
                <td>{loc.id}</td>
                <td>{loc.name}</td>
                <td>{loc.description}</td>
                <td>{loc.rooms.length}</td>
              </tr>
            ))
          }
        </tbody>
      </table>
    </main>
  )
}

export default App
