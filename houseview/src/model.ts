import {
  FURNITURE_PALETTES,
  SITE_INTERIOR,
  STAGE_CENTER,
  fallbackRect,
  furnitureFor,
  levelY,
  satelliteSlot,
  siteKindFor,
  u,
  wallHeightFor,
  type FurnitureKind,
  type Rect,
  type RoomPalette,
  type SiteDef,
} from './layout';
import {
  WALL_EAST,
  WALL_NORTH,
  WALL_SOUTH,
  WALL_WEST,
  type CatalogueData,
  type ContainerResponse,
  type DoorResponse,
  type FloorResponse,
  type ItemResponse,
  type LocationResponse,
  type PersonResponse,
  type ResolvedItem,
  type RoomResponse,
  type Selection,
  type StairResponse,
} from './types';

/** A top-level container drawn inside its room, in room-local scene units. */
export interface PlacedContainer {
  container: ContainerResponse;
  /** NW corner within the room */
  x: number;
  z: number;
  w: number;
  d: number;
  h: number;
  /** mount height off the floor (PositionZInches) — wall cabinets and shelves float */
  y: number;
  rotation: number;
  color: string;
  itemCount: number;
}

/** A door opening placed on one wall of its FromRoom, in room-local scene units. */
export interface PlacedDoor {
  door: DoorResponse;
  /** Domain.Enums.Wall ordinal */
  wall: number;
  /** distance along the wall from its min corner */
  offset: number;
  w: number;
  h: number;
}

/** A stair run rising out of its FromRoom, in room-local scene units. */
export interface PlacedStair {
  stair: StairResponse;
  x: number;
  z: number;
  run: number;
  w: number;
  rise: number;
  steps: number;
  rotation: number;
}

/** A database room placed onto the central dollhouse stage from its measured geometry. */
export interface PlacedRoom {
  room: RoomResponse;
  floor: FloorResponse;
  /** floor.levelIndex — the storey this room renders on */
  level: number;
  /** world-units footprint on its level (x/z = NW corner) */
  rect: Rect;
  /** degrees, rotates the room group around its NW corner */
  rotation: number;
  wallHeight: number;
  colors: RoomPalette;
  /** true when the footprint came from measured geometry rather than auto-layout */
  hasGeometry: boolean;
  /** every item that ultimately lives in this room (including inside containers) — used for counts */
  items: ResolvedItem[];
  /** room-level items tagged "Furniture" — the only item markers drawn inside the room */
  furnishings: ResolvedItem[];
  containers: PlacedContainer[];
  doors: PlacedDoor[];
  stairs: PlacedStair[];
  /** true when another room on the same level sits behind this wall - rendered as a half wall */
  northInterior: boolean;
  westInterior: boolean;
}

/** A database Location rendered as its own building in the neighbourhood. */
export interface Site {
  /** stable unique key, `loc-<id>` */
  key: string;
  label: string;
  def: SiteDef;
  location: LocationResponse;
  /** the location's storeys, sorted bottom-up by levelIndex */
  floors: FloorResponse[];
  /** all rooms across the location's floors */
  rooms: RoomResponse[];
  /** representative room used to furnish the satellite interior preview */
  featuredRoom: RoomResponse | null;
  items: ResolvedItem[];
}

export interface SceneModel {
  /** one building per Location, in id order */
  sites: Site[];
  sitesByKey: Map<string, Site>;
  itemsById: Map<number, ResolvedItem>;
  floorsById: Map<number, FloorResponse>;
  roomsById: Map<number, RoomResponse>;
  containersById: Map<number, ContainerResponse>;
  /** resolved items grouped by the id of the room they ultimately live in */
  itemsByRoom: Map<number, ResolvedItem[]>;
  /** items that resolve to no room at all */
  unassigned: ResolvedItem[];
  /** ids of items carrying the "Furniture" tag */
  furnitureItemIds: Set<number>;
  typeCounts: Map<number, number>;
  totalItems: number;
}

/** Furniture set for a room, shared by dollhouse rooms and satellite interiors. */
export function furnitureForRoom(room: RoomResponse | null): FurnitureKind {
  if (!room) return 'generic';
  return furnitureFor(room.roomType, room.name);
}

/**
 * Resolve the room an item ultimately lives in. An item sits either directly in
 * a Room (roomId) or inside a Container (containerId); a container is owned by a
 * Room or nests inside another container, so we walk parents until we reach the
 * container that sits in a room. Returns the immediate container too (for the
 * detail breadcrumb).
 */
function resolvePlacement(
  item: ItemResponse,
  roomsById: Map<number, RoomResponse>,
  containersById: Map<number, ContainerResponse>,
): { room: RoomResponse | null; container: ContainerResponse | null } {
  if (item.roomId != null) {
    return { room: roomsById.get(item.roomId) ?? null, container: null };
  }
  if (item.containerId != null) {
    const container = containersById.get(item.containerId) ?? null;
    let c: ContainerResponse | null = container;
    const seen = new Set<number>();
    while (c && c.roomId == null && c.parentContainerId != null && !seen.has(c.id)) {
      seen.add(c.id);
      c = containersById.get(c.parentContainerId) ?? null;
    }
    const room = c && c.roomId != null ? (roomsById.get(c.roomId) ?? null) : null;
    return { room, container };
  }
  return { room: null, container: null };
}

/** Ordered trail of names from the location down to the item's immediate parent. */
function buildBreadcrumb(
  item: ItemResponse,
  room: RoomResponse | null,
  floor: FloorResponse | null,
  location: LocationResponse | null,
  containersById: Map<number, ContainerResponse>,
): string[] {
  const parts: string[] = [];
  if (location) parts.push(location.name);
  // only spell the storey out for multi-floor locations — "Car › Main › Trunk" is noise
  if (floor && location && location.floors.length > 1) parts.push(floor.name);
  if (room) parts.push(room.name);
  if (item.containerId != null) {
    const chain: string[] = [];
    let c: ContainerResponse | null = containersById.get(item.containerId) ?? null;
    const seen = new Set<number>();
    while (c && !seen.has(c.id)) {
      seen.add(c.id);
      chain.push(c.name);
      if (c.parentContainerId == null) break;
      c = containersById.get(c.parentContainerId) ?? null;
    }
    chain.reverse();
    parts.push(...chain);
  }
  return parts;
}

export function buildSceneModel(data: CatalogueData): SceneModel {
  const floorsById = new Map<number, FloorResponse>(data.floors.map((f) => [f.id, f]));
  const roomsById = new Map<number, RoomResponse>(data.rooms.map((r) => [r.id, r]));
  const containersById = new Map<number, ContainerResponse>(data.containers.map((c) => [c.id, c]));
  const locationsById = new Map<number, LocationResponse>(data.locations.map((l) => [l.id, l]));
  const personsById = new Map<number, PersonResponse>(data.persons.map((p) => [p.id, p]));

  const resolved: ResolvedItem[] = data.items
    .filter((i) => !i.isDeleted)
    .sort((a, b) => a.id - b.id)
    .map((item) => {
      const { room, container } = resolvePlacement(item, roomsById, containersById);
      const floor = room ? (floorsById.get(room.floorId) ?? null) : null;
      const location = floor ? (locationsById.get(floor.locationId) ?? null) : null;
      const owner = item.ownerId != null ? (personsById.get(item.ownerId) ?? null) : null;
      const breadcrumb = buildBreadcrumb(item, room, floor, location, containersById);
      return { item, container, room, floor, location, owner, breadcrumb };
    });

  // group items by the room they ultimately live in; the rest go on the pallet
  const itemsByRoom = new Map<number, ResolvedItem[]>();
  const unassigned: ResolvedItem[] = [];
  for (const r of resolved) {
    if (r.room) {
      const list = itemsByRoom.get(r.room.id) ?? [];
      list.push(r);
      itemsByRoom.set(r.room.id, list);
    } else {
      unassigned.push(r);
    }
  }

  // one building per Location, on a stable ring around the central dollhouse
  const ordered = [...data.locations].sort((a, b) => a.id - b.id);
  const sites: Site[] = [];
  const sitesByKey = new Map<string, Site>();
  ordered.forEach((location, index) => {
    const floors = [...location.floors].sort((a, b) => a.levelIndex - b.levelIndex);
    const floorIds = new Set(floors.map((f) => f.id));
    const rooms = data.rooms.filter((r) => floorIds.has(r.floorId)).sort((a, b) => a.id - b.id);
    const def: SiteDef = { ...satelliteSlot(index, ordered.length), kind: siteKindFor(location.name) };
    const items = resolved.filter((r) => r.location?.id === location.id);
    const site: Site = {
      key: `loc-${location.id}`,
      label: location.name,
      def,
      location,
      floors,
      rooms,
      featuredRoom: rooms[0] ?? null,
      items,
    };
    sites.push(site);
    sitesByKey.set(site.key, site);
  });

  const itemsById = new Map<number, ResolvedItem>(resolved.map((r) => [r.item.id, r]));
  const typeCounts = new Map<number, number>();
  for (const r of resolved) {
    for (const t of r.item.itemTypes) {
      typeCounts.set(t, (typeCounts.get(t) ?? 0) + 1);
    }
  }

  return {
    sites,
    sitesByKey,
    itemsById,
    floorsById,
    roomsById,
    containersById,
    itemsByRoom,
    unassigned,
    furnitureItemIds: new Set(data.furnitureItemIds),
    typeCounts,
    totalItems: resolved.length,
  };
}

function hasFootprint(room: RoomResponse): boolean {
  return room.widthInches != null && room.depthInches != null && room.widthInches > 0 && room.depthInches > 0;
}

/** Palette for one room: stored colours win, furniture-kind palette fills the gaps. */
function paletteFor(room: RoomResponse, furniture: FurnitureKind): RoomPalette {
  const base = FURNITURE_PALETTES[furniture];
  return {
    wallColor: room.wallColor ?? base.wallColor,
    floorColor: room.floorColor ?? base.floorColor,
    accent: base.accent,
  };
}

/** Top-level containers of a room that carry enough geometry to draw. */
function placeContainers(
  room: RoomResponse,
  containers: ContainerResponse[],
  itemsById: Map<number, ResolvedItem>,
  wallHeight: number,
): PlacedContainer[] {
  const placed: PlacedContainer[] = [];
  for (const c of containers) {
    if (c.roomId !== room.id) continue;
    if (c.positionXInches == null || c.positionYInches == null || c.widthInches == null || c.depthInches == null) continue;
    // count items in this container or anything nested inside it
    let itemCount = 0;
    for (const r of itemsById.values()) {
      let cur: ContainerResponse | null = r.container;
      const seen = new Set<number>();
      while (cur && !seen.has(cur.id)) {
        if (cur.id === c.id) {
          itemCount += 1;
          break;
        }
        seen.add(cur.id);
        cur = cur.parentContainerId != null ? (containers.find((p) => p.id === cur!.parentContainerId) ?? null) : null;
      }
    }
    const h = Math.min(wallHeight * 0.92, Math.max(0.2, (c.heightInches ?? 24) / 40));
    placed.push({
      container: c,
      x: u(c.positionXInches),
      z: u(c.positionYInches),
      w: Math.max(0.25, u(c.widthInches)),
      d: Math.max(0.25, u(c.depthInches)),
      h,
      y: Math.max(0, Math.min((c.positionZInches ?? 0) / 40, wallHeight - h)),
      rotation: c.rotation ?? 0,
      color: c.color ?? '#c0a884',
      itemCount,
    });
  }
  return placed;
}

function placeDoors(room: RoomResponse, rect: Rect, doors: DoorResponse[], wallHeight: number): PlacedDoor[] {
  const placed: PlacedDoor[] = [];
  for (const door of doors) {
    if (door.fromRoomId !== room.id) continue;
    const along = door.wall === 0 || door.wall === 2 ? rect.w : rect.d; // north/south run along x
    const w = Math.min(along * 0.9, Math.max(0.4, u(door.widthInches)));
    const offset = Math.min(Math.max(0, u(door.offsetInches)), Math.max(0, along - w));
    placed.push({
      door,
      wall: door.wall,
      offset,
      w,
      h: Math.min(wallHeight * 0.92, Math.max(0.5, door.heightInches / 40)),
    });
  }
  return placed;
}

function placeStairs(room: RoomResponse, rect: Rect, stairs: StairResponse[]): PlacedStair[] {
  const placed: PlacedStair[] = [];
  for (const stair of stairs) {
    if (stair.fromRoomId !== room.id) continue;
    placed.push({
      stair,
      x: stair.positionXInches != null ? u(stair.positionXInches) : rect.w * 0.15,
      z: stair.positionYInches != null ? u(stair.positionYInches) : rect.d * 0.15,
      run: stair.runInches != null ? Math.max(1.2, u(stair.runInches)) : 3,
      w: stair.widthInches != null ? Math.max(0.6, u(stair.widthInches)) : 1.4,
      // stairs always climb one storey visually so they read as a connection
      rise: 2.6,
      steps: stair.stepCount ?? 10,
      rotation: stair.rotation ?? 0,
    });
  }
  return placed;
}

/**
 * Lay a Location's rooms onto the central dollhouse stage from their measured
 * geometry (Origin/Width/Depth in inches, scaled to scene units), floor by
 * floor. Rooms without geometry get auto-placed footprints east of the measured
 * ones. The whole location is then translated so its bounding box sits centred
 * on the stage. Interior walls — any wall with another room's floor space
 * behind it on the same level — become half walls so the camera sees in; only
 * true perimeter walls stay full height.
 */
export function placeRooms(site: Site, model: SceneModel, data: CatalogueData): PlacedRoom[] {
  const placed: PlacedRoom[] = [];

  for (const floor of site.floors) {
    const rooms = site.rooms.filter((r) => r.floorId === floor.id);
    const measured = rooms.filter(hasFootprint);
    const unmeasured = rooms.filter((r) => !hasFootprint(r));

    // measured rooms use their stored plan positions on this storey
    const floorPlaced: PlacedRoom[] = measured.map((room) => {
      const rect: Rect = {
        x: u(room.originXInches ?? 0),
        z: u(room.originYInches ?? 0),
        w: u(room.widthInches!),
        d: u(room.depthInches!),
      };
      return makePlacedRoom(room, floor, rect, true, model, data);
    });

    // unmeasured rooms line up east of the measured footprint
    const measuredMaxX = floorPlaced.reduce((max, p) => Math.max(max, p.rect.x + p.rect.w), 0);
    unmeasured.forEach((room, i) => {
      const fb = fallbackRect(i);
      const rect: Rect = { ...fb, x: fb.x + (measuredMaxX > 0 ? measuredMaxX + 1.2 : 0) };
      floorPlaced.push(makePlacedRoom(room, floor, rect, false, model, data));
    });

    placed.push(...floorPlaced);
  }

  // centre the whole location on the stage
  if (placed.length > 0) {
    let minX = Infinity;
    let minZ = Infinity;
    let maxX = -Infinity;
    let maxZ = -Infinity;
    for (const p of placed) {
      minX = Math.min(minX, p.rect.x);
      minZ = Math.min(minZ, p.rect.z);
      maxX = Math.max(maxX, p.rect.x + p.rect.w);
      maxZ = Math.max(maxZ, p.rect.z + p.rect.d);
    }
    const dx = STAGE_CENTER[0] - (minX + maxX) / 2;
    const dz = STAGE_CENTER[1] - (minZ + maxZ) / 2;
    for (const p of placed) {
      p.rect.x += dx;
      p.rect.z += dz;
    }
  }

  // interior-wall detection (axis-aligned rooms only): a wall is interior when
  // any other room on the same level starts behind it and overlaps its run —
  // adjacent, across a hallway gap, or straddling. Interior walls render half.
  for (const p of placed) {
    if (p.rotation !== 0) continue;
    const r = p.rect;
    const neighbours = placed.filter((o) => o !== p && o.level === p.level && o.rotation === 0);
    p.northInterior = neighbours.some(
      (o) =>
        o.rect.z < r.z - 0.05 &&
        o.rect.x < r.x + r.w - 0.05 &&
        o.rect.x + o.rect.w > r.x + 0.05,
    );
    p.westInterior = neighbours.some(
      (o) =>
        o.rect.x < r.x - 0.05 &&
        o.rect.z < r.z + r.d - 0.05 &&
        o.rect.z + o.rect.d > r.z + 0.05,
    );
  }

  // Doors live on their FromRoom's wall, but rooms only draw north and west
  // walls — on a south/east boundary the only wall standing belongs to the room
  // on the OTHER side. Mirror south/east door openings onto that neighbour's
  // north/west wall (matched geometrically, tolerating the wall-thickness gap
  // between footprints) so doorways are never bricked over.
  const DOOR_GAP = 0.5; // max plan gap between the door's wall and the neighbour's wall, in units
  for (const p of placed) {
    if (p.rotation !== 0) continue;
    for (const o of placed) {
      if (o === p || o.level !== p.level || o.rotation !== 0) continue;
      for (const d of o.doors) {
        if (d.wall === WALL_SOUTH) {
          const doorZ = o.rect.z + o.rect.d;
          const x0 = o.rect.x + d.offset;
          if (
            Math.abs(doorZ - p.rect.z) <= DOOR_GAP &&
            x0 + d.w > p.rect.x + 0.05 &&
            x0 < p.rect.x + p.rect.w - 0.05
          ) {
            p.doors.push({ ...d, wall: WALL_NORTH, offset: Math.min(Math.max(0, x0 - p.rect.x), Math.max(0, p.rect.w - d.w)) });
          }
        } else if (d.wall === WALL_EAST) {
          const doorX = o.rect.x + o.rect.w;
          const z0 = o.rect.z + d.offset;
          if (
            Math.abs(doorX - p.rect.x) <= DOOR_GAP &&
            z0 + d.w > p.rect.z + 0.05 &&
            z0 < p.rect.z + p.rect.d - 0.05
          ) {
            p.doors.push({ ...d, wall: WALL_WEST, offset: Math.min(Math.max(0, z0 - p.rect.z), Math.max(0, p.rect.d - d.w)) });
          }
        }
      }
    }
  }
  return placed;
}

function makePlacedRoom(
  room: RoomResponse,
  floor: FloorResponse,
  rect: Rect,
  hasGeometry: boolean,
  model: SceneModel,
  data: CatalogueData,
): PlacedRoom {
  const furniture = furnitureForRoom(room);
  const wallHeight = wallHeightFor(room.heightInches);
  const items = model.itemsByRoom.get(room.id) ?? [];
  return {
    room,
    floor,
    level: floor.levelIndex,
    rect,
    rotation: room.rotation ?? 0,
    wallHeight,
    colors: paletteFor(room, furniture),
    hasGeometry,
    items,
    furnishings: items.filter((r) => r.item.roomId === room.id && model.furnitureItemIds.has(r.item.id)),
    containers: placeContainers(room, data.containers, model.itemsById, wallHeight),
    doors: placeDoors(room, rect, data.doors, wallHeight),
    stairs: placeStairs(room, rect, data.stairs),
    northInterior: false,
    westInterior: false,
  };
}

/** Bounding box of a set of placed rooms, for the plinth and roof. */
export function placedBounds(placed: PlacedRoom[]): Rect | null {
  if (placed.length === 0) return null;
  let minX = Infinity;
  let minZ = Infinity;
  let maxX = -Infinity;
  let maxZ = -Infinity;
  for (const p of placed) {
    minX = Math.min(minX, p.rect.x);
    minZ = Math.min(minZ, p.rect.z);
    maxX = Math.max(maxX, p.rect.x + p.rect.w);
    maxZ = Math.max(maxZ, p.rect.z + p.rect.d);
  }
  return { x: minX, z: minZ, w: maxX - minX, d: maxZ - minZ };
}

/** World-space centre of a placed room, used as a camera fly-to target. */
export function roomCenter(placed: PlacedRoom): [number, number, number] {
  const { rect, level } = placed;
  return [rect.x + rect.w / 2, levelY(level) + 1.0, rect.z + rect.d / 2];
}

export function selectionEquals(a: Selection, b: Selection): boolean {
  if (a === null || b === null) return a === b;
  if (a.kind === 'item' && b.kind === 'item') return a.id === b.id;
  if (a.kind === 'container' && b.kind === 'container') return a.id === b.id;
  if (a.kind === 'room' && b.kind === 'room') return a.roomId === b.roomId;
  if (a.kind === 'location' && b.kind === 'location') return a.id === b.id;
  return false;
}

export function formatPrice(value: number | null): string {
  if (value == null) return '—';
  return value.toLocaleString('en-US', { style: 'currency', currency: 'USD' });
}

/** The figure shown for an item: present-day worth, falling back to purchase price. */
export function itemValue(item: ItemResponse): number | null {
  return item.currentValue ?? item.purchasePrice ?? null;
}

export function primaryType(item: ItemResponse): number {
  return item.itemTypes.length > 0 ? item.itemTypes[0] : 0;
}

// re-export for scene components that need the shared interior footprint
export { SITE_INTERIOR };
