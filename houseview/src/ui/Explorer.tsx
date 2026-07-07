import { useMemo, useState, type ReactNode } from 'react';
import { formatPrice, itemValue } from '../model';
import { mapApiError, useRemove } from '../mutations';
import {
  ACQUISITION_TYPE_NAMES,
  CONDITION_NAMES,
  CONTAINER_TYPE_NAMES,
  DELETED_REASON_NAMES,
  DOOR_KIND_NAMES,
  ITEM_TYPE_NAMES,
  ROOM_TYPE_NAMES,
  STAIR_SHAPE_NAMES,
  WALL_NAMES,
  type CatalogueData,
  type ContainerResponse,
  type FloorResponse,
  type ItemResponse,
  type LocationResponse,
  type RoomResponse,
} from '../types';
import { ItemExtras } from './ItemExtras';
import type { FormState } from './ManagePage';
import { Paginated } from './Paginated';
import { PictureHoverIcon } from './pictures/PictureHoverIcon';
import { PictureSection } from './pictures/PictureSection';

// Drill-down explorer for the Manage page: Locations → Floors → Rooms →
// Containers → Items, with the same breadcrumb trail the 3D detail panel
// builds. Soft-deleted items are hidden here (the Items tab is the audit
// view); doors and stairs surface on the room they belong to.

type ExploreNode =
  | { kind: 'root' }
  | { kind: 'location'; id: number }
  | { kind: 'floor'; id: number }
  | { kind: 'room'; id: number }
  | { kind: 'container'; id: number }
  | { kind: 'item'; id: number };

interface Crumb {
  label: string;
  to?: ExploreNode;
}

interface Maps {
  locationsById: Map<number, LocationResponse>;
  floorsById: Map<number, FloorResponse>;
  roomsById: Map<number, RoomResponse>;
  containersById: Map<number, ContainerResponse>;
  itemsById: Map<number, ItemResponse>;
  /** non-deleted items only */
  items: ItemResponse[];
  /** item id → the room its container chain bottoms out in (or its own roomId) */
  roomIdByItem: Map<number, number | null>;
  personName: (id: number | null) => string;
}

/** walk a container's parent chain up to the room that owns it (cycle-safe) */
function containerRoomId(container: ContainerResponse | undefined, containersById: Map<number, ContainerResponse>): number | null {
  const seen = new Set<number>();
  let c = container;
  while (c && !seen.has(c.id)) {
    seen.add(c.id);
    if (c.roomId != null) return c.roomId;
    c = c.parentContainerId != null ? containersById.get(c.parentContainerId) : undefined;
  }
  return null;
}

/** "12″ × 10″" style label, or an em dash when unmeasured */
function sizeLabel(w: number | null, d: number | null): string {
  if (w == null || d == null) return '—';
  return `${w}″ × ${d}″`;
}

function buildCrumbs(node: ExploreNode, maps: Maps): Crumb[] {
  const trail: Crumb[] = [{ label: 'All locations', to: { kind: 'root' } }];

  const addLocation = (id: number) => {
    const l = maps.locationsById.get(id);
    if (l) trail.push({ label: l.name, to: { kind: 'location', id } });
  };
  const addFloor = (id: number) => {
    const f = maps.floorsById.get(id);
    if (!f) return;
    addLocation(f.locationId);
    trail.push({ label: f.name, to: { kind: 'floor', id } });
  };
  const addRoom = (id: number) => {
    const r = maps.roomsById.get(id);
    if (!r) return;
    addFloor(r.floorId);
    trail.push({ label: r.name, to: { kind: 'room', id } });
  };
  const addContainerChain = (id: number) => {
    const chain: ContainerResponse[] = [];
    const seen = new Set<number>();
    let c = maps.containersById.get(id);
    while (c && !seen.has(c.id)) {
      seen.add(c.id);
      chain.unshift(c);
      c = c.parentContainerId != null ? maps.containersById.get(c.parentContainerId) : undefined;
    }
    if (chain[0]?.roomId != null) addRoom(chain[0].roomId);
    for (const cc of chain) trail.push({ label: cc.name, to: { kind: 'container', id: cc.id } });
  };

  if (node.kind === 'location') addLocation(node.id);
  else if (node.kind === 'floor') addFloor(node.id);
  else if (node.kind === 'room') addRoom(node.id);
  else if (node.kind === 'container') addContainerChain(node.id);
  else if (node.kind === 'item') {
    const it = maps.itemsById.get(node.id);
    if (it) {
      if (it.containerId != null) addContainerChain(it.containerId);
      else if (it.roomId != null) addRoom(it.roomId);
      trail.push({ label: it.name, to: { kind: 'item', id: it.id } });
    }
  }

  // the last crumb is where we are — not a link
  trail[trail.length - 1] = { label: trail[trail.length - 1].label };
  return trail;
}

interface ExplorerProps {
  data: CatalogueData;
  live: boolean;
  openForm: (form: NonNullable<FormState>) => void;
  /** opens the soft-delete (reason) dialog owned by the Manage page */
  deleteItem: (item: ItemResponse) => void;
}

export function Explorer({ data, live, openForm, deleteItem }: ExplorerProps) {
  const [node, setNode] = useState<ExploreNode>({ kind: 'root' });
  const [banner, setBanner] = useState<string | null>(null);

  const removeLocation = useRemove('locations');
  const removeFloor = useRemove('floors');
  const removeRoom = useRemove('rooms');
  const removeContainer = useRemove('containers');
  const removeDoor = useRemove('doors');
  const removeStair = useRemove('stairs');

  const maps = useMemo<Maps>(() => {
    const containersById = new Map(data.containers.map((c) => [c.id, c]));
    const personsById = new Map(data.persons.map((p) => [p.id, p]));
    const items = data.items.filter((i) => !i.isDeleted);
    const roomIdByItem = new Map<number, number | null>();
    for (const it of items) {
      roomIdByItem.set(
        it.id,
        it.roomId ?? (it.containerId != null ? containerRoomId(containersById.get(it.containerId), containersById) : null),
      );
    }
    return {
      locationsById: new Map(data.locations.map((l) => [l.id, l])),
      floorsById: new Map(data.floors.map((f) => [f.id, f])),
      roomsById: new Map(data.rooms.map((r) => [r.id, r])),
      containersById,
      itemsById: new Map(items.map((i) => [i.id, i])),
      items,
      roomIdByItem,
      personName: (id) => (id != null ? (personsById.get(id)?.name ?? '—') : '—'),
    };
  }, [data]);

  const roomItemCount = (roomId: number) => maps.items.filter((it) => maps.roomIdByItem.get(it.id) === roomId).length;
  const floorItemCount = (floorId: number) =>
    data.rooms.filter((r) => r.floorId === floorId).reduce((n, r) => n + roomItemCount(r.id), 0);
  const locationItemCount = (l: LocationResponse) => l.floors.reduce((n, f) => n + floorItemCount(f.id), 0);

  const crumbs = buildCrumbs(node, maps);
  const parentNode: ExploreNode = [...crumbs].reverse().find((c) => c.to)?.to ?? { kind: 'root' };

  /** confirm, run the delete, then step up to the parent crumb */
  const confirmDelete = (label: string, run: () => Promise<unknown>) => {
    if (!window.confirm(`Delete ${label}? This cannot be undone.`)) return;
    setBanner(null);
    run()
      .then(() => setNode(parentNode))
      .catch((e) => setBanner(mapApiError(e).banner));
  };

  /** row-level delete (doors/stairs) that stays on the current view */
  const confirmRowDelete = (label: string, run: () => Promise<unknown>) => {
    if (!window.confirm(`Delete ${label}? This cannot be undone.`)) return;
    setBanner(null);
    run().catch((e) => setBanner(mapApiError(e).banner));
  };

  const go = (to: ExploreNode) => {
    setBanner(null);
    setNode(to);
  };

  const nodeKey = `${node.kind}-${'id' in node ? node.id : 0}`;

  // NOTE: the per-level views below are invoked as plain functions, not JSX
  // components — as components their identity would change every render and
  // React would remount the subtree, resetting the pagers.

  return (
    <section className="manage-section">
      <nav className="manage-crumbs" aria-label="Breadcrumb">
        {crumbs.map((c, i) => {
          const last = i === crumbs.length - 1;
          return (
            <span key={`${c.label}-${i}`}>
              {c.to && !last ? (
                <button className="link-btn" onClick={() => go(c.to as ExploreNode)}>
                  {c.label}
                </button>
              ) : (
                <span className={last ? 'crumb-here' : ''}>{c.label}</span>
              )}
              {!last && <span className="crumb-sep"> › </span>}
            </span>
          );
        })}
      </nav>

      {banner && <p className="form-banner">{banner}</p>}

      {/* keyed by node so nested pagers reset when navigating */}
      <div key={nodeKey}>
        {node.kind === 'root' && rootView()}
        {node.kind === 'location' && locationView(node.id)}
        {node.kind === 'floor' && floorView(node.id)}
        {node.kind === 'room' && roomView(node.id)}
        {node.kind === 'container' && containerView(node.id)}
        {node.kind === 'item' && itemView(node.id)}
      </div>
    </section>
  );

  // --- per-level views -------------------------------------------------------

  function rootView() {
    return (
      <>
        <ViewHead kicker="Catalogue" title="All locations">
          {live && (
            <button className="btn btn-primary btn-small" onClick={() => openForm({ kind: 'location' })}>
              + Add location
            </button>
          )}
        </ViewHead>
        <Paginated rows={data.locations}>
          {(rows) => (
            <table className="manage-table">
              <thead>
                <tr><th>Name</th><th>Description</th><th>Floors</th><th>Rooms</th><th>Items</th></tr>
              </thead>
              <tbody>
                {rows.map((l) => {
                  const floorIds = new Set(l.floors.map((f) => f.id));
                  return (
                    <tr key={l.id}>
                      <td>
                        <button className="link-btn" onClick={() => go({ kind: 'location', id: l.id })}>{l.name}</button>{' '}
                        <PictureHoverIcon kind="locations" ownerId={l.id} live={live} />
                      </td>
                      <td>{l.description ?? '—'}</td>
                      <td>{l.floors.length}</td>
                      <td>{data.rooms.filter((r) => floorIds.has(r.floorId)).length}</td>
                      <td>{locationItemCount(l)}</td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}
        </Paginated>
      </>
    );
  }

  function locationView(id: number) {
    const location = maps.locationsById.get(id);
    if (!location) return rootView();
    const floorsTopDown = [...location.floors].sort((a, b) => b.levelIndex - a.levelIndex);
    return (
      <>
        <ViewHead kicker={`Location #${location.id}`} title={location.name} description={location.description}>
          {live && (
            <>
              <button className="btn btn-small" onClick={() => openForm({ kind: 'location', initial: location })}>Edit</button>
              <button
                className="btn btn-small btn-danger"
                onClick={() => confirmDelete(`location "${location.name}"`, () => removeLocation.mutateAsync({ id: location.id }))}
              >
                Delete
              </button>
            </>
          )}
        </ViewHead>
        <PictureSection kind="locations" ownerId={location.id} live={live} />
        <SubSection
          label={`${floorsTopDown.length} floor${floorsTopDown.length === 1 ? '' : 's'}`}
          action={
            live && (
              <button className="btn btn-primary btn-small" onClick={() => openForm({ kind: 'floor', presetLocationId: location.id })}>
                + Add floor
              </button>
            )
          }
        >
          <Paginated rows={floorsTopDown}>
            {(rows) => (
              <table className="manage-table">
                <thead>
                  <tr><th>Level</th><th>Name</th><th>Ceiling</th><th>Rooms</th><th>Items</th></tr>
                </thead>
                <tbody>
                  {rows.map((f) => (
                    <tr key={f.id}>
                      <td>{f.levelIndex}</td>
                      <td><button className="link-btn" onClick={() => go({ kind: 'floor', id: f.id })}>{f.name}</button></td>
                      <td>{f.ceilingHeightInches != null ? `${f.ceilingHeightInches}″` : '—'}</td>
                      <td>{data.rooms.filter((r) => r.floorId === f.id).length}</td>
                      <td>{floorItemCount(f.id)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </Paginated>
        </SubSection>
      </>
    );
  }

  function floorView(id: number) {
    const floor = maps.floorsById.get(id);
    if (!floor) return rootView();
    const rooms = data.rooms.filter((r) => r.floorId === floor.id);
    return (
      <>
        <ViewHead kicker={`Floor #${floor.id} · level ${floor.levelIndex}`} title={floor.name}>
          {live && (
            <>
              <button className="btn btn-small" onClick={() => openForm({ kind: 'floor', initial: floor })}>Edit</button>
              <button
                className="btn btn-small btn-danger"
                onClick={() => confirmDelete(`floor "${floor.name}"`, () => removeFloor.mutateAsync({ id: floor.id }))}
              >
                Delete
              </button>
            </>
          )}
        </ViewHead>
        <SubSection
          label={`${rooms.length} room${rooms.length === 1 ? '' : 's'}`}
          action={
            live && (
              <button className="btn btn-primary btn-small" onClick={() => openForm({ kind: 'room', presetFloorId: floor.id })}>
                + Add room
              </button>
            )
          }
        >
          <Paginated rows={rooms}>
            {(rows) => (
              <table className="manage-table">
                <thead>
                  <tr><th>Name</th><th>Type</th><th>Footprint</th><th>Containers</th><th>Items</th></tr>
                </thead>
                <tbody>
                  {rows.map((r) => (
                    <tr key={r.id}>
                      <td>
                        <button className="link-btn" onClick={() => go({ kind: 'room', id: r.id })}>{r.name}</button>{' '}
                        <PictureHoverIcon kind="rooms" ownerId={r.id} live={live} />
                      </td>
                      <td>{r.roomType != null ? (ROOM_TYPE_NAMES[r.roomType] ?? r.roomType) : '—'}</td>
                      <td>{sizeLabel(r.widthInches, r.depthInches)}</td>
                      <td>{data.containers.filter((c) => c.roomId === r.id).length}</td>
                      <td>{roomItemCount(r.id)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </Paginated>
        </SubSection>
      </>
    );
  }

  function roomView(id: number) {
    const room = maps.roomsById.get(id);
    if (!room) return rootView();
    const containers = data.containers.filter((c) => c.roomId === room.id);
    const looseItems = maps.items.filter((it) => it.roomId === room.id);
    const doors = data.doors.filter((d) => d.fromRoomId === room.id || d.toRoomId === room.id);
    const stairs = data.stairs.filter((s) => s.fromRoomId === room.id || s.toRoomId === room.id);
    const otherSide = (from: number, to: number | null) => {
      const otherId = from === room.id ? to : from;
      if (otherId == null) return 'Outside';
      return maps.roomsById.get(otherId)?.name ?? `room ${otherId}`;
    };
    return (
      <>
        <ViewHead kicker={`Room #${room.id}`} title={room.name} description={room.description}>
          {live && (
            <>
              <button className="btn btn-small" onClick={() => openForm({ kind: 'room', initial: room })}>Edit</button>
              <button
                className="btn btn-small btn-danger"
                onClick={() => confirmDelete(`room "${room.name}"`, () => removeRoom.mutateAsync({ id: room.id }))}
              >
                Delete
              </button>
            </>
          )}
        </ViewHead>
        <div className="chip-row">
          {room.roomType != null && <span className="chip chip-muted">{ROOM_TYPE_NAMES[room.roomType] ?? `Type ${room.roomType}`}</span>}
          {room.widthInches != null && room.depthInches != null && (
            <span className="chip chip-muted">{sizeLabel(room.widthInches, room.depthInches)}</span>
          )}
        </div>
        <PictureSection kind="rooms" ownerId={room.id} live={live} />

        {containerTable(
          'Storage in this room',
          containers,
          live && (
            <button className="btn btn-primary btn-small" onClick={() => openForm({ kind: 'container', presetRoomId: room.id })}>
              + Add container
            </button>
          ),
        )}
        {itemTable(
          'Items in this room',
          looseItems,
          live && (
            <button className="btn btn-primary btn-small" onClick={() => openForm({ kind: 'item', presetRoomId: room.id })}>
              + Add item
            </button>
          ),
        )}

        {(doors.length > 0 || live) && (
          <SubSection
            label="Doors"
            action={
              live && (
                <button className="btn btn-primary btn-small" onClick={() => openForm({ kind: 'door', presetFromRoomId: room.id })}>
                  + Add door
                </button>
              )
            }
          >
            {doors.length === 0 ? (
              <p className="explore-empty">No doors on this room yet.</p>
            ) : (
            <Paginated rows={doors}>
              {(rows) => (
                <table className="manage-table">
                  <thead>
                    <tr><th>Name</th><th>Kind</th><th>Wall</th><th>Connects to</th><th>Opening</th><th></th></tr>
                  </thead>
                  <tbody>
                    {rows.map((d) => (
                      <tr key={d.id}>
                        <td>{d.name ?? '—'}</td>
                        <td>{DOOR_KIND_NAMES[d.kind] ?? d.kind}</td>
                        <td>{WALL_NAMES[d.wall] ?? d.wall}</td>
                        <td>{otherSide(d.fromRoomId, d.toRoomId)}</td>
                        <td>{`${d.widthInches}″ × ${d.heightInches}″`}</td>
                        <td className="row-actions">
                          {live && (
                            <>
                              <button className="btn btn-small" onClick={() => openForm({ kind: 'door', initial: d })}>Edit</button>
                              <button
                                className="btn btn-small btn-danger"
                                onClick={() => confirmRowDelete(`door "${d.name ?? d.id}"`, () => removeDoor.mutateAsync({ id: d.id }))}
                              >
                                Delete
                              </button>
                            </>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </Paginated>
            )}
          </SubSection>
        )}

        {(stairs.length > 0 || live) && (
          <SubSection
            label="Stairs"
            action={
              live && (
                <button className="btn btn-primary btn-small" onClick={() => openForm({ kind: 'stair', presetFromRoomId: room.id })}>
                  + Add stair
                </button>
              )
            }
          >
            {stairs.length === 0 ? (
              <p className="explore-empty">No stairs connect here yet.</p>
            ) : (
            <Paginated rows={stairs}>
              {(rows) => (
                <table className="manage-table">
                  <thead>
                    <tr><th>Name</th><th>Shape</th><th>Connects to</th><th>Steps</th><th></th></tr>
                  </thead>
                  <tbody>
                    {rows.map((s) => (
                      <tr key={s.id}>
                        <td>{s.name ?? '—'}</td>
                        <td>{STAIR_SHAPE_NAMES[s.shape] ?? s.shape}</td>
                        <td>{otherSide(s.fromRoomId, s.toRoomId)}</td>
                        <td>{s.stepCount ?? '—'}</td>
                        <td className="row-actions">
                          {live && (
                            <>
                              <button className="btn btn-small" onClick={() => openForm({ kind: 'stair', initial: s })}>Edit</button>
                              <button
                                className="btn btn-small btn-danger"
                                onClick={() => confirmRowDelete(`stair "${s.name ?? s.id}"`, () => removeStair.mutateAsync({ id: s.id }))}
                              >
                                Delete
                              </button>
                            </>
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              )}
            </Paginated>
            )}
          </SubSection>
        )}
      </>
    );
  }

  function containerView(id: number) {
    const container = maps.containersById.get(id);
    if (!container) return rootView();
    const nested = data.containers.filter((c) => c.parentContainerId === container.id);
    const items = maps.items.filter((it) => it.containerId === container.id);
    return (
      <>
        <ViewHead kicker={`Container #${container.id}`} title={container.name} description={container.description}>
          {live && (
            <>
              <button className="btn btn-small" onClick={() => openForm({ kind: 'container', initial: container })}>Edit</button>
              <button
                className="btn btn-small btn-danger"
                onClick={() =>
                  confirmDelete(`container "${container.name}"`, () => removeContainer.mutateAsync({ id: container.id }))
                }
              >
                Delete
              </button>
            </>
          )}
        </ViewHead>
        <div className="chip-row">
          {container.containerType != null && (
            <span className="chip chip-muted">{CONTAINER_TYPE_NAMES[container.containerType] ?? `Type ${container.containerType}`}</span>
          )}
          {container.widthInches != null && container.depthInches != null && (
            <span className="chip chip-muted">{sizeLabel(container.widthInches, container.depthInches)}</span>
          )}
        </div>
        <PictureSection kind="containers" ownerId={container.id} live={live} />

        {containerTable(
          'Nested inside',
          nested,
          live && (
            <button
              className="btn btn-primary btn-small"
              onClick={() => openForm({ kind: 'container', presetParentContainerId: container.id })}
            >
              + Add container
            </button>
          ),
        )}
        {itemTable(
          'Items inside',
          items,
          live && (
            <button className="btn btn-primary btn-small" onClick={() => openForm({ kind: 'item', presetContainerId: container.id })}>
              + Add item
            </button>
          ),
        )}
      </>
    );
  }

  function itemView(id: number) {
    const item = maps.itemsById.get(id);
    if (!item) return rootView();
    const fmtDate = (d: string | null) =>
      d ? new Date(d).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' }) : '—';
    return (
      <>
        <ViewHead kicker={`Item #${item.id}`} title={item.name} description={item.description}>
          {live && (
            <>
              <button className="btn btn-small" onClick={() => openForm({ kind: 'item', initial: item })}>Edit</button>
              <button className="btn btn-small btn-danger" onClick={() => deleteItem(item)}>Delete</button>
            </>
          )}
        </ViewHead>
        <div className="chip-row">
          {item.itemTypes.map((t) => (
            <span key={t} className="chip chip-muted">{ITEM_TYPE_NAMES[t] ?? t}</span>
          ))}
          {item.condition != null && <span className="chip chip-muted">{CONDITION_NAMES[item.condition] ?? `Condition ${item.condition}`}</span>}
          {item.isStored && <span className="chip chip-muted">In storage</span>}
          {item.isDeleted && (
            <span className="chip chip-deleted">
              Deleted{item.reasonForDeletion != null ? ` · ${DELETED_REASON_NAMES[item.reasonForDeletion] ?? item.reasonForDeletion}` : ''}
            </span>
          )}
        </div>
        <dl className="panel-facts explore-facts">
          <div><dt>Value</dt><dd>{formatPrice(itemValue(item))}</dd></div>
          <div><dt>Owner</dt><dd>{maps.personName(item.ownerId)}</dd></div>
          <div><dt>Quantity</dt><dd>{item.quantity}</dd></div>
          {(item.brand || item.model) && (
            <div><dt>Make</dt><dd>{[item.brand, item.model].filter(Boolean).join(' · ')}</dd></div>
          )}
          {item.serialNumber && <div><dt>Serial</dt><dd>{item.serialNumber}</dd></div>}
          {item.acquisitionType != null && (
            <div><dt>Acquired</dt><dd>{ACQUISITION_TYPE_NAMES[item.acquisitionType] ?? item.acquisitionType}</dd></div>
          )}
          {item.purchasedFrom && <div><dt>Purchased from</dt><dd>{item.purchasedFrom}</dd></div>}
          {item.purchaseDate && <div><dt>Purchase date</dt><dd>{fmtDate(item.purchaseDate)}</dd></div>}
          {item.warrantyExpiryDate && <div><dt>Warranty until</dt><dd>{fmtDate(item.warrantyExpiryDate)}</dd></div>}
          <div><dt>Catalogued</dt><dd>{fmtDate(item.createdDate)}</dd></div>
        </dl>
        <PictureSection kind="items" ownerId={item.id} live={live} />
        {/* tags + event history need the live API (item-scoped endpoints) */}
        {live && <ItemExtras itemId={item.id} />}
      </>
    );
  }

  // --- shared tables ---------------------------------------------------------

  function containerTable(label: string, containers: ContainerResponse[], action?: ReactNode) {
    if (containers.length === 0 && !action) return null;
    return (
      <SubSection label={label} action={action}>
        {containers.length === 0 ? (
          <p className="explore-empty">No containers here yet.</p>
        ) : (
        <Paginated rows={containers}>
          {(rows) => (
            <table className="manage-table">
              <thead>
                <tr><th>Name</th><th>Type</th><th>Size</th><th>Items</th><th>Nested</th></tr>
              </thead>
              <tbody>
                {rows.map((c) => (
                  <tr key={c.id}>
                    <td>
                      <button className="link-btn" onClick={() => go({ kind: 'container', id: c.id })}>{c.name}</button>{' '}
                      <PictureHoverIcon kind="containers" ownerId={c.id} live={live} />
                    </td>
                    <td>{c.containerType != null ? (CONTAINER_TYPE_NAMES[c.containerType] ?? c.containerType) : '—'}</td>
                    <td>{sizeLabel(c.widthInches, c.depthInches)}</td>
                    <td>{maps.items.filter((it) => it.containerId === c.id).length}</td>
                    <td>{data.containers.filter((n) => n.parentContainerId === c.id).length}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </Paginated>
        )}
      </SubSection>
    );
  }

  function itemTable(label: string, items: ItemResponse[], action?: ReactNode) {
    return (
      <SubSection label={label} action={action}>
        {items.length === 0 ? (
          <p className="explore-empty">Nothing catalogued here yet.</p>
        ) : (
          <Paginated rows={items}>
            {(rows) => (
              <table className="manage-table">
                <thead>
                  <tr><th>Name</th><th>Types</th><th>Value</th><th>Owner</th></tr>
                </thead>
                <tbody>
                  {rows.map((it) => (
                    <tr key={it.id}>
                      <td>
                        <button className="link-btn" onClick={() => go({ kind: 'item', id: it.id })}>{it.name}</button>{' '}
                        <PictureHoverIcon kind="items" ownerId={it.id} live={live} />
                      </td>
                      <td>{it.itemTypes.map((t) => ITEM_TYPE_NAMES[t] ?? t).join(', ')}</td>
                      <td>{formatPrice(itemValue(it))}</td>
                      <td>{maps.personName(it.ownerId)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </Paginated>
        )}
      </SubSection>
    );
  }
}

function ViewHead({
  kicker,
  title,
  description,
  children,
}: {
  kicker: string;
  title: string;
  description?: string | null;
  children?: ReactNode;
}) {
  return (
    <div className="explore-head">
      <div>
        <div className="explore-kicker">{kicker}</div>
        <h2 className="explore-title">{title}</h2>
        {description && <p className="explore-desc">{description}</p>}
      </div>
      {children && <div className="explore-actions">{children}</div>}
    </div>
  );
}

function SubSection({ label, action, children }: { label: string; action?: ReactNode; children: ReactNode }) {
  return (
    <div className="explore-section">
      <div className="explore-section-head">
        <h3>{label}</h3>
        {action}
      </div>
      {children}
    </div>
  );
}
