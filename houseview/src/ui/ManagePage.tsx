import { useMemo, useState, type ReactNode } from 'react';
import { useCatalogue } from '../api';
import { formatPrice, itemValue } from '../model';
import { mapApiError, useCollections, useRemove, useTags } from '../mutations';
import {
  DELETED_REASON_NAMES,
  ITEM_TYPE_NAMES,
  type CollectionResponse,
  type ContainerResponse,
  type ItemResponse,
  type LocationResponse,
  type PersonResponse,
  type RoomResponse,
  type TagResponse,
} from '../types';
import { CollectionMembers } from './CollectionMembers';
import {
  CollectionForm,
  ContainerForm,
  DeleteItemDialog,
  ItemForm,
  LocationForm,
  PersonForm,
  RoomForm,
  TagForm,
  type RefData,
} from './forms/EntityForms';
import './manage.css';

type Tab = 'items' | 'rooms' | 'locations' | 'containers' | 'persons' | 'tags' | 'collections';

const TABS: [Tab, string][] = [
  ['items', 'Items'],
  ['rooms', 'Rooms'],
  ['locations', 'Locations'],
  ['containers', 'Containers'],
  ['persons', 'People'],
  ['tags', 'Tags'],
  ['collections', 'Collections'],
];

type FormState =
  | { kind: 'item'; initial?: ItemResponse }
  | { kind: 'room'; initial?: RoomResponse }
  | { kind: 'location'; initial?: LocationResponse }
  | { kind: 'container'; initial?: ContainerResponse }
  | { kind: 'person'; initial?: PersonResponse }
  | { kind: 'tag'; initial?: TagResponse }
  | { kind: 'collection'; initial?: CollectionResponse }
  | null;

interface ManagePageProps {
  onBack: () => void;
}

export function ManagePage({ onBack }: ManagePageProps) {
  const { data } = useCatalogue();
  const tagsQuery = useTags();
  const collectionsQuery = useCollections();

  const [tab, setTab] = useState<Tab>('items');
  const [form, setForm] = useState<FormState>(null);
  const [deleteItem, setDeleteItem] = useState<ItemResponse | null>(null);
  const [members, setMembers] = useState<CollectionResponse | null>(null);
  const [banner, setBanner] = useState<string | null>(null);

  const removeRoom = useRemove('rooms');
  const removeLocation = useRemove('locations');
  const removeContainer = useRemove('containers');
  const removePerson = useRemove('persons');
  const removeTag = useRemove('tags', [['tags']]);
  const removeCollection = useRemove('collections', [['collections']]);

  const live = data?.live ?? false;

  const lookups = useMemo<RefData>(
    () => ({
      locations: data?.locations ?? [],
      rooms: data?.rooms ?? [],
      containers: data?.containers ?? [],
      persons: data?.persons ?? [],
    }),
    [data],
  );

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

  return (
    <div className="index-page manage-page">
      <div className="index-inner">
        <header className="index-header">
          <button className="index-back" onClick={onBack}>
            ← Back to the neighbourhood
          </button>
          <span className={`data-badge ${live ? 'live' : 'demo'}`}>{live ? 'live data' : 'demo data'}</span>
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

        {tab === 'items' && (
          <Section title="Items" onAdd={live ? () => setForm({ kind: 'item' }) : undefined}>
            <table className="manage-table">
              <thead>
                <tr><th>#</th><th>Name</th><th>Types</th><th>Value</th><th>Where</th><th>Owner</th><th></th></tr>
              </thead>
              <tbody>
                {data.items.map((it) => (
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
          </Section>
        )}

        {tab === 'rooms' && (
          <Section title="Rooms" onAdd={live ? () => setForm({ kind: 'room' }) : undefined}>
            <table className="manage-table">
              <thead><tr><th>#</th><th>Name</th><th>Location</th><th>Description</th><th></th></tr></thead>
              <tbody>
                {data.rooms.map((r) => (
                  <tr key={r.id}>
                    <td>{r.id}</td>
                    <td>{r.name}</td>
                    <td>{locationsById.get(r.locationId)?.name ?? r.locationId}</td>
                    <td>{r.description ?? '—'}</td>
                    <td className="row-actions">
                      <RowActions live={live} onEdit={() => setForm({ kind: 'room', initial: r })} onDelete={() => confirmDelete(`room "${r.name}"`, () => removeRoom.mutateAsync({ id: r.id }))} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Section>
        )}

        {tab === 'locations' && (
          <Section title="Locations" onAdd={live ? () => setForm({ kind: 'location' }) : undefined}>
            <table className="manage-table">
              <thead><tr><th>#</th><th>Name</th><th>Description</th><th>Rooms</th><th></th></tr></thead>
              <tbody>
                {data.locations.map((l) => (
                  <tr key={l.id}>
                    <td>{l.id}</td>
                    <td>{l.name}</td>
                    <td>{l.description ?? '—'}</td>
                    <td>{data.rooms.filter((r) => r.locationId === l.id).length}</td>
                    <td className="row-actions">
                      <RowActions live={live} onEdit={() => setForm({ kind: 'location', initial: l })} onDelete={() => confirmDelete(`location "${l.name}"`, () => removeLocation.mutateAsync({ id: l.id }))} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Section>
        )}

        {tab === 'containers' && (
          <Section title="Containers" onAdd={live ? () => setForm({ kind: 'container' }) : undefined}>
            <table className="manage-table">
              <thead><tr><th>#</th><th>Name</th><th>Sits in</th><th>Description</th><th></th></tr></thead>
              <tbody>
                {data.containers.map((c) => (
                  <tr key={c.id}>
                    <td>{c.id}</td>
                    <td>{c.name}</td>
                    <td>
                      {c.roomId != null
                        ? `Room · ${roomsById.get(c.roomId)?.name ?? c.roomId}`
                        : c.parentContainerId != null
                          ? `Container · ${containersById.get(c.parentContainerId)?.name ?? c.parentContainerId}`
                          : '—'}
                    </td>
                    <td>{c.description ?? '—'}</td>
                    <td className="row-actions">
                      <RowActions live={live} onEdit={() => setForm({ kind: 'container', initial: c })} onDelete={() => confirmDelete(`container "${c.name}"`, () => removeContainer.mutateAsync({ id: c.id }))} />
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </Section>
        )}

        {tab === 'persons' && (
          <Section title="People" onAdd={live ? () => setForm({ kind: 'person' }) : undefined}>
            <table className="manage-table">
              <thead><tr><th>#</th><th>Name</th><th></th></tr></thead>
              <tbody>
                {data.persons.map((p) => (
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
          </Section>
        )}

        {tab === 'tags' && (
          <Section title="Tags" onAdd={live ? () => setForm({ kind: 'tag' }) : undefined}>
            <table className="manage-table">
              <thead><tr><th>#</th><th>Name</th><th>Description</th><th></th></tr></thead>
              <tbody>
                {(tagsQuery.data ?? []).map((t) => (
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
          </Section>
        )}

        {tab === 'collections' && (
          <Section title="Collections" onAdd={live ? () => setForm({ kind: 'collection' }) : undefined}>
            <table className="manage-table">
              <thead><tr><th>#</th><th>Name</th><th>Items</th><th></th></tr></thead>
              <tbody>
                {(collectionsQuery.data ?? []).map((c) => (
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
          </Section>
        )}
      </div>

      {/* forms */}
      {form?.kind === 'item' && <ItemForm initial={form.initial} lookups={lookups} onClose={() => setForm(null)} />}
      {form?.kind === 'room' && <RoomForm initial={form.initial} lookups={lookups} onClose={() => setForm(null)} />}
      {form?.kind === 'location' && <LocationForm initial={form.initial} onClose={() => setForm(null)} />}
      {form?.kind === 'container' && <ContainerForm initial={form.initial} lookups={lookups} onClose={() => setForm(null)} />}
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
