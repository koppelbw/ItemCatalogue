import gsap from 'gsap';
import { useEffect, useRef } from 'react';
import type { SceneModel, Site } from '../model';
import { formatPrice, itemValue, primaryType } from '../model';
import {
  CONDITION_NAMES,
  DELETED_REASON_NAMES,
  ITEM_TYPE_COLORS,
  ITEM_TYPE_NAMES,
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
  onEditItem: (item: ItemResponse) => void;
  onDeleteItem: (item: ItemResponse) => void;
  onAddToRoom: (roomId: number) => void;
  onAddToContainer: (containerId: number) => void;
  onClose: () => void;
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
  locationName,
  live,
  onSelectItem,
  onAddToRoom,
}: {
  room: RoomResponse;
  items: ResolvedItem[];
  locationName: string | null;
  live: boolean;
  onSelectItem: (id: number) => void;
  onAddToRoom: (roomId: number) => void;
}) {
  return (
    <>
      <div className="panel-kicker">Room #{room.id}</div>
      <h2>{room.name}</h2>
      {locationName && <p className="panel-desc">in {locationName}</p>}
      {room.description && <p className="panel-desc">{room.description}</p>}
      <div className="panel-section-label">
        {items.length === 0 ? 'Nothing catalogued here yet' : `${items.length} item${items.length === 1 ? '' : 's'} here`}
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

function LocationCard({ site, onSelectItem }: { site: Site; onSelectItem: (id: number) => void }) {
  const { location } = site;
  return (
    <>
      <div className="panel-kicker">Location #{location.id}</div>
      <h2>{site.label}</h2>
      {location.description && <p className="panel-desc">{location.description}</p>}
      <div className="chip-row">
        <span className="chip chip-muted">
          {site.rooms.length} room{site.rooms.length === 1 ? '' : 's'}
        </span>
      </div>
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
  trail,
  items,
  live,
  onSelectItem,
  onAddToContainer,
}: {
  container: ContainerResponse;
  trail: string[];
  items: ResolvedItem[];
  live: boolean;
  onSelectItem: (id: number) => void;
  onAddToContainer: (containerId: number) => void;
}) {
  return (
    <>
      <div className="panel-kicker">Container #{container.id}</div>
      <h2>{container.name}</h2>
      {trail.length > 0 && <p className="panel-desc">{trail.join(' › ')}</p>}
      {container.description && <p className="panel-desc">{container.description}</p>}
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

/** Names from the location down to (but not including) the given container. */
function containerTrail(container: ContainerResponse, model: SceneModel): string[] {
  const chain: string[] = [];
  let roomId: number | null = null;
  let c: ContainerResponse | null = container;
  const seen = new Set<number>();
  while (c && !seen.has(c.id)) {
    seen.add(c.id);
    if (c.id !== container.id) chain.push(c.name);
    if (c.roomId != null) {
      roomId = c.roomId;
      break;
    }
    c = c.parentContainerId != null ? (model.containersById.get(c.parentContainerId) ?? null) : null;
  }
  chain.reverse();
  const parts: string[] = [];
  if (roomId != null) {
    const room = model.roomsById.get(roomId);
    if (room) {
      const site = model.sites.find((s) => s.location.id === room.locationId);
      if (site) parts.push(site.label);
      parts.push(room.name);
    }
  }
  parts.push(...chain);
  return parts;
}

export function DetailPanel({
  model,
  selection,
  live,
  onSelectItem,
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
      gsap.fromTo(el, { autoAlpha: 0, x: 60 }, { autoAlpha: 1, x: 0, duration: 0.6, ease: 'power3.out' });
    }
  }, [open, selection]);

  if (!selection) return null;

  let body: JSX.Element | null = null;
  if (selection.kind === 'item') {
    const resolved = model.itemsById.get(selection.id);
    body = resolved ? <ItemCard resolved={resolved} live={live} onEditItem={onEditItem} onDeleteItem={onDeleteItem} /> : null;
  } else if (selection.kind === 'location') {
    const site = model.sites.find((s) => s.location.id === selection.id);
    body = site ? <LocationCard site={site} onSelectItem={onSelectItem} /> : null;
  } else if (selection.kind === 'container') {
    const container = model.containersById.get(selection.id);
    if (container) {
      const items = [...model.itemsById.values()].filter((r) => r.container?.id === container.id);
      body = (
        <ContainerCard
          container={container}
          trail={containerTrail(container, model)}
          items={items}
          live={live}
          onSelectItem={onSelectItem}
          onAddToContainer={onAddToContainer}
        />
      );
    }
  } else {
    const room = model.roomsById.get(selection.roomId);
    if (room) {
      const items = model.itemsByRoom.get(room.id) ?? [];
      const site = model.sites.find((s) => s.location.id === room.locationId);
      body = (
        <RoomCard
          room={room}
          items={items}
          locationName={site?.label ?? null}
          live={live}
          onSelectItem={onSelectItem}
          onAddToRoom={onAddToRoom}
        />
      );
    }
  }
  if (!body) return null;

  return (
    <div className="detail-panel" ref={ref}>
      <button className="panel-close" onClick={onClose} aria-label="Close">
        ×
      </button>
      {body}
    </div>
  );
}
