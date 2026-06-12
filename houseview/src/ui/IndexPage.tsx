import gsap from 'gsap';
import { useEffect, useMemo, useRef, useState } from 'react';
import type { SceneModel } from '../model';
import { formatPrice, primaryType } from '../model';
import { ITEM_TYPE_COLORS, ITEM_TYPE_NAMES, type ResolvedItem } from '../types';

// "The Index" - the traditional counterpart to the dollhouse: an editorial,
// searchable list of every catalogued item, rendered as a frosted sheet over
// the 3D scene. Each row can jump back into the house, camera and all.

type SortKey = 'name' | 'price' | 'newest';

interface IndexPageProps {
  model: SceneModel;
  live: boolean;
  onBack: () => void;
  onViewItem: (id: number) => void;
}

export function IndexPage({ model, live, onBack, onViewItem }: IndexPageProps) {
  const [query, setQuery] = useState('');
  const [types, setTypes] = useState<ReadonlySet<number>>(new Set<number>());
  const [roomId, setRoomId] = useState<number | 'all'>('all');
  const [sort, setSort] = useState<SortKey>('name');
  const rootRef = useRef<HTMLDivElement>(null);
  const listRef = useRef<HTMLUListElement>(null);
  const searchRef = useRef<HTMLInputElement>(null);

  const all = useMemo(() => [...model.itemsById.values()], [model]);

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    const list = all.filter((r) => {
      if (q) {
        const hay = [r.item.name, r.item.description, r.location?.name, r.room?.name, r.owner?.name]
          .filter(Boolean)
          .join(' ')
          .toLowerCase();
        if (!hay.includes(q)) return false;
      }
      if (types.size > 0 && !r.item.itemTypes.some((t) => types.has(t))) return false;
      if (roomId !== 'all' && r.room?.id !== roomId) return false;
      return true;
    });
    switch (sort) {
      case 'price':
        return list.sort((a, b) => (b.item.price ?? -1) - (a.item.price ?? -1));
      case 'newest':
        return list.sort((a, b) => Date.parse(b.item.createdDate) - Date.parse(a.item.createdDate));
      default:
        return list.sort((a, b) => a.item.name.localeCompare(b.item.name));
    }
  }, [all, query, types, roomId, sort]);

  const totalValue = useMemo(() => filtered.reduce((sum, r) => sum + (r.item.price ?? 0), 0), [filtered]);

  const roomsWithCounts = useMemo(() => {
    const counts = new Map<number, number>();
    for (const r of all) {
      if (r.room) counts.set(r.room.id, (counts.get(r.room.id) ?? 0) + 1);
    }
    return [...model.roomsById.values()]
      .map((room) => ({ room, count: counts.get(room.id) ?? 0 }))
      .filter((e) => e.count > 0);
  }, [all, model]);

  // page entrance
  useEffect(() => {
    const root = rootRef.current;
    if (!root) return;
    gsap.fromTo(root, { autoAlpha: 0 }, { autoAlpha: 1, duration: 0.45, ease: 'power2.out' });
    gsap.fromTo(
      root.querySelectorAll('.index-reveal'),
      { y: 34, autoAlpha: 0 },
      { y: 0, autoAlpha: 1, duration: 0.7, stagger: 0.08, ease: 'power3.out', delay: 0.1 },
    );
    searchRef.current?.focus();
  }, []);

  // rows cascade in whenever the result set changes
  const filterKey = filtered.map((r) => r.item.id).join(',');
  useEffect(() => {
    const rows = listRef.current?.children;
    if (!rows || rows.length === 0) return;
    gsap.fromTo(
      rows,
      { y: 16, autoAlpha: 0 },
      { y: 0, autoAlpha: 1, duration: 0.4, stagger: 0.035, ease: 'power2.out', overwrite: 'auto' },
    );
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filterKey]);

  // "/" focuses search; Escape clears it, then leaves the page
  useEffect(() => {
    const onKey = (e: KeyboardEvent) => {
      if (e.key === '/' && document.activeElement !== searchRef.current) {
        e.preventDefault();
        searchRef.current?.focus();
      }
      if (e.key === 'Escape') {
        if (document.activeElement === searchRef.current && searchRef.current?.value) {
          setQuery('');
        } else {
          onBack();
        }
      }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onBack]);

  const toggleType = (t: number) => {
    setTypes((prev) => {
      const next = new Set(prev);
      if (next.has(t)) next.delete(t);
      else next.add(t);
      return next;
    });
  };

  const resetFilters = () => {
    setQuery('');
    setTypes(new Set<number>());
    setRoomId('all');
  };

  return (
    <div className="index-page" ref={rootRef}>
      <div className="index-inner">
        <header className="index-header index-reveal">
          <button className="index-back" onClick={onBack}>
            ← Back to the house
          </button>
          <span className={`data-badge ${live ? 'live' : 'demo'}`}>{live ? 'live data' : 'demo data'}</span>
        </header>

        <h1 className="index-title index-reveal">
          The Index
          <sup>{all.length}</sup>
        </h1>
        <p className="index-sub index-reveal">Every item in the catalogue, the traditional way.</p>

        <div className="index-search index-reveal">
          <input
            ref={searchRef}
            type="search"
            placeholder="Search items, rooms, owners…"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            aria-label="Search items"
          />
          <kbd>/</kbd>
        </div>

        <div className="index-toolbar index-reveal">
          <div className="index-filters">
            {ITEM_TYPE_NAMES.map((name, t) =>
              (model.typeCounts.get(t) ?? 0) > 0 ? (
                <button
                  key={name}
                  className={`type-chip${types.has(t) ? ' on' : ''}`}
                  style={{ '--chip': ITEM_TYPE_COLORS[t] } as React.CSSProperties}
                  onClick={() => toggleType(t)}
                >
                  <i />
                  {name}
                  <em>{model.typeCounts.get(t)}</em>
                </button>
              ) : null,
            )}
            <select
              className="room-select"
              value={roomId === 'all' ? 'all' : String(roomId)}
              onChange={(e) => setRoomId(e.target.value === 'all' ? 'all' : Number(e.target.value))}
              aria-label="Filter by room"
            >
              <option value="all">All rooms</option>
              {roomsWithCounts.map(({ room, count }) => (
                <option key={room.id} value={room.id}>
                  {room.name} ({count})
                </option>
              ))}
            </select>
          </div>
          <div className="index-sort" role="group" aria-label="Sort">
            {(
              [
                ['name', 'Name'],
                ['price', 'Price'],
                ['newest', 'Newest'],
              ] as [SortKey, string][]
            ).map(([key, label]) => (
              <button key={key} className={sort === key ? 'on' : ''} onClick={() => setSort(key)}>
                {label}
              </button>
            ))}
          </div>
        </div>

        {filtered.length === 0 ? (
          <div className="index-empty index-reveal">
            <p>Nothing matches.</p>
            <button onClick={resetFilters}>Clear the filters</button>
          </div>
        ) : (
          <ul className="index-list" ref={listRef}>
            {filtered.map((r) => (
              <IndexRow key={r.item.id} resolved={r} onView={() => onViewItem(r.item.id)} />
            ))}
          </ul>
        )}

        <footer className="index-footer">
          Showing {filtered.length} of {all.length} items
          {totalValue > 0 && <> · combined value {formatPrice(totalValue)}</>}
        </footer>
      </div>
    </div>
  );
}

function IndexRow({ resolved, onView }: { resolved: ResolvedItem; onView: () => void }) {
  const { item, location, room, owner } = resolved;
  const accent = ITEM_TYPE_COLORS[primaryType(item) % ITEM_TYPE_COLORS.length];
  return (
    <li className="index-row">
      <button className="index-row-main" onClick={onView} title="View in the house">
        <i className="row-dot" style={{ background: accent }} />
        <span className="row-name">
          <strong>{item.name}</strong>
          {item.description && <small>{item.description}</small>}
        </span>
        <span className="row-where">
          {location ? (
            <>
              {location.name}
              {room && <em> › {room.name}</em>}
            </>
          ) : (
            <em>Unassigned</em>
          )}
        </span>
        <span className="row-owner">{owner?.name ?? '—'}</span>
        <span className="row-price">{formatPrice(item.price)}</span>
        <span className="row-action">View in house ↗</span>
      </button>
    </li>
  );
}
