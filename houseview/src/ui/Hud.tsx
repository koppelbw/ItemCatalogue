import gsap from 'gsap';
import { useEffect, useRef } from 'react';
import { FLOOR_LABELS, FLOOR_ORDER, type FloorLevel } from '../layout';
import type { PlacedRoom, SceneModel } from '../model';
import { ITEM_TYPE_COLORS, ITEM_TYPE_NAMES } from '../types';

interface HudProps {
  model: SceneModel;
  /** rooms of the active Location, shown in the room dock */
  placedRooms: PlacedRoom[];
  live: boolean;
  floor: FloorLevel;
  activeSite: string;
  onFloor: (level: FloorLevel) => void;
  onFlyToRoom: (roomId: number) => void;
  onSite: (key: string) => void;
  onResetView: () => void;
  onBrowse: () => void;
  onAbout: () => void;
  onManage: () => void;
}

const FLOOR_SHORT: Record<FloorLevel, string> = { [-1]: 'B', 0: '1', 1: '2', 2: 'A' };

export function Hud({ model, placedRooms, live, floor, activeSite, onFloor, onFlyToRoom, onSite, onResetView, onBrowse, onAbout, onManage }: HudProps) {
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
          <div className="nav-btn-row">
            <button className="nav-btn" onClick={onBrowse}>
              Browse the index ↗
            </button>
            <button className="nav-btn nav-btn-ghost" onClick={onManage}>
              Manage
            </button>
            <button className="nav-btn nav-btn-ghost" onClick={onAbout}>
              About
            </button>
          </div>
        </div>
      </header>

      <nav className="floor-switch hud-pop" aria-label="Floor">
        {FLOOR_ORDER.map((level) => (
          <button
            key={level}
            className={floor === level ? 'active' : ''}
            onClick={() => onFloor(level)}
            title={FLOOR_LABELS[level]}
          >
            <span className="floor-key">{FLOOR_SHORT[level]}</span>
            <span className="floor-name">{FLOOR_LABELS[level]}</span>
          </button>
        ))}
        <button className="reset" onClick={onResetView} title="Reset view">
          ⟲
        </button>
        <span className="floor-hint">↑ ↓ floors</span>
      </nav>

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
        {placedRooms.map((p) => (
          <button key={p.room.id} onClick={() => onFlyToRoom(p.room.id)}>
            {p.room.name}
            {p.items.length > 0 && <em>{p.items.length}</em>}
          </button>
        ))}
      </nav>

      {/* the neighbourhood: one pill per database Location */}
      <nav className="room-dock site-dock hud-animate" aria-label="Locations">
        {model.sites.map((site) => (
          <button key={site.key} className={activeSite === site.key ? 'on' : ''} onClick={() => onSite(site.key)}>
            {site.label}
            {site.items.length > 0 && <em>{site.items.length}</em>}
          </button>
        ))}
        <span className="floor-hint dock-hint">← → keys</span>
      </nav>
    </div>
  );
}
