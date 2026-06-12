import gsap from 'gsap';
import { useEffect, useRef } from 'react';
import { FLOOR_LABELS, FLOOR_ORDER, type FloorLevel } from '../layout';
import type { CarRoom, PlacedRoom, SceneModel } from '../model';
import { ITEM_TYPE_COLORS, ITEM_TYPE_NAMES } from '../types';

interface HudProps {
  model: SceneModel;
  live: boolean;
  floor: FloorLevel;
  onFloor: (level: FloorLevel) => void;
  onFlyToRoom: (roomId: number) => void;
  onResetView: () => void;
  onBrowse: () => void;
}

const FLOOR_SHORT: Record<FloorLevel, string> = { [-1]: 'B', 0: '1', 1: '2', 2: 'A' };

export function Hud({ model, live, floor, onFloor, onFlyToRoom, onResetView, onBrowse }: HudProps) {
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

  const dockEntries: { id: number; name: string; count: number; level: FloorLevel | 'car' }[] = [
    ...model.placedRooms.map((p: PlacedRoom) => ({
      id: p.room.id,
      name: p.room.name,
      count: p.items.length,
      level: p.def.level as FloorLevel | 'car',
    })),
    ...model.carRooms.map((c: CarRoom) => ({ id: c.room.id, name: c.room.name, count: c.items.length, level: 'car' as const })),
  ];

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
              <strong>{model.placedRooms.length + model.carRooms.length}</strong>
              <span>rooms</span>
            </div>
          </div>
          <button className="nav-btn" onClick={onBrowse}>
            Browse the index ↗
          </button>
        </div>
      </header>

      <nav className="floor-switch hud-animate" aria-label="Floor">
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
        <span className="floor-hint">↑ ↓ keys</span>
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

      <nav className="room-dock hud-animate" aria-label="Rooms">
        {dockEntries.map((entry) => (
          <button key={entry.id} onClick={() => onFlyToRoom(entry.id)}>
            {entry.name}
            {entry.count > 0 && <em>{entry.count}</em>}
          </button>
        ))}
      </nav>
    </div>
  );
}
