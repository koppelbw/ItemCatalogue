import gsap from 'gsap';
import { useEffect, useMemo, useRef, useState } from 'react';
import type { SceneModel } from '../model';
import { formatPrice, itemValue, primaryType } from '../model';
import { useCollections, useTagItemIds, useTags } from '../mutations';
import { ITEM_TYPE_COLORS, ITEM_TYPE_NAMES, type ResolvedItem } from '../types';
import { Paginated } from './Paginated';
import { PictureHoverIcon } from './pictures/PictureHoverIcon';
import { SocialFooter } from './SocialFooter';
import { TopNav, type View } from './TopNav';

// "The Index" - the traditional counterpart to the dollhouse: an editorial,
// searchable list of every catalogued item, rendered as a frosted sheet over
// the 3D scene. Each row can jump back into the house, camera and all.

type SortKey = 'name' | 'price-desc' | 'price-asc' | 'newest' | 'oldest';
type StoredFilter = 'all' | 'stored' | 'out';

const SORT_OPTIONS: [SortKey, string][] = [
  ['name', 'Name'],
  ['price-desc', 'Value ↓'],
  ['price-asc', 'Value ↑'],
  ['newest', 'Newest'],
  ['oldest', 'Oldest'],
];

const STORED_OPTIONS: [StoredFilter, string][] = [
  ['all', 'All'],
  ['stored', 'In storage'],
  ['out', 'In use'],
];

interface IndexPageProps {
  model: SceneModel;
  live: boolean;
  onNavigate: (view: View) => void;
  onViewItem: (id: number) => void;
}

interface FilterOption {
  id: number;
  name: string;
  count: number;
}

/** The room a container ultimately sits in, walked up through nested parents. */
function containerRoomName(id: number, model: SceneModel): string | null {
  let c = model.containersById.get(id) ?? null;
  const seen = new Set<number>();
  while (c && c.roomId == null && c.parentContainerId != null && !seen.has(c.id)) {
    seen.add(c.id);
    c = model.containersById.get(c.parentContainerId) ?? null;
  }
  return c && c.roomId != null ? (model.roomsById.get(c.roomId)?.name ?? null) : null;
}

export function IndexPage({ model, live, onNavigate, onViewItem }: IndexPageProps) {
  const [query, setQuery] = useState('');
  const [types, setTypes] = useState<ReadonlySet<number>>(new Set<number>());
  // the dropdown chain, top-down; each level's options cascade from the ones above
  const [locationId, setLocationId] = useState<number | 'all' | 'none'>('all');
  const [floorId, setFloorId] = useState<number | 'all'>('all');
  const [roomId, setRoomId] = useState<number | 'all'>('all');
  const [containerId, setContainerId] = useState<number | 'all'>('all');
  const [collectionId, setCollectionId] = useState<number | 'all'>('all');
  const [tagId, setTagId] = useState<number | 'all'>('all');
  const [ownerId, setOwnerId] = useState<number | 'all' | 'none'>('all');
  const [stored, setStored] = useState<StoredFilter>('all');
  const [sort, setSort] = useState<SortKey>('name');
  const rootRef = useRef<HTMLDivElement>(null);
  const searchRef = useRef<HTMLInputElement>(null);

  const all = useMemo(() => [...model.itemsById.values()], [model]);

  // tag + collection membership come from their own endpoints, not the catalogue;
  // when the API is unreachable (demo data) those two dropdowns simply stay hidden
  const tags = useTags().data;
  const collections = useCollections().data;
  const tagMembers = useTagItemIds(tags).data ?? null;

  const collectionMembers = useMemo(() => {
    const map = new Map<number, ReadonlySet<number>>();
    for (const c of collections ?? []) map.set(c.id, new Set(c.items.map((i) => i.itemId)));
    return map;
  }, [collections]);

  // every container in the item's parent chain, so filtering by a wardrobe also
  // finds items in boxes nested inside it
  const chainByItem = useMemo(() => {
    const map = new Map<number, ReadonlySet<number>>();
    for (const r of all) {
      const chain = new Set<number>();
      let c = r.container;
      while (c && !chain.has(c.id)) {
        chain.add(c.id);
        c = c.parentContainerId != null ? (model.containersById.get(c.parentContainerId) ?? null) : null;
      }
      map.set(r.item.id, chain);
    }
    return map;
  }, [all, model]);

  // one predicate per dropdown; options for each level are computed from the
  // items that survive every predicate ABOVE it, and the result list applies all
  const chain = useMemo(
    () => ({
      location: (r: ResolvedItem) =>
        locationId === 'all' ? true : locationId === 'none' ? r.location === null : r.location?.id === locationId,
      floor: (r: ResolvedItem) => floorId === 'all' || r.floor?.id === floorId,
      room: (r: ResolvedItem) => roomId === 'all' || r.room?.id === roomId,
      container: (r: ResolvedItem) => containerId === 'all' || (chainByItem.get(r.item.id)?.has(containerId) ?? false),
      collection: (r: ResolvedItem) => collectionId === 'all' || (collectionMembers.get(collectionId)?.has(r.item.id) ?? false),
      tag: (r: ResolvedItem) => tagId === 'all' || (tagMembers?.get(tagId)?.has(r.item.id) ?? false),
      owner: (r: ResolvedItem) => (ownerId === 'all' ? true : ownerId === 'none' ? r.owner === null : r.owner?.id === ownerId),
    }),
    [locationId, floorId, roomId, containerId, collectionId, tagId, ownerId, chainByItem, collectionMembers, tagMembers],
  );

  const filtered = useMemo(() => {
    const q = query.trim().toLowerCase();
    const list = all.filter((r) => {
      if (q) {
        const hay = [r.item.name, r.item.description, r.item.brand, r.owner?.name, ...r.breadcrumb]
          .filter(Boolean)
          .join(' ')
          .toLowerCase();
        if (!hay.includes(q)) return false;
      }
      if (types.size > 0 && !r.item.itemTypes.some((t) => types.has(t))) return false;
      if (!chain.location(r) || !chain.floor(r) || !chain.room(r) || !chain.container(r)) return false;
      if (!chain.collection(r) || !chain.tag(r) || !chain.owner(r)) return false;
      if (stored === 'stored' && !r.item.isStored) return false;
      if (stored === 'out' && r.item.isStored) return false;
      return true;
    });
    switch (sort) {
      case 'price-desc':
        return list.sort((a, b) => (itemValue(b.item) ?? -Infinity) - (itemValue(a.item) ?? -Infinity));
      case 'price-asc':
        return list.sort((a, b) => (itemValue(a.item) ?? Infinity) - (itemValue(b.item) ?? Infinity));
      case 'newest':
        return list.sort((a, b) => Date.parse(b.item.createdDate) - Date.parse(a.item.createdDate));
      case 'oldest':
        return list.sort((a, b) => Date.parse(a.item.createdDate) - Date.parse(b.item.createdDate));
      default:
        return list.sort((a, b) => a.item.name.localeCompare(b.item.name));
    }
  }, [all, query, types, chain, stored, sort]);

  const totalValue = useMemo(() => filtered.reduce((sum, r) => sum + (itemValue(r.item) ?? 0), 0), [filtered]);

  // dropdown options with live counts, narrowed progressively down the chain so
  // every list only offers choices that are viable under the filters above it
  const dropdowns = useMemo(() => {
    const byName = (a: FilterOption, b: FilterOption) => a.name.localeCompare(b.name);
    // when no location is pinned, floor/room names repeat across buildings — prefix them
    const manyLocations = locationId === 'all' && model.sites.length > 1;

    let pool = all;
    const locCounts = new Map<number, number>();
    let unassigned = 0;
    for (const r of pool) {
      if (r.location) locCounts.set(r.location.id, (locCounts.get(r.location.id) ?? 0) + 1);
      else unassigned += 1;
    }
    const locations: FilterOption[] = model.sites
      .map((s) => ({ id: s.location.id, name: s.label, count: locCounts.get(s.location.id) ?? 0 }))
      .filter((o) => o.count > 0);

    pool = pool.filter(chain.location);
    const floorAgg = new Map<number, FilterOption>();
    for (const r of pool) {
      if (!r.floor) continue;
      const entry = floorAgg.get(r.floor.id) ?? {
        id: r.floor.id,
        name: manyLocations && r.location ? `${r.location.name} · ${r.floor.name}` : r.floor.name,
        count: 0,
      };
      entry.count += 1;
      floorAgg.set(r.floor.id, entry);
    }
    const floors = [...floorAgg.values()].sort(byName);

    pool = pool.filter(chain.floor);
    const roomAgg = new Map<number, FilterOption>();
    for (const r of pool) {
      if (!r.room) continue;
      const entry = roomAgg.get(r.room.id) ?? {
        id: r.room.id,
        name: manyLocations && r.location ? `${r.location.name} · ${r.room.name}` : r.room.name,
        count: 0,
      };
      entry.count += 1;
      roomAgg.set(r.room.id, entry);
    }
    const rooms = [...roomAgg.values()].sort(byName);

    pool = pool.filter(chain.room);
    const containerCounts = new Map<number, number>();
    for (const r of pool) {
      for (const id of chainByItem.get(r.item.id) ?? []) {
        containerCounts.set(id, (containerCounts.get(id) ?? 0) + 1);
      }
    }
    const containers: FilterOption[] = [...containerCounts.entries()]
      .map(([id, count]) => {
        const name = model.containersById.get(id)?.name ?? `Container ${id}`;
        const roomName = roomId === 'all' ? containerRoomName(id, model) : null;
        return { id, count, name: roomName ? `${roomName} · ${name}` : name };
      })
      .sort(byName);

    pool = pool.filter(chain.container);
    let poolIds = new Set(pool.map((r) => r.item.id));
    const collectionOpts: FilterOption[] = (collections ?? [])
      .map((c) => {
        let count = 0;
        for (const member of collectionMembers.get(c.id) ?? []) if (poolIds.has(member)) count += 1;
        return { id: c.id, name: c.name, count };
      })
      .filter((o) => o.count > 0)
      .sort(byName);

    pool = pool.filter(chain.collection);
    poolIds = new Set(pool.map((r) => r.item.id));
    const tagOpts: FilterOption[] = tagMembers
      ? (tags ?? [])
          .map((t) => {
            let count = 0;
            for (const member of tagMembers.get(t.id) ?? []) if (poolIds.has(member)) count += 1;
            return { id: t.id, name: t.name, count };
          })
          .filter((o) => o.count > 0)
          .sort(byName)
      : [];

    pool = pool.filter(chain.tag);
    const ownerAgg = new Map<number, FilterOption>();
    let unowned = 0;
    for (const r of pool) {
      if (r.owner) {
        const entry = ownerAgg.get(r.owner.id) ?? { id: r.owner.id, name: r.owner.name, count: 0 };
        entry.count += 1;
        ownerAgg.set(r.owner.id, entry);
      } else {
        unowned += 1;
      }
    }
    const owners = [...ownerAgg.values()].sort(byName);

    return { locations, unassigned, floors, rooms, containers, collections: collectionOpts, tags: tagOpts, owners, unowned };
  }, [all, model, chain, chainByItem, collections, collectionMembers, tags, tagMembers, locationId, roomId]);

  // a change higher up the chain prunes lower options; drop any selection that
  // no longer points at a viable choice
  useEffect(() => {
    const gone = (v: number | 'all' | 'none', options: FilterOption[]) =>
      typeof v === 'number' && !options.some((o) => o.id === v);
    if (gone(floorId, dropdowns.floors)) setFloorId('all');
    if (gone(roomId, dropdowns.rooms)) setRoomId('all');
    if (gone(containerId, dropdowns.containers)) setContainerId('all');
    if (gone(collectionId, dropdowns.collections)) setCollectionId('all');
    if (tagMembers && gone(tagId, dropdowns.tags)) setTagId('all');
    if (gone(ownerId, dropdowns.owners)) setOwnerId('all');
    if (ownerId === 'none' && dropdowns.unowned === 0) setOwnerId('all');
  }, [dropdowns, floorId, roomId, containerId, collectionId, tagId, ownerId, tagMembers]);

  const activeFilterCount =
    (query.trim() ? 1 : 0) +
    (types.size > 0 ? 1 : 0) +
    [locationId, floorId, roomId, containerId, collectionId, tagId, ownerId].filter((v) => v !== 'all').length +
    (stored !== 'all' ? 1 : 0);

  const storedCounts = useMemo(() => {
    let inStorage = 0;
    for (const r of all) if (r.item.isStored) inStorage += 1;
    return { stored: inStorage, out: all.length - inStorage };
  }, [all]);

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
          onNavigate('house');
        }
      }
    };
    window.addEventListener('keydown', onKey);
    return () => window.removeEventListener('keydown', onKey);
  }, [onNavigate]);

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
    setLocationId('all');
    setFloorId('all');
    setRoomId('all');
    setContainerId('all');
    setCollectionId('all');
    setTagId('all');
    setOwnerId('all');
    setStored('all');
  };

  return (
    <div className="index-page" ref={rootRef}>
      <div className="index-inner">
        <header className="index-header index-reveal">
          <button className="page-brand" onClick={() => onNavigate('house')}>
            Habitat
          </button>
          <div className="about-header-right">
            <TopNav current="index" onNavigate={onNavigate} />
            <span className={`data-badge ${live ? 'live' : 'demo'}`}>{live ? 'live data' : 'demo data'}</span>
          </div>
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
            placeholder="Search items, locations, rooms, owners…"
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

            <FilterSelect
              label="Filter by location"
              allLabel="All locations"
              value={locationId}
              options={dropdowns.locations}
              onChange={setLocationId}
              noneLabel="Unassigned"
              noneCount={dropdowns.unassigned}
            />
            {dropdowns.floors.length > 0 && (
              <FilterSelect
                label="Filter by floor"
                allLabel="All floors"
                value={floorId}
                options={dropdowns.floors}
                onChange={(v) => setFloorId(v === 'none' ? 'all' : v)}
              />
            )}
            {dropdowns.rooms.length > 0 && (
              <FilterSelect
                label="Filter by room"
                allLabel="All rooms"
                value={roomId}
                options={dropdowns.rooms}
                onChange={(v) => setRoomId(v === 'none' ? 'all' : v)}
              />
            )}
            {dropdowns.containers.length > 0 && (
              <FilterSelect
                label="Filter by container"
                allLabel="All containers"
                value={containerId}
                options={dropdowns.containers}
                onChange={(v) => setContainerId(v === 'none' ? 'all' : v)}
              />
            )}
            {dropdowns.collections.length > 0 && (
              <FilterSelect
                label="Filter by collection"
                allLabel="All collections"
                value={collectionId}
                options={dropdowns.collections}
                onChange={(v) => setCollectionId(v === 'none' ? 'all' : v)}
              />
            )}
            {dropdowns.tags.length > 0 && (
              <FilterSelect
                label="Filter by tag"
                allLabel="All tags"
                value={tagId}
                options={dropdowns.tags}
                onChange={(v) => setTagId(v === 'none' ? 'all' : v)}
              />
            )}
            {(dropdowns.owners.length > 0 || dropdowns.unowned > 0) && (
              <FilterSelect
                label="Filter by owner"
                allLabel="All owners"
                value={ownerId}
                options={dropdowns.owners}
                onChange={setOwnerId}
                noneLabel="No owner"
                noneCount={dropdowns.unowned}
              />
            )}

            <div className="index-sort" role="group" aria-label="Stored">
              {STORED_OPTIONS.map(([key, label]) => (
                <button key={key} className={stored === key ? 'on' : ''} onClick={() => setStored(key)} title={
                  key === 'stored' ? `${storedCounts.stored} in storage` : key === 'out' ? `${storedCounts.out} in use` : 'Everything'
                }>
                  {label}
                </button>
              ))}
            </div>

            {activeFilterCount > 0 && (
              <button className="clear-filters" onClick={resetFilters}>
                Clear {activeFilterCount} filter{activeFilterCount === 1 ? '' : 's'} ×
              </button>
            )}
          </div>

          <div className="index-sort" role="group" aria-label="Sort">
            {SORT_OPTIONS.map(([key, label]) => (
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
          // remount (resetting to page 1) whenever the filter/sort signature changes
          <Paginated
            key={`${query}|${sort}|${[...types].join(',')}|${locationId}|${floorId}|${roomId}|${containerId}|${collectionId}|${tagId}|${ownerId}|${stored}`}
            rows={filtered}
            pageSize={25}
          >
            {(pageRows) => <IndexList pageRows={pageRows} live={live} onViewItem={onViewItem} />}
          </Paginated>
        )}

        <footer className="index-footer">
          Showing {filtered.length} of {all.length} items
          {totalValue > 0 && <> · combined value {formatPrice(totalValue)}</>}
        </footer>

        <SocialFooter />
      </div>
    </div>
  );
}

function FilterSelect({
  label,
  allLabel,
  value,
  options,
  onChange,
  noneLabel,
  noneCount,
}: {
  label: string;
  allLabel: string;
  value: number | 'all' | 'none';
  options: FilterOption[];
  onChange: (v: number | 'all' | 'none') => void;
  /** when set (with a non-zero count), offers a "none of these" bucket option */
  noneLabel?: string;
  noneCount?: number;
}) {
  return (
    <select
      className="room-select"
      value={typeof value === 'number' ? String(value) : value}
      onChange={(e) =>
        onChange(e.target.value === 'all' || e.target.value === 'none' ? e.target.value : Number(e.target.value))
      }
      aria-label={label}
    >
      <option value="all">{allLabel}</option>
      {options.map((o) => (
        <option key={o.id} value={o.id}>
          {o.name} ({o.count})
        </option>
      ))}
      {noneLabel != null && (noneCount ?? 0) > 0 && (
        <option value="none">
          {noneLabel} ({noneCount})
        </option>
      )}
    </select>
  );
}

/** One page of rows; cascades them in whenever the visible page changes. */
function IndexList({
  pageRows,
  live,
  onViewItem,
}: {
  pageRows: ResolvedItem[];
  live: boolean;
  onViewItem: (id: number) => void;
}) {
  const listRef = useRef<HTMLUListElement>(null);
  const pageKey = pageRows.map((r) => r.item.id).join(',');
  useEffect(() => {
    const rows = listRef.current?.children;
    if (!rows || rows.length === 0) return;
    gsap.fromTo(
      rows,
      { y: 16, autoAlpha: 0 },
      { y: 0, autoAlpha: 1, duration: 0.4, stagger: 0.035, ease: 'power2.out', overwrite: 'auto' },
    );
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [pageKey]);

  return (
    <ul className="index-list" ref={listRef}>
      {pageRows.map((r) => (
        <IndexRow key={r.item.id} resolved={r} live={live} onView={() => onViewItem(r.item.id)} />
      ))}
    </ul>
  );
}

function IndexRow({ resolved, live, onView }: { resolved: ResolvedItem; live: boolean; onView: () => void }) {
  const { item, location, room, owner } = resolved;
  const accent = ITEM_TYPE_COLORS[primaryType(item) % ITEM_TYPE_COLORS.length];
  return (
    <li className="index-row">
      <button className="index-row-main" onClick={onView} title="View in the neighborhood">
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
        <span className="row-price">
          {formatPrice(itemValue(item))}
          {item.isStored && <small className="row-stored">stored</small>}
        </span>
        <span className="row-action">View in 3D ↗</span>
      </button>
      {/* a sibling of the row button (the hover icon is itself a button, and
          buttons cannot nest); hidden on phones with the other secondary columns */}
      <span className="index-row-pic">
        <PictureHoverIcon kind="items" ownerId={item.id} live={live} />
      </span>
    </li>
  );
}
