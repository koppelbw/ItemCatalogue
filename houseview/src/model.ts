import {
  CAR_ROOM_MATCHES,
  ROOM_DEFS,
  SITE_DEFS,
  cabinSiteDef,
  extraRoomDef,
  levelY,
  type CarSlot,
  type FurnitureKind,
  type RoomDef,
  type SiteDef,
} from './layout';
import type { CatalogueData, ItemResponse, LocationResponse, PersonResponse, ResolvedItem, RoomResponse, Selection } from './types';

/** A database room placed somewhere inside the main house. */
export interface PlacedRoom {
  room: RoomResponse;
  def: RoomDef;
  items: ResolvedItem[];
  /** true when another room on the same level sits behind this wall - rendered as a half wall */
  northInterior: boolean;
  westInterior: boolean;
}

export interface CarRoom {
  slot: CarSlot;
  room: RoomResponse;
  items: ResolvedItem[];
}

/** A database Location rendered as its own building in the neighbourhood. */
export interface Site {
  /** lowercased location name; 'house' and 'car' are special */
  key: string;
  label: string;
  def: SiteDef;
  location: LocationResponse | null;
  /** the room this location references - decides the interior furnishing */
  featuredRoom: RoomResponse | null;
  items: ResolvedItem[];
}

export interface SceneModel {
  /** all sites in dock order: house first, then locations by id, car last */
  sites: Site[];
  placedRooms: PlacedRoom[];
  carRooms: CarRoom[];
  /** items with no location at all */
  unassigned: ResolvedItem[];
  itemsById: Map<number, ResolvedItem>;
  roomsById: Map<number, RoomResponse>;
  typeCounts: Map<number, number>;
  totalItems: number;
}

export function siteKeyForLocation(location: LocationResponse): string {
  return location.name.trim().toLowerCase();
}

/** Furniture set for a room name, shared by house rooms and site interiors. */
export function furnitureForRoom(room: RoomResponse | null): FurnitureKind {
  if (!room) return 'generic';
  return ROOM_DEFS.find((d) => d.match === room.name.trim().toLowerCase())?.furniture ?? 'generic';
}

export function buildSceneModel(data: CatalogueData): SceneModel {
  const roomsById = new Map<number, RoomResponse>(data.rooms.map((r) => [r.id, r]));
  const locationsById = new Map<number, LocationResponse>(data.locations.map((l) => [l.id, l]));
  const personsById = new Map<number, PersonResponse>(data.persons.map((p) => [p.id, p]));

  const resolved: ResolvedItem[] = data.items
    .filter((i) => !i.isDeleted)
    .sort((a, b) => a.id - b.id)
    .map((item) => {
      const location = item.locationId != null ? (locationsById.get(item.locationId) ?? null) : null;
      const room = location ? (roomsById.get(location.roomId) ?? null) : null;
      const owner = item.ownerId != null ? (personsById.get(item.ownerId) ?? null) : null;
      return { item, location, room, owner };
    });

  // --- sites: one per distinct location name; house and car are anchored ---
  const houseSite: Site = { key: 'house', label: 'House', def: SITE_DEFS.house, location: null, featuredRoom: null, items: [] };
  const sitesByKey = new Map<string, Site>([['house', houseSite]]);
  const orderedSites: Site[] = [houseSite];
  let carSite: Site | null = null;
  let cabinIndex = 0;

  for (const location of [...data.locations].sort((a, b) => a.id - b.id)) {
    const key = siteKeyForLocation(location);
    const featuredRoom = roomsById.get(location.roomId) ?? null;
    if (key === 'house') {
      if (!houseSite.location) {
        houseSite.location = location;
        houseSite.label = location.name;
        houseSite.featuredRoom = featuredRoom;
      }
      continue;
    }
    if (key === 'car') {
      if (!carSite) {
        carSite = { key, label: location.name, def: SITE_DEFS.car, location, featuredRoom, items: [] };
        sitesByKey.set(key, carSite);
      }
      continue;
    }
    if (!sitesByKey.has(key)) {
      const def = SITE_DEFS[key] ?? cabinSiteDef(cabinIndex++);
      const site: Site = { key, label: location.name, def, location, featuredRoom, items: [] };
      sitesByKey.set(key, site);
      orderedSites.push(site);
    }
  }

  // the car renders whenever the database has car rooms, location or not
  const hasCarRooms = data.rooms.some((r) => CAR_ROOM_MATCHES.some((m) => m === r.name.trim().toLowerCase()));
  if (!carSite && hasCarRooms) {
    carSite = { key: 'car', label: 'Car', def: SITE_DEFS.car, location: null, featuredRoom: null, items: [] };
    sitesByKey.set('car', carSite);
  }
  if (carSite) orderedSites.push(carSite);

  // --- route every item to its location's site ---
  const houseItemsByRoom = new Map<number, ResolvedItem[]>();
  const carItemsByRoom = new Map<number, ResolvedItem[]>();
  const unassigned: ResolvedItem[] = [];
  for (const r of resolved) {
    if (!r.location) {
      unassigned.push(r);
      continue;
    }
    const key = siteKeyForLocation(r.location);
    const site = sitesByKey.get(key);
    site?.items.push(r);
    if (key === 'house') {
      const list = houseItemsByRoom.get(r.location.roomId) ?? [];
      list.push(r);
      houseItemsByRoom.set(r.location.roomId, list);
    } else if (key === 'car') {
      const list = carItemsByRoom.get(r.location.roomId) ?? [];
      list.push(r);
      carItemsByRoom.set(r.location.roomId, list);
    }
  }

  // --- the main house still models every non-car room ---
  const placedRooms: PlacedRoom[] = [];
  const carRooms: CarRoom[] = [];
  let extraIndex = 0;
  for (const room of [...data.rooms].sort((a, b) => a.id - b.id)) {
    const key = room.name.trim().toLowerCase();
    const carSlot = CAR_ROOM_MATCHES.find((m) => m === key);
    if (carSlot) {
      carRooms.push({ slot: carSlot, room, items: carItemsByRoom.get(room.id) ?? [] });
      continue;
    }
    const def = ROOM_DEFS.find((d) => d.match === key) ?? extraRoomDef(extraIndex++);
    placedRooms.push({ room, def, items: houseItemsByRoom.get(room.id) ?? [], northInterior: false, westInterior: false });
  }

  // Walls with another room directly behind them become half walls so every
  // room stays visible from the default south-east camera.
  for (const p of placedRooms) {
    const r = p.def.rect;
    const neighbours = placedRooms.filter((o) => o !== p && o.def.level === p.def.level);
    p.northInterior = neighbours.some(
      (o) =>
        Math.abs(o.def.rect.z + o.def.rect.d - r.z) < 0.05 &&
        o.def.rect.x < r.x + r.w - 0.05 &&
        o.def.rect.x + o.def.rect.w > r.x + 0.05,
    );
    p.westInterior = neighbours.some(
      (o) =>
        Math.abs(o.def.rect.x + o.def.rect.w - r.x) < 0.05 &&
        o.def.rect.z < r.z + r.d - 0.05 &&
        o.def.rect.z + o.def.rect.d > r.z + 0.05,
    );
  }

  const itemsById = new Map<number, ResolvedItem>(resolved.map((r) => [r.item.id, r]));
  const typeCounts = new Map<number, number>();
  for (const r of resolved) {
    for (const t of r.item.itemTypes) {
      typeCounts.set(t, (typeCounts.get(t) ?? 0) + 1);
    }
  }

  return { sites: orderedSites, placedRooms, carRooms, unassigned, itemsById, roomsById, typeCounts, totalItems: resolved.length };
}

/** World-space centre of a placed room, used as a camera fly-to target. */
export function roomCenter(placed: PlacedRoom): [number, number, number] {
  const { rect, level } = placed.def;
  return [rect.x + rect.w / 2, levelY(level) + 1.0, rect.z + rect.d / 2];
}

export function selectionEquals(a: Selection, b: Selection): boolean {
  if (a === null || b === null) return a === b;
  if (a.kind === 'item' && b.kind === 'item') return a.id === b.id;
  if (a.kind === 'room' && b.kind === 'room') return a.roomId === b.roomId;
  if (a.kind === 'location' && b.kind === 'location') return a.id === b.id;
  return false;
}

export function formatPrice(price: number | null): string {
  if (price == null) return '—';
  return price.toLocaleString('en-US', { style: 'currency', currency: 'USD' });
}

export function primaryType(item: ItemResponse): number {
  return item.itemTypes.length > 0 ? item.itemTypes[0] : 0;
}
