import { useEffect, useState } from 'react'
import type { LocationResponse, PagedResponse } from './api/types'

function App() {
  // ── State ───────────────────────────────────────────────────────────────
  // Holds the locations we fetch. Starts as an empty array, so the first render
  // shows an empty table. The <LocationResponse[]> tells TS what's in the array.
  const [locations, setLocations] = useState<LocationResponse[]>([]);
  const [loading, setLoading] = useState(true) //true here = fetch immediately
  const [error, setError] = useState<string | null>(null);

  // ── Side-effect: fetch once after the first render ───────────────────────
  useEffect(() => {

    // new way of doing things
    // must make async func inside useEffect since useEffect cannot be async
    async function load(){
      try {
        const res = await fetch('/api/locations');
  
        if(!res.ok){
          throw new Error(`HTTP ${res.status}`);
        }

        const page = await res.json() as PagedResponse<LocationResponse>;
        setLocations(page.items);
      }
      catch(e){
        setError(e instanceof Error ? e.message : 'Failed to load');
      }
      finally{
        setLoading(false);
      }
    }
    load()

    // old way of doing things
    // fetch('/api/locations')
    //   .then((response) => response.json() as Promise<PagedResponse<LocationResponse>>)
    //   .then((page) => setLocations(page.items))
  }, []) // empty deps = run exactly once, on mount

  // ── Render ──────────────────────────────────────────────────────────────
  return (
    <main>
      <h1>Locations</h1>
      {
      loading ? 
      (
        <p>Loading...</p>
      ) :
      error ? <p className="error">{error}</p> : 
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
      }
    </main>
  )
}

export default App
