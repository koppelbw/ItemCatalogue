// Spatial blueprint of the dollhouse. Room records from the database are matched
// onto these footprints by name; unmatched rooms get generic cabins on the lawn.

export type FloorLevel = -1 | 0 | 1 | 2;

export const WALL_HEIGHT = 2.5;
/** Sims-style half wall used for interior partitions so the camera can see into every room. */
export const HALF_WALL_HEIGHT = 1.05;
export const WALL_THICKNESS = 0.14;
export const SLAB_THICKNESS = 0.18;
export const LEVEL_HEIGHT = 3.0;

export const FLOOR_LABELS: Record<FloorLevel, string> = {
  [-1]: 'Basement',
  0: 'Ground floor',
  1: 'Upper floor',
  2: 'Attic & roof',
};

export const FLOOR_ORDER: FloorLevel[] = [2, 1, 0, -1];

/**
 * The house sits on a foundation plinth slightly above the lawn so floor
 * slabs never share a plane with the grass (which causes z-fighting shimmer).
 */
export const HOUSE_BASE = 0.22;

export function levelY(level: FloorLevel): number {
  return HOUSE_BASE + level * LEVEL_HEIGHT;
}

export interface Rect {
  x: number;
  z: number;
  w: number;
  d: number;
}

export type FurnitureKind =
  | 'living'
  | 'kitchen'
  | 'dining'
  | 'bathroom'
  | 'bedroom'
  | 'office'
  | 'storage'
  | 'garage'
  | 'basement'
  | 'attic'
  | 'generic';

export interface RoomDef {
  /** lowercase room name this footprint matches */
  match: string;
  level: FloorLevel;
  rect: Rect;
  floorColor: string;
  wallColor: string;
  accent: string;
  furniture: FurnitureKind;
}

// House shell: x 0..10, z 0..8. Garage attached east: x 10..14.6.
// The camera sits to the south-east, so each room keeps its north (z=0 side)
// and west (x=0 side) walls - the classic cutaway dollhouse look.
export const ROOM_DEFS: RoomDef[] = [
  // Ground floor
  { match: 'living room', level: 0, rect: { x: 0, z: 0, w: 6, d: 5 }, floorColor: '#d9c2a0', wallColor: '#f1e2cc', accent: '#ef6f6c', furniture: 'living' },
  { match: 'kitchen', level: 0, rect: { x: 6, z: 0, w: 4, d: 4 }, floorColor: '#ccd6d2', wallColor: '#dcebe2', accent: '#3ec6b8', furniture: 'kitchen' },
  { match: 'dining room', level: 0, rect: { x: 6, z: 4, w: 4, d: 4 }, floorColor: '#d9c2a0', wallColor: '#f3d8c0', accent: '#f2a93b', furniture: 'dining' },
  { match: 'bathroom', level: 0, rect: { x: 0, z: 5, w: 3, d: 3 }, floorColor: '#cfe3ea', wallColor: '#dceef4', accent: '#5b8def', furniture: 'bathroom' },
  { match: 'garage', level: 0, rect: { x: 10, z: 0, w: 4.6, d: 5.2 }, floorColor: '#b6bac0', wallColor: '#d8d3c8', accent: '#8c97a5', furniture: 'garage' },
  // Upper floor
  { match: 'bedroom', level: 1, rect: { x: 0, z: 0, w: 6, d: 5 }, floorColor: '#d9c2a0', wallColor: '#e8def2', accent: '#a78bdb', furniture: 'bedroom' },
  { match: 'office', level: 1, rect: { x: 6, z: 0, w: 4, d: 4 }, floorColor: '#c9b794', wallColor: '#d7e4f2', accent: '#5b8def', furniture: 'office' },
  { match: 'storage', level: 1, rect: { x: 6, z: 4, w: 4, d: 4 }, floorColor: '#c5beb2', wallColor: '#e0dacd', accent: '#b08a5a', furniture: 'storage' },
  // Whole-footprint floors
  { match: 'basement', level: -1, rect: { x: 0, z: 0, w: 10, d: 8 }, floorColor: '#a5a19a', wallColor: '#c6c1b7', accent: '#8c97a5', furniture: 'basement' },
  { match: 'attic', level: 2, rect: { x: 0, z: 0, w: 10, d: 8 }, floorColor: '#c9a886', wallColor: '#e6d7be', accent: '#b08a5a', furniture: 'attic' },
];

// Rooms that live in the car instead of the house.
export const CAR_ROOM_MATCHES = ['glove box', 'trunk'] as const;
export type CarSlot = (typeof CAR_ROOM_MATCHES)[number];

/** Car park position on the driveway (group origin, ground level). */
export const CAR_POSITION: [number, number, number] = [12.3, 0, 8.6];

// ---------------------------------------------------------------------------
// Sites: every database Location is its own building in the neighbourhood.
// The full dollhouse is the "house" site; locations are matched by name and
// anything unrecognised gets an auto-placed cabin.
// ---------------------------------------------------------------------------

export type SiteKind = 'house' | 'apartment' | 'cottage' | 'storage' | 'car' | 'cabin';

export interface SiteDef {
  kind: SiteKind;
  /** north-west corner of the site footprint on the lawn */
  origin: [number, number];
  /** camera fly-to target when the site becomes active */
  focus: [number, number, number];
  zoom: number;
}

export const SITE_DEFS: Record<string, SiteDef> = {
  house: { kind: 'house', origin: [0, 0], focus: [6.5, 1.2, 5.5], zoom: 1 },
  apartment: { kind: 'apartment', origin: [-13, -8], focus: [-10.7, 1.4, -5.9], zoom: 1.8 },
  grandmas: { kind: 'cottage', origin: [-15, 9], focus: [-12.7, 1.2, 11.1], zoom: 1.8 },
  'storage unit': { kind: 'storage', origin: [19, -3], focus: [21.3, 1.2, -0.9], zoom: 1.8 },
  car: { kind: 'car', origin: [CAR_POSITION[0], CAR_POSITION[2]], focus: [CAR_POSITION[0], 1, CAR_POSITION[2]], zoom: 2.0 },
};

/** Auto-placed cabins for locations the layout does not know by name. */
export function cabinSiteDef(index: number): SiteDef {
  const x = -4 - index * 7;
  const z = 17;
  return { kind: 'cabin', origin: [x, z], focus: [x + 2.3, 1.1, z + 2.1], zoom: 1.8 };
}

/**
 * A stable satellite slot on the lawn for a non-active Location, placed on an
 * ellipse around the central dollhouse so buildings never overlap the stage or
 * each other. Position depends only on (index, count) so the neighbourhood
 * never reshuffles when the active Location changes — the active one's slot is
 * simply left empty (it is drawn as the central dollhouse instead). `kind` is a
 * placeholder; the model overrides it with a per-Location shell style.
 */
export function satelliteSlot(index: number, count: number): SiteDef {
  const cx = 6;
  const cz = 4;
  const rx = 22;
  const rz = 16;
  const angle = -Math.PI / 2 + (index / Math.max(1, count)) * Math.PI * 2;
  const ox = cx + Math.cos(angle) * rx - SITE_INTERIOR.w / 2;
  const oz = cz + Math.sin(angle) * rz - SITE_INTERIOR.d / 2;
  return {
    kind: 'cabin',
    origin: [ox, oz],
    focus: [ox + SITE_INTERIOR.w / 2, 1.2, oz + SITE_INTERIOR.d / 2],
    zoom: 1.8,
  };
}

/** Interior footprint shared by all small site buildings. */
export const SITE_INTERIOR: Rect = { x: 0, z: 0, w: 4.6, d: 4.2 };

/** Footprints for database rooms that match nothing above: cabins west of the house. */
export function extraRoomDef(index: number): RoomDef {
  return {
    match: `extra-${index}`,
    level: 0,
    rect: { x: -5.2, z: index * 4.0, w: 3.6, d: 3.2 },
    floorColor: '#cdc3b2',
    wallColor: '#e7dfd0',
    accent: '#8c97a5',
    furniture: 'generic',
  };
}

/**
 * Deterministic display spots for item markers inside a room footprint.
 * Spots form a relaxed grid biased toward the open (south-east) half of the room
 * so markers do not bury themselves in the furniture along the back walls.
 */
export function itemSpots(rect: Rect, count: number): [number, number][] {
  const spots: [number, number][] = [];
  if (count === 0) return spots;
  const padX = Math.min(1.0, rect.w * 0.22);
  const padZ = Math.min(1.0, rect.d * 0.22);
  const usableW = rect.w - padX * 2;
  const usableD = rect.d - padZ * 2;
  const cols = Math.max(1, Math.min(count, Math.round(usableW / 1.25)));
  for (let i = 0; i < count; i++) {
    const col = i % cols;
    const row = Math.floor(i / cols);
    const x = padX + (cols === 1 ? usableW / 2 : (usableW * col) / (cols - 1));
    // weave rows from the front (south) toward the back
    const z = rect.d - padZ - (row % 3) * Math.min(1.3, usableD / 2);
    // tiny deterministic jitter so rows do not look machine-stamped
    const jitter = ((i * 37) % 10) / 22 - 0.22;
    spots.push([x + jitter * 0.5, z + jitter]);
  }
  return spots;
}
