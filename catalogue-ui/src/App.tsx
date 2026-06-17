import { useEffect, useState, type FormEvent } from 'react'
import type { LocationResponse, PagedResponse } from './api/types'

function App() {
  // ── List state ────────────────────────────────────────────────────────────
  const [locations, setLocations] = useState<LocationResponse[]>([])
  const [loading, setLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)

  // ── Create-form state: React owns each field's value (controlled inputs) ───
  const [name, setName] = useState('')
  const [description, setDescription] = useState('')
  const [submitting, setSubmitting] = useState(false)

  // ── Edit state: which row we're editing (null = creating) + its rowVersion ──
  const [editingId, setEditingId] = useState<number | null>(null)
  const [editingRowVersion, setEditingRowVersion] = useState<string | null>(null)

  // ── Load the list. Lifted to component scope (out of useEffect) so the
  //    create handler can also call it to refresh after adding a row. ─────────
  async function loadLocations() {
    try {
      const res = await fetch('/api/locations')

      if (!res.ok) {
        throw new Error(`HTTP ${res.status}`)
      }
      
      const page = (await res.json()) as PagedResponse<LocationResponse>
      
      setLocations(page.items)
    } 
    catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to load')
    } 
    finally {
      setLoading(false)
    }
  }

  useEffect(() => {
    loadLocations()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []) // run once on mount

  // ── Create OR Update handler: PUT when editing an existing row, else POST ───
  async function handleSubmit(e: FormEvent) {
    e.preventDefault() // stop the browser's default full-page form POST
    setSubmitting(true)
    setError(null)
    try {
      if (editingId === null) {
        // CREATE (POST)
        const res = await fetch('/api/locations', {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ name, description }),
        })
        if (!res.ok) 
          throw new Error(`HTTP ${res.status}`)
      } 
      else {
        const result = await fetch(`/api/locations/${editingId}`, {
          method: 'PUT',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({
              //   The body must match UpdateLocationRequest(Id, Name, Description, RowVersion):
              id: editingId, name, description, rowVersion: editingRowVersion,
            })
        })

        if(result.status === 409)
          throw new Error('This location changed since you loaded it. Reload and try again.')

        if(!result.ok)
          throw new Error(`HTTP ${result.status}`)
      }

      cancelEdit() // clears the form AND exits edit mode
      await loadLocations() // refresh the table
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to save')
    } finally {
      setSubmitting(false)
    }
  }

  // Begin editing a row: copy its values into the form and remember its identity.
  function startEdit(loc: LocationResponse) {
    setEditingId(loc.id)
    setEditingRowVersion(loc.rowVersion)
    setName(loc.name)
    setDescription(loc.description ?? '')    
  }

  // Exit edit mode and reset the form back to a blank "create" state.
  function cancelEdit() {
    setEditingId(null)
    setEditingRowVersion(null)
    setName('')
    setDescription('')
  }

  // ── Delete handler ─────────────────────────────────────────────────────────
  async function handleDelete(id: number) {
    if (!window.confirm('Delete this location?')) return // native confirm dialog
    setError(null)
    try {
      const result = await fetch(`/api/locations/${id}`, {method: 'DELETE'})

      if(result.status === 409)
        throw new Error('Location is in use (has rooms)')

      if(!result.ok)
        throw new Error(`HTTP ${result.status}`)

      await loadLocations() // refresh so the deleted row disappears
    } catch (e) {
      setError(e instanceof Error ? e.message : 'Failed to delete')
    }
  }

  // ── Render ──────────────────────────────────────────────────────────────────
  return (
    <main>
      <h1>Locations</h1>

      {/* Create form. onSubmit fires on the submit button OR Enter in a field. */}
      <form onSubmit={handleSubmit}>
        <label>
          Name:{' '}
          {/* 👉 BLANK A1: make this controlled — bind it to `name`.
              Add  value={name}  and  onChange={(e) => setName(e.target.value)} */}
          <input value={name} onChange={(e) => setName(e.target.value)} />
        </label>{' '}
        <label>
          Description:{' '}
          {/* 👉 BLANK A2: same idea, bound to `description` / setDescription. */}
          <input value={description} onChange={(e) => setDescription(e.target.value)}/>
        </label>{' '}
        <button type="submit" disabled={submitting}>
          {submitting
            ? 'Saving…'
            : editingId === null
              ? 'Add location'
              : 'Save changes'}
        </button>{' '}
        {/* Cancel button only shows while editing (conditional render with &&). */}
        {editingId !== null && (
          <button type="button" onClick={cancelEdit}>
            Cancel
          </button>
        )}
      </form>

      {loading ? (
        <p>Loading...</p>
      ) : error ? (
        <p className="error">{error}</p>
      ) : (
        <table>
          <thead>
            <tr>
              <th>Id</th>
              <th>Name</th>
              <th>Description</th>
              <th>Rooms</th>
              <th>Actions</th>
            </tr>
          </thead>
          <tbody>
            {locations.map((loc) => (
              <tr key={loc.id}>
                <td>{loc.id}</td>
                <td>{loc.name}</td>
                <td>{loc.description}</td>
                <td>{loc.rooms.length}</td>
                <td>
                  <button onClick={() => startEdit(loc)}>Edit</button>{' '}
                  <button onClick={() => handleDelete(loc.id)}>Delete</button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      )}
    </main>
  )
}

export default App
