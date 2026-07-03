// Spatial constants for the dollhouse. Room footprints now come from the
// database (Room.OriginX/Y + Width/Depth in inches); this module owns the
// inches → scene-unit scaling, the vertical story rhythm, and the fallback
// footprints used for rooms that have not been measured yet.

export const WALL_HEIGHT = 2.5;
/** Sims-style half wall used for interior partitions so the camera can see into every room. */
export const HALF_WALL_HEIGHT = 1.05;
export const WALL_THICKNESS = 0.14;
export const SLAB_THICKNESS = 0.18;
export const LEVEL_HEIGHT = 3.0;

/**
 * Plan scale: 1 scene unit = 24 inches (2 ft), so a 15×12 ft room is a 7.5×6
 * unit footprint. Vertical scale is squashed harder (1 unit = 40 in) to keep
 * the cutaway dollhouse look — real 8 ft walls would tower over the furniture.
 */
export const PLAN_INCHES_PER_UNIT = 24;
export const VERTICAL_INCHES_PER_UNIT = 40;

/** plan-view inches → scene units */
export const u = (inches: number): number => inches / PLAN_INCHES_PER_UNIT;

/** wall/ceiling height in scene units for a room, from measured inches when present */
export function wallHeightFor(heightInches: number | null): number {
  if (heightInches == null) return WALL_HEIGHT;
  return Math.min(2.6, Math.max(0.5, heightInches / VERTICAL_INCHES_PER_UNIT));
}

/**
 * The house sits on a foundation plinth slightly above the lawn so floor
 * slabs never share a plane with the grass (which causes z-fighting shimmer).
 */
export const HOUSE_BASE = 0.22;

/** Y of a story's floor slab. levelIndex is signed (basement = -1, ground = 0, …). */
export function levelY(levelIndex: number): number {
  return HOUSE_BASE + levelIndex * LEVEL_HEIGHT;
}

/** World-space center of the central dollhouse stage; active locations are laid out around it. */
export const STAGE_CENTER: [number, number] = [6.5, 4.5];

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

/** Domain.Enums.RoomType ordinal → furniture set. */
export const ROOM_TYPE_FURNITURE: FurnitureKind[] = [
  'bedroom', // Bedroom
  'bathroom', // Bathroom
  'kitchen', // Kitchen
  'living', // LivingRoom
  'dining', // DiningRoom
  'generic', // Hallway
  'office', // Office
  'garage', // Garage
  'storage', // Laundry
  'storage', // Closet
  'basement', // Basement
  'attic', // Attic
  'generic', // Other
];

/** name-match fallback for rooms without a RoomType */
const NAME_FURNITURE: [string, FurnitureKind][] = [
  ['living', 'living'],
  ['kitchen', 'kitchen'],
  ['dining', 'dining'],
  ['bath', 'bathroom'],
  ['bed', 'bedroom'],
  ['office', 'office'],
  ['storage', 'storage'],
  ['garage', 'garage'],
  ['basement', 'basement'],
  ['attic', 'attic'],
];

export function furnitureFor(roomType: number | null, name: string): FurnitureKind {
  if (roomType != null && ROOM_TYPE_FURNITURE[roomType]) return ROOM_TYPE_FURNITURE[roomType];
  const key = name.trim().toLowerCase();
  return NAME_FURNITURE.find(([match]) => key.includes(match))?.[1] ?? 'generic';
}

/** Visual shape for a top-level container, built from the container's own dimensions + color. */
export type ContainerShapeKind = 'bed' | 'seating' | 'table' | 'dresser' | 'wardrobe' | 'piano' | 'box';

/**
 * Furniture-shaped containers (beds, sofas, tables) share the DB's generic
 * ContainerType ordinals — there is no Bed/Sofa/Table type; most are catalogued
 * as "Other" or "Chest" — so those shapes are inferred from the container's
 * name (the same fallback pattern as `furnitureFor` above). Types that DO carry
 * shape information (Cabinet, Drawer, Wardrobe) are used when the name says nothing.
 */
const NAME_CONTAINER_SHAPES: [string, ContainerShapeKind][] = [
  ['bed', 'bed'],
  ['sofa', 'seating'],
  ['couch', 'seating'],
  ['chair', 'seating'],
  ['table', 'table'],
  ['desk', 'table'],
  ['dresser', 'dresser'],
  ['nightstand', 'dresser'],
  ['closet', 'wardrobe'],
  ['wardrobe', 'wardrobe'],
  ['piano', 'piano'],
];

/** Domain.Enums.ContainerType ordinals that imply a shape: Cabinet 2, Drawer 3, Wardrobe 5. */
const TYPE_CONTAINER_SHAPES: Record<number, ContainerShapeKind> = {
  2: 'dresser',
  3: 'dresser',
  5: 'wardrobe',
};

export function containerShapeFor(containerType: number | null, name: string): ContainerShapeKind {
  const key = name.trim().toLowerCase();
  // the LAST keyword wins so the head noun decides: "Bed 1 Closet" is a closet, not a bed
  let byName: ContainerShapeKind | undefined;
  let bestAt = -1;
  for (const [match, kind] of NAME_CONTAINER_SHAPES) {
    const at = key.lastIndexOf(match);
    if (at > bestAt) {
      bestAt = at;
      byName = kind;
    }
  }
  if (byName) return byName;
  if (containerType != null && TYPE_CONTAINER_SHAPES[containerType]) return TYPE_CONTAINER_SHAPES[containerType];
  return 'box';
}

/** Compass direction a container's front points; shapes are authored facing south. */
export type FacingDir = 'N' | 'S' | 'E' | 'W';

/** Shape kinds with a distinct front that should face into the room. */
const DIRECTIONAL_KINDS = new Set<ContainerShapeKind>(['bed', 'seating', 'dresser', 'wardrobe', 'piano']);

const OPPOSITE: Record<FacingDir, FacingDir> = { N: 'S', S: 'N', E: 'W', W: 'E' };

/**
 * Logical facing for a container with no explicit rotation: its back goes
 * against the nearest room wall so it opens into the room. Beds back
 * (headboard) onto a short end; sofas, dressers, wardrobes and pianos back
 * onto a long side. Near-square pieces (nightstands) are free to pick any wall.
 */
export function containerFacing(
  kind: ContainerShapeKind,
  roomW: number,
  roomD: number,
  x: number,
  z: number,
  w: number,
  d: number,
): FacingDir {
  if (!DIRECTIONAL_KINDS.has(kind)) return 'S';
  const dist: Record<FacingDir, number> = { N: z, W: x, S: roomD - (z + d), E: roomW - (x + w) };
  const nearSquare = Math.max(w, d) < Math.min(w, d) * 1.3;
  let backs: FacingDir[];
  if (nearSquare && kind !== 'bed') backs = ['N', 'S', 'E', 'W'];
  else if (kind === 'bed' ? w >= d : w < d) backs = ['W', 'E'];
  else backs = ['N', 'S'];
  const back = backs.reduce((best, dir) => (dist[dir] < dist[best] ? dir : best));
  return OPPOSITE[back];
}

export interface RoomPalette {
  floorColor: string;
  wallColor: string;
  accent: string;
}

/** Fallback palette by furniture kind, for rooms without stored colors. */
export const FURNITURE_PALETTES: Record<FurnitureKind, RoomPalette> = {
  living: { floorColor: '#d9c2a0', wallColor: '#f1e2cc', accent: '#ef6f6c' },
  kitchen: { floorColor: '#ccd6d2', wallColor: '#dcebe2', accent: '#3ec6b8' },
  dining: { floorColor: '#d9c2a0', wallColor: '#f3d8c0', accent: '#f2a93b' },
  bathroom: { floorColor: '#cfe3ea', wallColor: '#dceef4', accent: '#5b8def' },
  bedroom: { floorColor: '#d9c2a0', wallColor: '#e8def2', accent: '#a78bdb' },
  office: { floorColor: '#c9b794', wallColor: '#d7e4f2', accent: '#5b8def' },
  storage: { floorColor: '#c5beb2', wallColor: '#e0dacd', accent: '#b08a5a' },
  garage: { floorColor: '#b6bac0', wallColor: '#d8d3c8', accent: '#8c97a5' },
  basement: { floorColor: '#a5a19a', wallColor: '#c6c1b7', accent: '#8c97a5' },
  attic: { floorColor: '#c9a886', wallColor: '#e6d7be', accent: '#b08a5a' },
  generic: { floorColor: '#cdc3b2', wallColor: '#e7dfd0', accent: '#8c97a5' },
};

/**
 * Fallback footprint for a room with no measured geometry: a modest cabin
 * placed in reading order, three to a row. `index` counts unmeasured rooms on
 * the same floor.
 */
export function fallbackRect(index: number): Rect {
  const w = 4.2;
  const d = 3.6;
  const col = index % 3;
  const row = Math.floor(index / 3);
  return { x: col * (w + 0.6), z: row * (d + 0.6), w, d };
}

// ---------------------------------------------------------------------------
// Sites: every database Location is its own building in the neighborhood.
// The active one is drawn as the central cutaway dollhouse from its measured
// rooms; the rest stand as small satellite buildings around the stage.
// ---------------------------------------------------------------------------

export type SiteKind = 'apartment' | 'cottage' | 'townhouse' | 'storage' | 'cabin' | 'car';

export interface SiteDef {
  kind: SiteKind;
  /** north-west corner of the site footprint on the lawn */
  origin: [number, number];
  /** camera fly-to target when the site becomes active */
  focus: [number, number, number];
  zoom: number;
}

/** Shell silhouettes matched by location name; anything unrecognised is a cabin. */
const NAME_SHELLS: [string, SiteKind][] = [
  ['apartment', 'apartment'],
  ['grandma', 'townhouse'],
  ['cottage', 'cottage'],
  ['storage', 'storage'],
  ['car', 'car'],
];

export function siteKindFor(name: string): SiteKind {
  const key = name.trim().toLowerCase();
  return NAME_SHELLS.find(([match]) => key.includes(match))?.[1] ?? 'cabin';
}

/**
 * A stable satellite slot on the lawn for a non-active Location, placed on an
 * ellipse around the central dollhouse so buildings never overlap the stage or
 * each other. Position depends only on (index, count) so the neighborhood
 * never reshuffles when the active Location changes — the active one's slot is
 * simply left empty (it is drawn as the central dollhouse instead). `kind` is a
 * placeholder; the model overrides it with a per-Location shell style.
 *
 * The isometric camera looks from the south-east, so anything north-west of
 * the stage hides directly behind the dollhouse and anything south-east sits
 * in front of it. The usual five-location neighborhood uses hand-tuned angles
 * that keep every slot clear of both zones; other counts fall back to an even
 * spread. 0° is east, 90° is south.
 */
const SLOT_ANGLES_5 = [-55, 10, 80, 150, 215].map((deg) => (deg * Math.PI) / 180);

export function satelliteSlot(index: number, count: number): SiteDef {
  const [cx, cz] = STAGE_CENTER;
  const rx = 22;
  const rz = 18.5;
  const angle =
    count === SLOT_ANGLES_5.length
      ? SLOT_ANGLES_5[index]
      : -Math.PI / 2 + ((index + 0.55) / Math.max(1, count)) * Math.PI * 2;
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
