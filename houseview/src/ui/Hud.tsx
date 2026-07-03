import gsap from 'gsap';
import { useEffect, useRef } from 'react';
import type { PlacedRoom, SceneModel } from '../model';
import { ITEM_TYPE_COLORS, ITEM_TYPE_NAMES, type FloorResponse } from '../types';

interface HudProps {
  model: SceneModel;
  /** rooms of the active Location, shown in the room dock */
  placedRooms: PlacedRoom[];
  /** storeys of the active Location, sorted top-down */
  floors: FloorResponse[];
  live: boolean;
  /** levelIndex of the storey in focus */
  floor: number;
  activeSite: string;
  onFloor: (level: number) => void;
  onFlyToRoom: (roomId: number) => void;
  onSite: (key: string) => void;
  onResetView: () => void;
  onBrowse: () => void;
  onAbout: () => void;
  onManage: () => void;
}

/** Chip label for a storey: B for basements, 1-based numbers above grade. */
function levelShort(levelIndex: number): string {
  return levelIndex < 0 ? (levelIndex === -1 ? 'B' : `B${-levelIndex}`) : String(levelIndex + 1);
}

export function Hud({
  model,
  placedRooms,
  floors,
  live,
  floor,
  activeSite,
  onFloor,
  onFlyToRoom,
  onSite,
  onResetView,
  onBrowse,
  onAbout,
  onManage,
}: HudProps) {
  const rootRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    const root = rootRef.current;
    if (!root) return;
    const blocks = root.querySelectorAll('.hud-animate');
    gsap.fromTo(
      blocks,
      { autoAlpha: 0, y: 24 },
      { autoAlpha: 1, y: 0, duration: 0.9, stagger: 0.12, delay: 0.5, ease: 'power3.out' },
    );
  }, []);

  // Re-reveal the floor switcher and room dock each time the active Location
  // changes (the one-time intro above only catches elements present at mount).
  useEffect(() => {
    const els = rootRef.current?.querySelectorAll('.hud-pop');
    if (!els || els.length === 0) return;
    gsap.fromTo(
      els,
      { autoAlpha: 0, y: 18 },
      { autoAlpha: 1, y: 0, duration: 0.55, stagger: 0.1, delay: 0.2, ease: 'power3.out' },
    );
  }, [activeSite]);

  // rooms of the storey in focus first, then the rest of the building
  const dockRooms = [...placedRooms].sort((a, b) => (a.level === floor ? -1 : 0) - (b.level === floor ? -1 : 0) || a.room.id - b.room.id);

  return (
    <div className="hud" ref={rootRef}>
      <header className="hud-header hud-animate">
        <div className="wordmark">
          <h1>Habitat</h1>
          <p>
            ItemCatalogue, spatially
            <span className={`data-badge ${live ? 'live' : 'demo'}`}>{live ? 'live data' : 'demo data'}</span>
          </p>
        </div>
        <div className="header-right">
          <nav className="top-nav" aria-label="Pages">
            <button className="top-nav-btn primary" onClick={onBrowse} title="Browse the index">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" aria-hidden="true">
                <path d="M8.5 6h12M8.5 12h12M8.5 18h12" />
                <circle cx="3.8" cy="6" r="1.5" fill="currentColor" stroke="none" />
                <circle cx="3.8" cy="12" r="1.5" fill="currentColor" stroke="none" />
                <circle cx="3.8" cy="18" r="1.5" fill="currentColor" stroke="none" />
              </svg>
              <span>Browse</span>
            </button>
            <button className="top-nav-btn" onClick={onManage} title="Manage locations, rooms, containers and items">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" aria-hidden="true">
                <path d="M3.5 8h9M18.9 8h1.6M3.5 16h3M12.9 16h7.6" />
                <circle cx="15.7" cy="8" r="2.6" />
                <circle cx="9.7" cy="16" r="2.6" />
              </svg>
              <span>Manage</span>
            </button>
            <button className="top-nav-btn" onClick={onAbout} title="About Habitat">
              <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2.2" strokeLinecap="round" aria-hidden="true">
                <circle cx="12" cy="12" r="9.2" />
                <path d="M12 11v5.4" />
                <circle cx="12" cy="7.4" r="0.6" fill="currentColor" stroke="none" />
              </svg>
              <span>About</span>
            </button>
          </nav>
          <div className="stats">
            <div>
              <strong>{model.totalItems}</strong>
              <span>items</span>
            </div>
            <div>
              <strong>{model.sites.length}</strong>
              <span>places</span>
            </div>
          </div>
        </div>
      </header>

      {/* the active location's storeys, from the database (top-down) */}
      {floors.length > 1 && (
        <nav className="floor-switch hud-pop" aria-label="Floor">
          {floors.map((f) => (
            <button
              key={f.id}
              className={floor === f.levelIndex ? 'active' : ''}
              onClick={() => onFloor(f.levelIndex)}
              title={f.name}
            >
              <span className="floor-key">{levelShort(f.levelIndex)}</span>
              <span className="floor-name">{f.name}</span>
            </button>
          ))}
          <button className="reset" onClick={onResetView} title="Reset view">
            ⟲
          </button>
          <span className="floor-hint">↑ ↓ floors</span>
        </nav>
      )}

      <aside className="legend hud-animate">
        {ITEM_TYPE_NAMES.map((name, i) => {
          const count = model.typeCounts.get(i) ?? 0;
          if (count === 0) return null;
          return (
            <span key={name} className="legend-entry">
              <i style={{ background: ITEM_TYPE_COLORS[i] }} />
              {name}
              <em>{count}</em>
            </span>
          );
        })}
        {model.unassigned.length > 0 && (
          <span className="legend-entry">
            <i style={{ background: '#9aa1a8' }} />
            Unassigned
            <em>{model.unassigned.length}</em>
          </span>
        )}
      </aside>

      {/* rooms of the active Location */}
      <nav className="room-dock hud-pop" aria-label="Rooms">
        {dockRooms.length > 0 && <span className="dock-label">Rooms</span>}
        <div className="dock-scroll">
          {dockRooms.map((p) => (
            <button key={p.room.id} onClick={() => onFlyToRoom(p.room.id)}>
              {p.room.name}
              {p.items.length > 0 && <em>{p.items.length}</em>}
            </button>
          ))}
        </div>
      </nav>

      {/* the neighbourhood: one pill per database Location */}
      <nav className="room-dock site-dock hud-animate" aria-label="Locations">
        <span className="dock-label">Places</span>
        <div className="dock-scroll">
          {model.sites.map((site) => (
            <button key={site.key} className={activeSite === site.key ? 'on' : ''} onClick={() => onSite(site.key)}>
              {site.label}
              {site.items.length > 0 && <em>{site.items.length}</em>}
            </button>
          ))}
        </div>
      </nav>
    </div>
  );
}
