import gsap from 'gsap';
import { useEffect, useRef } from 'react';
import type { SceneModel } from '../model';
import { formatPrice, primaryType } from '../model';
import {
  DELETED_REASON_NAMES,
  ITEM_TYPE_COLORS,
  ITEM_TYPE_NAMES,
  type ResolvedItem,
  type RoomResponse,
  type Selection,
} from '../types';

interface DetailPanelProps {
  model: SceneModel;
  selection: Selection;
  onSelectItem: (id: number) => void;
  onClose: () => void;
}

function ItemCard({ resolved }: { resolved: ResolvedItem }) {
  const { item, location, room, owner } = resolved;
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
          <span key={t} className="chip" style={{ background: `${ITEM_TYPE_COLORS[t % ITEM_TYPE_COLORS.length]}22`, color: ITEM_TYPE_COLORS[t % ITEM_TYPE_COLORS.length] }}>
            {ITEM_TYPE_NAMES[t % ITEM_TYPE_NAMES.length]}
          </span>
        ))}
        {item.isStored && <span className="chip chip-muted">In storage</span>}
        {item.isDeleted && (
          <span className="chip chip-deleted">
            Deleted{item.reasonForDeletion != null ? ` · ${DELETED_REASON_NAMES[item.reasonForDeletion] ?? `reason ${item.reasonForDeletion}`}` : ''}
          </span>
        )}
      </div>
      <dl className="panel-facts">
        <div>
          <dt>Price</dt>
          <dd>{formatPrice(item.price)}</dd>
        </div>
        <div>
          <dt>Owner</dt>
          <dd>{owner?.name ?? '—'}</dd>
        </div>
        <div>
          <dt>Where</dt>
          <dd>{location ? `${location.name}${room ? ` · ${room.name}` : ''}` : 'Unassigned'}</dd>
        </div>
        <div>
          <dt>Catalogued</dt>
          <dd>{new Date(item.createdDate).toLocaleDateString('en-US', { year: 'numeric', month: 'short', day: 'numeric' })}</dd>
        </div>
      </dl>
    </>
  );
}

function RoomCard({
  room,
  items,
  onSelectItem,
}: {
  room: RoomResponse;
  items: ResolvedItem[];
  onSelectItem: (id: number) => void;
}) {
  return (
    <>
      <div className="panel-kicker">Room #{room.id}</div>
      <h2>{room.name}</h2>
      {room.description && <p className="panel-desc">{room.description}</p>}
      <div className="panel-section-label">
        {items.length === 0 ? 'Nothing catalogued here yet' : `${items.length} item${items.length === 1 ? '' : 's'} here`}
      </div>
      <ul className="panel-items">
        {items.map((r) => {
          const accent = ITEM_TYPE_COLORS[primaryType(r.item) % ITEM_TYPE_COLORS.length];
          return (
            <li key={r.item.id}>
              <button onClick={() => onSelectItem(r.item.id)}>
                <i style={{ background: accent }} />
                <span>{r.item.name}</span>
                <em>{formatPrice(r.item.price)}</em>
              </button>
            </li>
          );
        })}
      </ul>
    </>
  );
}

export function DetailPanel({ model, selection, onSelectItem, onClose }: DetailPanelProps) {
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
    body = resolved ? <ItemCard resolved={resolved} /> : null;
  } else {
    const room = model.roomsById.get(selection.roomId);
    if (room) {
      const placed = model.placedRooms.find((p) => p.room.id === room.id);
      const car = model.carRooms.find((c) => c.room.id === room.id);
      body = <RoomCard room={room} items={placed?.items ?? car?.items ?? []} onSelectItem={onSelectItem} />;
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
