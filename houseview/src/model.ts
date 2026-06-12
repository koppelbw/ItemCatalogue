import { CAR_ROOM_MATCHES, ROOM_DEFS, extraRoomDef, levelY, type CarSlot, type RoomDef } from './layout';
import type { CatalogueData, ItemResponse, LocationResponse, PersonResponse, ResolvedItem, RoomResponse, Selection } from './types';

/** A database room placed somewhere in the 3D world. */
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

export interface SceneModel {
  placedRooms: PlacedRoom[];
  carRooms: CarRoom[];
  /** items whose location's room matched nothing (or that have no location) */
  unassigned: ResolvedItem[];
  itemsById: Map<number, ResolvedItem>;
  roomsById: Map<number, RoomResponse>;
  typeCounts: Map<number, number>;
  totalItems: number;
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

  const itemsByRoomId = new Map<number, ResolvedItem[]>();
  const unassigned: ResolvedItem[] = [];
  for (const r of resolved) {
    if (r.room) {
      const list = itemsByRoomId.get(r.room.id) ?? [];
      list.push(r);
      itemsByRoomId.set(r.room.id, list);
    } else {
      unassigned.push(r);
    }
  }

  const placedRooms: PlacedRoom[] = [];
  const carRooms: CarRoom[] = [];
  let extraIndex = 0;
  for (const room of [...data.rooms].sort((a, b) => a.id - b.id)) {
    const key = room.name.trim().toLowerCase();
    const carSlot = CAR_ROOM_MATCHES.find((m) => m === key);
    if (carSlot) {
      carRooms.push({ slot: carSlot, room, items: itemsByRoomId.get(room.id) ?? [] });
      continue;
    }
    const def = ROOM_DEFS.find((d) => d.match === key) ?? extraRoomDef(extraIndex++);
    placedRooms.push({ room, def, items: itemsByRoomId.get(room.id) ?? [], northInterior: false, westInterior: false });
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

  return { placedRooms, carRooms, unassigned, itemsById, roomsById, typeCounts, totalItems: resolved.length };
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
  return false;
}

export function formatPrice(price: number | null): string {
  if (price == null) return '—';
  return price.toLocaleString('en-US', { style: 'currency', currency: 'USD' });
}

export function primaryType(item: ItemResponse): number {
  return item.itemTypes.length > 0 ? item.itemTypes[0] : 0;
}
