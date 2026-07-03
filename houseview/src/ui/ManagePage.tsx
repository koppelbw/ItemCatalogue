import { useMemo, useState, type ReactNode } from 'react';
import { useCatalogue } from '../api';
import { formatPrice, itemValue } from '../model';
import { mapApiError, useCollections, useRemove, useTags } from '../mutations';
import {
  CONTAINER_TYPE_NAMES,
  DOOR_KIND_NAMES,
  ITEM_TYPE_NAMES,
  ROOM_TYPE_NAMES,
  STAIR_SHAPE_NAMES,
  WALL_NAMES,
  type CollectionResponse,
  type ContainerResponse,
  type DoorResponse,
  type FloorResponse,
  type ItemResponse,
  type LocationResponse,
  type PersonResponse,
  type RoomResponse,
  type StairResponse,
  type TagResponse,
} from '../types';
import { CollectionMembers } from './CollectionMembers';
import { Explorer } from './Explorer';
import { Paginated } from './Paginated';
import { TopNav, type View } from './TopNav';
import {
  CollectionForm,
  ContainerForm,
  DeleteItemDialog,
  DoorForm,
  FloorForm,
  ItemForm,
  LocationForm,
  PersonForm,
  RoomForm,
  StairForm,
  TagForm,
  type RefData,
} from './forms/EntityForms';
import './manage.css';

type Tab = 'explore' | 'items' | 'locations' | 'floors' | 'rooms' | 'containers' | 'doors' | 'stairs' | 'persons' | 'tags' | 'collections';

const TABS: [Tab, string][] = [
  ['explore', 'Explore'],
  ['items', 'Items'],
  ['locations', 'Locations'],
  ['floors', 'Floors'],
  ['rooms', 'Rooms'],
  ['containers', 'Containers'],
  ['doors', 'Doors'],
  ['stairs', 'Stairs'],
  ['persons', 'People'],
  ['tags', 'Tags'],
  ['collections', 'Collections'],
];

// exported for the Explorer, whose "+ Add" buttons pre-fill the parent entity
export type FormState =
  | { kind: 'item'; initial?: ItemResponse; presetRoomId?: number; presetContainerId?: number }
  | { kind: 'location'; initial?: LocationResponse }
  | { kind: 'floor'; initial?: FloorResponse; presetLocationId?: number }
  | { kind: 'room'; initial?: RoomResponse; presetFloorId?: number }
  | { kind: 'container'; initial?: ContainerResponse; presetRoomId?: number; presetParentContainerId?: number }
  | { kind: 'door'; initial?: DoorResponse; presetFromRoomId?: number }
  | { kind: 'stair'; initial?: StairResponse; presetFromRoomId?: number }
  | { kind: 'person'; initial?: PersonResponse }
  | { kind: 'tag'; initial?: TagResponse }
  | { kind: 'collection'; initial?: CollectionResponse }
  | null;

interface ManagePageProps {
  onNavigate: (view: View) => void;
}

/** "12 × 10 in" style label, or an em dash when unmeasured */
function sizeCell(w: number | null, d: number | null): string {
  if (w == null || d == null) return '—';
  return `${w}″ × ${d}″`;
}

export function ManagePage({ onNavigate }: ManagePageProps) {
  const { data } = useCatalogue();
  const tagsQuery = useTags();
  const collectionsQuery = useCollections();

  const [tab, setTab] = useState<Tab>('explore');
  const [form, setForm] = useState<FormState>(null);
  const [deleteItem, setDeleteItem] = useState<ItemResponse | null>(null);
  const [members, setMembers] = useState<CollectionResponse | null>(null);
  const [banner, setBanner] = useState<string | null>(null);

  const removeLocation = useRemove('locations');
  const removeFloor = useRemove('floors');
  const removeRoom = useRemove('rooms');
  const removeContainer = useRemove('containers');
  const removeDoor = useRemove('doors');
  const removeStair = useRemove('stairs');
  const removePerson = useRemove('persons');
  // deleting the "Furniture" tag changes which items the scene renders
  const removeTag = useRemove('tags', [['tags'], ['catalogue']]);
  const removeCollection = useRemove('collections', [['collections']]);

  const live = data?.live ?? false;

  const lookups = useMemo<RefData>(
    () => ({
      locations: data?.locations ?? [],
      floors: data?.floors ?? [],
      rooms: data?.rooms ?? [],
      containers: data?.containers ?? [],
      persons: data?.persons ?? [],
    }),
    [data],
  );

  const floorsById = useMemo(() => new Map((data?.floors ?? []).map((f) => [f.id, f])), [data]);
  const roomsById = useMemo(() => new Map((data?.rooms ?? []).map((r) => [r.id, r])), [data]);
  const containersById = useMemo(() => new Map((data?.containers ?? []).map((c) => [c.id, c])), [data]);
  const locationsById = useMemo(() => new Map((data?.locations ?? []).map((l) => [l.id, l])), [data]);
  const personsById = useMemo(() => new Map((data?.persons ?? []).map((p) => [p.id, p])), [data]);

  if (!data) {
    return (
      <div className="index-page">
        <div className="index-inner">
          <p>Loading…</p>
        </div>
      </div>
    );
  }

  const runDelete = async (promise: Promise<unknown>) => {
    setBanner(null);
    try {
      await promise;
    } catch (e) {
      setBanner(mapApiError(e).banner);
    }
  };

  const confirmDelete = (label: string, run: () => Promise<unknown>) => {
    if (!window.confirm(`Delete ${label}? This cannot be undone.`)) return;
    void runDelete(run());
  };

  const itemWhere = (it: ItemResponse): string => {
    if (it.roomId != null) return roomsById.get(it.roomId)?.name ?? `room ${it.roomId}`;
    if (it.containerId != null) return containersById.get(it.containerId)?.name ?? `container ${it.containerId}`;
    return '—';
  };

  const roomWhere = (r: RoomResponse): string => {
    const floor = floorsById.get(r.floorId);
    if (!floor) return `floor ${r.floorId}`;
    const loc = locationsById.get(floor.locationId);
    return loc ? `${loc.name} › ${floor.name}` : floor.name;
  };

  const roomCell = (roomId: number | null): string => {
    if (roomId == null) return 'Outside';
    return roomsById.get(roomId)?.name ?? `room ${roomId}`;
  };

  return (
    <div className="index-page manage-page">
      <div className="index-inner">
        <header className="index-header">
          <button className="page-brand" onClick={() => onNavigate('house')}>
            Habitat
          </button>
          <div className="about-header-right">
            <TopNav current="manage" onNavigate={onNavigate} />
            <span className={`data-badge ${live ? 'live' : 'demo'}`}>{live ? 'live data' : 'demo data'}</span>
          </div>
        </header>

        <h1 className="index-title">Manage</h1>
        {!live && (
          <p className="form-banner">Editing is disabled while showing demo data — start the API to make changes.</p>
        )}
        {banner && <p className="form-banner">{banner}</p>}

        <nav className="manage-tabs">
          {TABS.map(([key, label]) => (
            <button key={key} className={tab === key ? 'on' : ''} onClick={() => setTab(key)}>
              {label}
            </button>
          ))}
        </nav>

        {tab === 'explore' && <Explorer data={data} live={live} openForm={setForm} deleteItem={setDeleteItem} />}

        {tab === 'items' && (
          <Section title="Items" onAdd={live ? () => setForm({ kind: 'item' }) : undefined}>
            <Paginated rows={data.items}>
              {(rows) => (
            <table className="manage-table">
              <thead>
                <tr><th>#</th><th>Name</th><th>Types</th><th>Value</th><th>Where</th><th>Owner</th><th></th></tr>
              </thead>
              <tbody>
                {rows.map((it) => (
                  <tr key={it.id} className={it.isDeleted ? 'row-deleted' : ''}>
                    <td>{it.id}</td>
                    <td>{it.name}{it.isDeleted && <span className="chip chip-deleted">deleted</span>}</td>
                    <td>{it.itemTypes.map((t) => ITEM_TYPE_NAMES[t] ?? t).join(', ')}</td>
                    <td>{formatPrice(itemValue(it))}</td>
                    <td>{itemWhere(it)}</td>
                    <td>{it.ownerId != null ? (personsById.get(it.ownerId)?.name ?? '—') : '—'}</td>
                    <td className="row-actions">
                      <RowActions
                        live={live}
                        onEdit={() => setForm({ kind: 'item', initial: it })}
                        onDelete={() => setDeleteItem(it)}
                        deleted={it.isDeleted}
                      />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
              )}
            </Paginated>
          </Section>
        )}

        {tab === 'locations' && (
          <Section title="Locations" onAdd={live ? () => setForm({ kind: 'location' }) : undefined}>
            <Paginated rows={data.locations}>
              {(rows) => (
            <table className="manage-table">
              <thead><tr><th>#</th><th>Name</th><th>Description</th><th>Floors</th><th>Rooms</th><th></th></tr></thead>
              <tbody>
                {rows.map((l) => {
                  const floorIds = new Set(l.floors.map((f) => f.id));
                  const roomCount = data.rooms.filter((r) => floorIds.has(r.floorId)).length;
                  return (
                    <tr key={l.id}>
                      <td>{l.id}</td>
                      <td>{l.name}</td>
                      <td>{l.description ?? '—'}</td>
                      <td>{l.floors.length}</td>
                      <td>{roomCount}</td>
                      <td className="row-actions">
                        <RowActions live={live} onEdit={() => setForm({ kind: 'location', initial: l })} onDelete={() => confirmDelete(`location "${l.name}"`, () => removeLocation.mutateAsync({ id: l.id }))} />
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
              )}
            </Paginated>
          </Section>
        )}

        {tab === 'floors' && (
          <Section title="Floors" onAdd={live ? () => setForm({ kind: 'floor' }) : undefined}>
            <Paginated rows={data.floors}>
              {(rows) => (
            <table className="manage-table">
              <thead><tr><th>#</th><th>Name</th><th>Location</th><th>Level</th><th>Ceiling</th><th>Rooms</th><th></th></tr></thead>
              <tbody>
                {rows.map((f) => (
                  <tr key={f.id}>
                    <td>{f.id}</td>
                    <td>{f.name}</td>
                    <td>{locationsById.get(f.locationId)?.name ?? f.locationId}</td>
                    <td>{f.levelIndex}</td>
                    <td>{f.ceilingHeightInches != null ? `${f.ceilingHeightInches}″` : '—'}</td>
                    <td>{data.rooms.filter((r) => r.floorId === f.id).length}</td>
                    <td className="row-actions">
                      <RowActions live={live} onEdit={() => setForm({ kind: 'floor', initial: f })} onDelete={() => confirmDelete(`floor "${f.name}"`, () => removeFloor.mutateAsync({ id: f.id }))} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
              )}
            </Paginated>
          </Section>
        )}

        {tab === 'rooms' && (
          <Section title="Rooms" onAdd={live ? () => setForm({ kind: 'room' }) : undefined}>
            <Paginated rows={data.rooms}>
              {(rows) => (
            <table className="manage-table">
              <thead><tr><th>#</th><th>Name</th><th>Floor</th><th>Type</th><th>Footprint</th><th></th></tr></thead>
              <tbody>
                {rows.map((r) => (
                  <tr key={r.id}>
                    <td>{r.id}</td>
                    <td>{r.name}</td>
                    <td>{roomWhere(r)}</td>
                    <td>{r.roomType != null ? (ROOM_TYPE_NAMES[r.roomType] ?? r.roomType) : '—'}</td>
                    <td>{sizeCell(r.widthInches, r.depthInches)}</td>
                    <td className="row-actions">
                      <RowActions live={live} onEdit={() => setForm({ kind: 'room', initial: r })} onDelete={() => confirmDelete(`room "${r.name}"`, () => removeRoom.mutateAsync({ id: r.id }))} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
              )}
            </Paginated>
          </Section>
        )}

        {tab === 'containers' && (
          <Section title="Containers" onAdd={live ? () => setForm({ kind: 'container' }) : undefined}>
            <Paginated rows={data.containers}>
              {(rows) => (
            <table className="manage-table">
              <thead><tr><th>#</th><th>Name</th><th>Type</th><th>Sits in</th><th>Size</th><th></th></tr></thead>
              <tbody>
                {rows.map((c) => (
                  <tr key={c.id}>
                    <td>{c.id}</td>
                    <td>{c.name}</td>
                    <td>{c.containerType != null ? (CONTAINER_TYPE_NAMES[c.containerType] ?? c.containerType) : '—'}</td>
                    <td>
                      {c.roomId != null
                        ? `Room · ${roomsById.get(c.roomId)?.name ?? c.roomId}`
                        : c.parentContainerId != null
                          ? `Container · ${containersById.get(c.parentContainerId)?.name ?? c.parentContainerId}`
                          : '—'}
                    </td>
                    <td>{sizeCell(c.widthInches, c.depthInches)}</td>
                    <td className="row-actions">
                      <RowActions live={live} onEdit={() => setForm({ kind: 'container', initial: c })} onDelete={() => confirmDelete(`container "${c.name}"`, () => removeContainer.mutateAsync({ id: c.id }))} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
              )}
            </Paginated>
          </Section>
        )}

        {tab === 'doors' && (
          <Section title="Doors" onAdd={live ? () => setForm({ kind: 'door' }) : undefined}>
            <Paginated rows={data.doors}>
              {(rows) => (
            <table className="manage-table">
              <thead><tr><th>#</th><th>Name</th><th>Kind</th><th>From</th><th>To</th><th>Wall</th><th>Opening</th><th></th></tr></thead>
              <tbody>
                {rows.map((d) => (
                  <tr key={d.id}>
                    <td>{d.id}</td>
                    <td>{d.name ?? '—'}</td>
                    <td>{DOOR_KIND_NAMES[d.kind] ?? d.kind}</td>
                    <td>{roomCell(d.fromRoomId)}</td>
                    <td>{roomCell(d.toRoomId)}</td>
                    <td>{WALL_NAMES[d.wall] ?? d.wall}</td>
                    <td>{`${d.widthInches}″ × ${d.heightInches}″`}</td>
                    <td className="row-actions">
                      <RowActions live={live} onEdit={() => setForm({ kind: 'door', initial: d })} onDelete={() => confirmDelete(`door "${d.name ?? d.id}"`, () => removeDoor.mutateAsync({ id: d.id }))} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
              )}
            </Paginated>
          </Section>
        )}

        {tab === 'stairs' && (
          <Section title="Stairs" onAdd={live ? () => setForm({ kind: 'stair' }) : undefined}>
            <Paginated rows={data.stairs}>
              {(rows) => (
            <table className="manage-table">
              <thead><tr><th>#</th><th>Name</th><th>Shape</th><th>From (lower)</th><th>To (upper)</th><th>Steps</th><th></th></tr></thead>
              <tbody>
                {rows.map((s) => (
                  <tr key={s.id}>
                    <td>{s.id}</td>
                    <td>{s.name ?? '—'}</td>
                    <td>{STAIR_SHAPE_NAMES[s.shape] ?? s.shape}</td>
                    <td>{roomCell(s.fromRoomId)}</td>
                    <td>{roomCell(s.toRoomId)}</td>
                    <td>{s.stepCount ?? '—'}</td>
                    <td className="row-actions">
                      <RowActions live={live} onEdit={() => setForm({ kind: 'stair', initial: s })} onDelete={() => confirmDelete(`stair "${s.name ?? s.id}"`, () => removeStair.mutateAsync({ id: s.id }))} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
              )}
            </Paginated>
          </Section>
        )}

        {tab === 'persons' && (
          <Section title="People" onAdd={live ? () => setForm({ kind: 'person' }) : undefined}>
            <Paginated rows={data.persons}>
              {(rows) => (
            <table className="manage-table">
              <thead><tr><th>#</th><th>Name</th><th></th></tr></thead>
              <tbody>
                {rows.map((p) => (
                  <tr key={p.id}>
                    <td>{p.id}</td>
                    <td>{p.name}</td>
                    <td className="row-actions">
                      <RowActions live={live} onEdit={() => setForm({ kind: 'person', initial: p })} onDelete={() => confirmDelete(`person "${p.name}"`, () => removePerson.mutateAsync({ id: p.id }))} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
              )}
            </Paginated>
          </Section>
        )}

        {tab === 'tags' && (
          <Section title="Tags" onAdd={live ? () => setForm({ kind: 'tag' }) : undefined}>
            <Paginated rows={tagsQuery.data ?? []}>
              {(rows) => (
            <table className="manage-table">
              <thead><tr><th>#</th><th>Name</th><th>Description</th><th></th></tr></thead>
              <tbody>
                {rows.map((t) => (
                  <tr key={t.id}>
                    <td>{t.id}</td>
                    <td>{t.name}</td>
                    <td>{t.description ?? '—'}</td>
                    <td className="row-actions">
                      <RowActions live={live} onEdit={() => setForm({ kind: 'tag', initial: t })} onDelete={() => confirmDelete(`tag "${t.name}"`, () => removeTag.mutateAsync({ id: t.id }))} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
              )}
            </Paginated>
          </Section>
        )}

        {tab === 'collections' && (
          <Section title="Collections" onAdd={live ? () => setForm({ kind: 'collection' }) : undefined}>
            <Paginated rows={collectionsQuery.data ?? []}>
              {(rows) => (
            <table className="manage-table">
              <thead><tr><th>#</th><th>Name</th><th>Items</th><th></th></tr></thead>
              <tbody>
                {rows.map((c) => (
                  <tr key={c.id}>
                    <td>{c.id}</td>
                    <td>{c.name}</td>
                    <td>{c.items.length}</td>
                    <td className="row-actions">
                      {live && <button className="btn btn-small" onClick={() => setMembers(c)}>Members</button>}
                      <RowActions live={live} onEdit={() => setForm({ kind: 'collection', initial: c })} onDelete={() => confirmDelete(`collection "${c.name}"`, () => removeCollection.mutateAsync({ id: c.id }))} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
              )}
            </Paginated>
          </Section>
        )}
      </div>

      {/* forms */}
      {form?.kind === 'item' && (
        <ItemForm
          initial={form.initial}
          lookups={lookups}
          presetRoomId={form.presetRoomId ?? null}
          presetContainerId={form.presetContainerId ?? null}
          onClose={() => setForm(null)}
        />
      )}
      {form?.kind === 'location' && <LocationForm initial={form.initial} onClose={() => setForm(null)} />}
      {form?.kind === 'floor' && (
        <FloorForm initial={form.initial} lookups={lookups} presetLocationId={form.presetLocationId ?? null} onClose={() => setForm(null)} />
      )}
      {form?.kind === 'room' && (
        <RoomForm initial={form.initial} lookups={lookups} presetFloorId={form.presetFloorId ?? null} onClose={() => setForm(null)} />
      )}
      {form?.kind === 'container' && (
        <ContainerForm
          initial={form.initial}
          lookups={lookups}
          presetRoomId={form.presetRoomId ?? null}
          presetParentContainerId={form.presetParentContainerId ?? null}
          onClose={() => setForm(null)}
        />
      )}
      {form?.kind === 'door' && (
        <DoorForm initial={form.initial} lookups={lookups} presetFromRoomId={form.presetFromRoomId ?? null} onClose={() => setForm(null)} />
      )}
      {form?.kind === 'stair' && (
        <StairForm initial={form.initial} lookups={lookups} presetFromRoomId={form.presetFromRoomId ?? null} onClose={() => setForm(null)} />
      )}
      {form?.kind === 'person' && <PersonForm initial={form.initial} onClose={() => setForm(null)} />}
      {form?.kind === 'tag' && <TagForm initial={form.initial} onClose={() => setForm(null)} />}
      {form?.kind === 'collection' && <CollectionForm initial={form.initial} onClose={() => setForm(null)} />}

      {deleteItem && <DeleteItemDialog item={deleteItem} onClose={() => setDeleteItem(null)} />}
      {members && <CollectionMembers collection={members} items={data.items} onClose={() => setMembers(null)} />}
    </div>
  );
}

function Section({ title, onAdd, children }: { title: string; onAdd?: () => void; children: ReactNode }) {
  return (
    <section className="manage-section">
      <div className="manage-section-head">
        <h2>{title}</h2>
        {onAdd && (
          <button className="btn btn-primary btn-small" onClick={onAdd}>
            + Add
          </button>
        )}
      </div>
      {children}
    </section>
  );
}

function RowActions({ live, onEdit, onDelete, deleted }: { live: boolean; onEdit: () => void; onDelete: () => void; deleted?: boolean }) {
  if (!live) return <span className="row-actions-muted">—</span>;
  return (
    <>
      <button className="btn btn-small" onClick={onEdit}>
        Edit
      </button>
      {!deleted && (
        <button className="btn btn-small btn-danger" onClick={onDelete}>
          Delete
        </button>
      )}
    </>
  );
}
