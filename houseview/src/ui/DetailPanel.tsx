import gsap from 'gsap';
import { useEffect, useRef } from 'react';
import type { SceneModel, Site } from '../model';
import { formatPrice, itemValue, primaryType } from '../model';
import {
  CONDITION_NAMES,
  CONTAINER_TYPE_NAMES,
  DELETED_REASON_NAMES,
  ITEM_TYPE_COLORS,
  ITEM_TYPE_NAMES,
  ROOM_TYPE_NAMES,
  type ContainerResponse,
  type ItemResponse,
  type ResolvedItem,
  type RoomResponse,
  type Selection,
} from '../types';

interface DetailPanelProps {
  model: SceneModel;
  selection: Selection;
  /** when false (demo data) all edit affordances are hidden */
  live: boolean;
  onSelectItem: (id: number) => void;
  onSelectContainer: (id: number) => void;
  onSelectRoom: (roomId: number) => void;
  onSelectLocation: (locationId: number) => void;
  /** fly the dollhouse to a story (by levelIndex) of the active location */
  onSelectFloor: (levelIndex: number) => void;
  onEditItem: (item: ItemResponse) => void;
  onDeleteItem: (item: ItemResponse) => void;
  onAddToRoom: (roomId: number) => void;
  onAddToContainer: (containerId: number) => void;
  onClose: () => void;
}

/** "12′ × 10′" from inches, when both sides are measured */
function footprintLabel(widthInches: number | null, depthInches: number | null): string | null {
  if (widthInches == null || depthInches == null) return null;
  const ft = (v: number) => {
    const f = v / 12;
    return Number.isInteger(f) ? String(f) : f.toFixed(1);
  };
  return `${ft(widthInches)}′ × ${ft(depthInches)}′`;
}

function ItemRow({ resolved, onSelectItem }: { resolved: ResolvedItem; onSelectItem: (id: number) => void }) {
  const accent = ITEM_TYPE_COLORS[primaryType(resolved.item) % ITEM_TYPE_COLORS.length];
  return (
    <li>
      <button onClick={() => onSelectItem(resolved.item.id)}>
        <i style={{ background: accent }} />
        <span>{resolved.item.name}</span>
        <em>{formatPrice(itemValue(resolved.item))}</em>
      </button>
    </li>
  );
}

function ItemCard({
  resolved,
  live,
  onEditItem,
  onDeleteItem,
}: {
  resolved: ResolvedItem;
  live: boolean;
  onEditItem: (item: ItemResponse) => void;
  onDeleteItem: (item: ItemResponse) => void;
}) {
  const { item, owner, breadcrumb } = resolved;
  const accent = ITEM_TYPE_COLORS[primaryType(item) % ITEM_TYPE_COLORS.length];
  return (
    <>
      <div className="panel-kicker" style={{ color: accent }}>
        Item #{item.id}
      </div>
      <h2>{item.name}</h2>
      {item.description && <p className="panel-desc">{item.description}</p>}
      <div className="chip-row">
        {item.itemTypes.map((t) => (
          <span
            key={t}
            className="chip"
            style={{ background: `${ITEM_TYPE_COLORS[t % ITEM_TYPE_COLORS.length]}22`, color: ITEM_TYPE_COLORS[t % ITEM_TYPE_COLORS.length] }}
          >
            {ITEM_TYPE_NAMES[t % ITEM_TYPE_NAMES.length]}
          </span>
        ))}
        {item.condition != null && <span className="chip chip-muted">{CONDITION_NAMES[item.condition] ?? `Condition ${item.condition}`}</span>}
        {item.isStored && <span className="chip chip-muted">In storage</span>}
        {item.isDeleted && (
          <span className="chip chip-deleted">
            Deleted{item.reasonForDeletion != null ? ` · ${DELETED_REASON_NAMES[item.reasonForDeletion] ?? `reason ${item.reasonForDeletion}`}` : ''}
          </span>
        )}
      </div>
      <dl className="panel-facts">
        <div>
          <dt>Value</dt>
          <dd>{formatPrice(itemValue(item))}</dd>
        </div>
        <div>
          <dt>Owner</dt>
          <dd>{owner?.name ?? '—'}</dd>
        </div>
        <div>
          <dt>Where</dt>
          <dd>{breadcrumb.length > 0 ? breadcrumb.join(' › ') : 'Unassigned'}</dd>
        </div>
        {item.quantity > 1 && (
          <div>
            <dt>Quantity</dt>
            <dd>{item.quantity}</dd>
          </div>
        )}
        {(item.brand || item.model) && (
          <div>
            <dt>Make</dt>
            <dd>{[item.brand, item.model].filter(Boolean).join(' · ')}</dd>
          </div>
        )}
        <div>
          <dt>Catalogued</dt>
          <dd>{new Date(item.createdDate).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' })}</dd>
        </div>
      </dl>
      {live && (
        <div className="panel-actions">
          <button className="btn btn-small" onClick={() => onEditItem(item)}>
            Edit
          </button>
          {!item.isDeleted && (
            <button className="btn btn-small btn-danger" onClick={() => onDeleteItem(item)}>
              Delete
            </button>
          )}
        </div>
      )}
    </>
  );
}

function RoomCard({
  room,
  items,
  containers,
  live,
  onSelectItem,
  onSelectContainer,
  onAddToRoom,
}: {
  room: RoomResponse;
  items: ResolvedItem[];
  containers: ContainerResponse[];
  live: boolean;
  onSelectItem: (id: number) => void;
  onSelectContainer: (id: number) => void;
  onAddToRoom: (roomId: number) => void;
}) {
  const size = footprintLabel(room.widthInches, room.depthInches);
  return (
    <>
      <div className="panel-kicker">Room #{room.id}</div>
      <h2>{room.name}</h2>
      {room.description && <p className="panel-desc">{room.description}</p>}
      <div className="chip-row">
        {room.roomType != null && <span className="chip chip-muted">{ROOM_TYPE_NAMES[room.roomType] ?? `Type ${room.roomType}`}</span>}
        {size && <span className="chip chip-muted">{size}</span>}
      </div>
      {containers.length > 0 && (
        <>
          <div className="panel-section-label">Storage in this room</div>
          <div className="chip-row">
            {containers.map((c) => (
              <button key={c.id} className="chip chip-btn" onClick={() => onSelectContainer(c.id)}>
                {c.name}
              </button>
            ))}
          </div>
        </>
      )}
      <div className="panel-section-label">
        {items.length === 0
          ? containers.length > 0
            ? 'No items outside of storage'
            : 'Nothing catalogued here yet'
          : `${items.length} item${items.length === 1 ? '' : 's'} here`}
      </div>
      <ul className="panel-items">
        {items.map((r) => (
          <ItemRow key={r.item.id} resolved={r} onSelectItem={onSelectItem} />
        ))}
      </ul>
      {live && (
        <div className="panel-actions">
          <button className="btn btn-small btn-primary" onClick={() => onAddToRoom(room.id)}>
            + Add item here
          </button>
        </div>
      )}
    </>
  );
}

function LocationCard({
  site,
  onSelectItem,
  onSelectRoom,
  onSelectFloor,
}: {
  site: Site;
  onSelectItem: (id: number) => void;
  onSelectRoom: (roomId: number) => void;
  onSelectFloor: (levelIndex: number) => void;
}) {
  const { location } = site;
  const floorsTopDown = [...site.floors].sort((a, b) => b.levelIndex - a.levelIndex);
  return (
    <>
      <div className="panel-kicker">Location #{location.id}</div>
      <h2>{site.label}</h2>
      {location.description && <p className="panel-desc">{location.description}</p>}
      <div className="chip-row">
        <span className="chip chip-muted">
          {site.floors.length} floor{site.floors.length === 1 ? '' : 's'}
        </span>
        <span className="chip chip-muted">
          {site.rooms.length} room{site.rooms.length === 1 ? '' : 's'}
        </span>
      </div>
      {floorsTopDown.map((floor) => {
        const rooms = site.rooms.filter((r) => r.floorId === floor.id);
        return (
          <div key={floor.id}>
            <button
              className="panel-section-label panel-section-btn"
              onClick={() => onSelectFloor(floor.levelIndex)}
              title={`Show ${floor.name} in the house`}
            >
              {floor.name} ⌖
            </button>
            {rooms.length > 0 && (
              <div className="chip-row">
                {rooms.map((r) => (
                  <button key={r.id} className="chip chip-btn" onClick={() => onSelectRoom(r.id)}>
                    {r.name}
                  </button>
                ))}
              </div>
            )}
          </div>
        );
      })}
      <div className="panel-section-label">
        {site.items.length === 0 ? 'Nothing catalogued here yet' : `${site.items.length} item${site.items.length === 1 ? '' : 's'} here`}
      </div>
      <ul className="panel-items">
        {site.items.map((r) => (
          <ItemRow key={r.item.id} resolved={r} onSelectItem={onSelectItem} />
        ))}
      </ul>
    </>
  );
}

function ContainerCard({
  container,
  items,
  nested,
  live,
  onSelectItem,
  onSelectContainer,
  onAddToContainer,
}: {
  container: ContainerResponse;
  items: ResolvedItem[];
  /** containers nested directly inside this one */
  nested: ContainerResponse[];
  live: boolean;
  onSelectItem: (id: number) => void;
  onSelectContainer: (id: number) => void;
  onAddToContainer: (containerId: number) => void;
}) {
  const size = footprintLabel(container.widthInches, container.depthInches);
  return (
    <>
      <div className="panel-kicker">Container #{container.id}</div>
      <h2>{container.name}</h2>
      {container.description && <p className="panel-desc">{container.description}</p>}
      <div className="chip-row">
        {container.containerType != null && (
          <span className="chip chip-muted">{CONTAINER_TYPE_NAMES[container.containerType] ?? `Type ${container.containerType}`}</span>
        )}
        {size && <span className="chip chip-muted">{size}</span>}
      </div>
      {nested.length > 0 && (
        <>
          <div className="panel-section-label">Nested inside</div>
          <div className="chip-row">
            {nested.map((c) => (
              <button key={c.id} className="chip chip-btn" onClick={() => onSelectContainer(c.id)}>
                {c.name}
              </button>
            ))}
          </div>
        </>
      )}
      <div className="panel-section-label">
        {items.length === 0 ? 'Nothing catalogued here yet' : `${items.length} item${items.length === 1 ? '' : 's'} inside`}
      </div>
      <ul className="panel-items">
        {items.map((r) => (
          <ItemRow key={r.item.id} resolved={r} onSelectItem={onSelectItem} />
        ))}
      </ul>
      {live && (
        <div className="panel-actions">
          <button className="btn btn-small btn-primary" onClick={() => onAddToContainer(container.id)}>
            + Add item here
          </button>
        </div>
      )}
    </>
  );
}

/** one step of the panel's navigation trail; clickable when `go` is set */
interface Crumb {
  label: string;
  go?: () => void;
}

interface CrumbHandlers {
  room: (roomId: number) => void;
  container: (id: number) => void;
  location: (locationId: number) => void;
}

/** Location › Floor › Room crumbs, spelling the story out only for multi-floor locations. */
function roomCrumbs(room: RoomResponse, model: SceneModel, on: CrumbHandlers): Crumb[] {
  const crumbs: Crumb[] = [];
  const floor = model.floorsById.get(room.floorId);
  const site = floor ? model.sites.find((s) => s.location.id === floor.locationId) : undefined;
  if (site) {
    crumbs.push({ label: site.label, go: () => on.location(site.location.id) });
    if (floor && site.floors.length > 1) crumbs.push({ label: floor.name });
  }
  crumbs.push({ label: room.name, go: () => on.room(room.id) });
  return crumbs;
}

/** The container chain from the outermost ancestor down to `container`, plus the room it sits in. */
function containerChain(
  container: ContainerResponse,
  model: SceneModel,
): { chain: ContainerResponse[]; room: RoomResponse | null } {
  const chain: ContainerResponse[] = [];
  let room: RoomResponse | null = null;
  let c: ContainerResponse | null = container;
  const seen = new Set<number>();
  while (c && !seen.has(c.id)) {
    seen.add(c.id);
    chain.unshift(c);
    if (c.roomId != null) {
      room = model.roomsById.get(c.roomId) ?? null;
      break;
    }
    c = c.parentContainerId != null ? (model.containersById.get(c.parentContainerId) ?? null) : null;
  }
  return { chain, room };
}

/** Full navigation trail for the current selection; the last crumb is the selection itself. */
function buildCrumbs(selection: NonNullable<Selection>, model: SceneModel, on: CrumbHandlers): Crumb[] {
  if (selection.kind === 'location') {
    const site = model.sites.find((s) => s.location.id === selection.id);
    return site ? [{ label: site.label }] : [];
  }
  if (selection.kind === 'room') {
    const room = model.roomsById.get(selection.roomId);
    if (!room) return [];
    const crumbs = roomCrumbs(room, model, on);
    crumbs[crumbs.length - 1] = { label: room.name };
    return crumbs;
  }
  if (selection.kind === 'container') {
    const container = model.containersById.get(selection.id);
    if (!container) return [];
    const { chain, room } = containerChain(container, model);
    const crumbs = room ? roomCrumbs(room, model, on) : [];
    for (const c of chain) crumbs.push({ label: c.name, go: () => on.container(c.id) });
    crumbs[crumbs.length - 1] = { label: container.name };
    return crumbs;
  }
  const resolved = model.itemsById.get(selection.id);
  if (!resolved) return [];
  const crumbs: Crumb[] = [];
  if (resolved.container) {
    const { chain, room } = containerChain(resolved.container, model);
    const home = room ?? resolved.room;
    if (home) crumbs.push(...roomCrumbs(home, model, on));
    for (const c of chain) crumbs.push({ label: c.name, go: () => on.container(c.id) });
  } else if (resolved.room) {
    crumbs.push(...roomCrumbs(resolved.room, model, on));
  } else if (resolved.location) {
    const locationId = resolved.location.id;
    const site = model.sites.find((s) => s.location.id === locationId);
    if (site) crumbs.push({ label: site.label, go: () => on.location(locationId) });
  }
  crumbs.push({ label: resolved.item.name });
  return crumbs;
}

function Breadcrumbs({ crumbs }: { crumbs: Crumb[] }) {
  if (crumbs.length < 2) return null;
  return (
    <nav className="panel-breadcrumb" aria-label="Breadcrumb">
      {crumbs.map((c, i) => {
        const last = i === crumbs.length - 1;
        return (
          <span key={`${c.label}-${i}`} className="crumb-step">
            {c.go && !last ? (
              <button className="crumb" onClick={c.go}>
                {c.label}
              </button>
            ) : (
              <span className={last ? 'crumb crumb-here' : 'crumb crumb-static'}>{c.label}</span>
            )}
            {!last && <span className="crumb-sep">›</span>}
          </span>
        );
      })}
    </nav>
  );
}

export function DetailPanel({
  model,
  selection,
  live,
  onSelectItem,
  onSelectContainer,
  onSelectRoom,
  onSelectLocation,
  onSelectFloor,
  onEditItem,
  onDeleteItem,
  onAddToRoom,
  onAddToContainer,
  onClose,
}: DetailPanelProps) {
  const ref = useRef<HTMLDivElement>(null);
  const open = selection !== null;

  useEffect(() => {
    const el = ref.current;
    if (!el) return;
    if (open) {
      // clearProps hands transform back to the stylesheet once the tween ends,
      // so the desktop translateY(-50%) centring survives breakpoint resizes
      gsap.fromTo(
        el,
        { autoAlpha: 0, x: 60 },
        { autoAlpha: 1, x: 0, duration: 0.6, ease: 'power3.out', clearProps: 'transform,translate' },
      );
    }
  }, [open, selection]);

  if (!selection) return null;

  let body: JSX.Element | null = null;
  if (selection.kind === 'item') {
    const resolved = model.itemsById.get(selection.id);
    body = resolved ? <ItemCard resolved={resolved} live={live} onEditItem={onEditItem} onDeleteItem={onDeleteItem} /> : null;
  } else if (selection.kind === 'location') {
    const site = model.sites.find((s) => s.location.id === selection.id);
    body = site ? (
      <LocationCard site={site} onSelectItem={onSelectItem} onSelectRoom={onSelectRoom} onSelectFloor={onSelectFloor} />
    ) : null;
  } else if (selection.kind === 'container') {
    const container = model.containersById.get(selection.id);
    if (container) {
      const items = [...model.itemsById.values()].filter((r) => r.container?.id === container.id);
      const nested = [...model.containersById.values()].filter((c) => c.parentContainerId === container.id);
      body = (
        <ContainerCard
          container={container}
          items={items}
          nested={nested}
          live={live}
          onSelectItem={onSelectItem}
          onSelectContainer={onSelectContainer}
          onAddToContainer={onAddToContainer}
        />
      );
    }
  } else {
    const room = model.roomsById.get(selection.roomId);
    if (room) {
      // only items sitting loose in the room; contained items are reached via their container
      const items = (model.itemsByRoom.get(room.id) ?? []).filter((r) => r.container === null);
      const containers = [...model.containersById.values()].filter((c) => c.roomId === room.id);
      body = (
        <RoomCard
          room={room}
          items={items}
          containers={containers}
          live={live}
          onSelectItem={onSelectItem}
          onSelectContainer={onSelectContainer}
          onAddToRoom={onAddToRoom}
        />
      );
    }
  }
  if (!body) return null;

  const crumbs = buildCrumbs(selection, model, {
    room: onSelectRoom,
    container: onSelectContainer,
    location: onSelectLocation,
  });

  return (
    <div className="detail-panel" ref={ref}>
      <button className="panel-close" onClick={onClose} aria-label="Close">
        ×
      </button>
      <Breadcrumbs crumbs={crumbs} />
      {body}
    </div>
  );
}
