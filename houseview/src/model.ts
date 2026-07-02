import {
  ROOM_DEFS,
  SITE_DEFS,
  extraRoomDef,
  levelY,
  satelliteSlot,
  type FurnitureKind,
  type RoomDef,
  type SiteDef,
  type SiteKind,
} from './layout';
import type {
  CatalogueData,
  ContainerResponse,
  ItemResponse,
  LocationResponse,
  PersonResponse,
  ResolvedItem,
  RoomResponse,
  Selection,
} from './types';

/** A database room placed onto a footprint inside the active dollhouse. */
export interface PlacedRoom {
  room: RoomResponse;
  def: RoomDef;
  items: ResolvedItem[];
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
  /** the rooms that belong to this location */
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
  roomsById: Map<number, RoomResponse>;
  containersById: Map<number, ContainerResponse>;
  /** resolved items grouped by the id of the room they ultimately live in */
  itemsByRoom: Map<number, ResolvedItem[]>;
  /** items that resolve to no room at all */
  unassigned: ResolvedItem[];
  typeCounts: Map<number, number>;
  totalItems: number;
}

/** Furniture set for a room name, shared by dollhouse rooms and satellite interiors. */
export function furnitureForRoom(room: RoomResponse | null): FurnitureKind {
  if (!room) return 'generic';
  return ROOM_DEFS.find((d) => d.match === room.name.trim().toLowerCase())?.furniture ?? 'generic';
}

/** Shell style for a Location: matched by name, otherwise a generic cabin. */
function siteKindFor(name: string): SiteKind {
  const known = SITE_DEFS[name.trim().toLowerCase()];
  return known && known.kind !== 'house' ? known.kind : 'cabin';
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
  location: LocationResponse | null,
  containersById: Map<number, ContainerResponse>,
): string[] {
  const parts: string[] = [];
  if (location) parts.push(location.name);
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
  const roomsById = new Map<number, RoomResponse>(data.rooms.map((r) => [r.id, r]));
  const containersById = new Map<number, ContainerResponse>(data.containers.map((c) => [c.id, c]));
  const locationsById = new Map<number, LocationResponse>(data.locations.map((l) => [l.id, l]));
  const personsById = new Map<number, PersonResponse>(data.persons.map((p) => [p.id, p]));

  const resolved: ResolvedItem[] = data.items
    .filter((i) => !i.isDeleted)
    .sort((a, b) => a.id - b.id)
    .map((item) => {
      const { room, container } = resolvePlacement(item, roomsById, containersById);
      const location = room ? (locationsById.get(room.locationId) ?? null) : null;
      const owner = item.ownerId != null ? (personsById.get(item.ownerId) ?? null) : null;
      const breadcrumb = buildBreadcrumb(item, room, location, containersById);
      return { item, container, room, location, owner, breadcrumb };
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
    const rooms = data.rooms.filter((r) => r.locationId === location.id).sort((a, b) => a.id - b.id);
    const def: SiteDef = { ...satelliteSlot(index, ordered.length), kind: siteKindFor(location.name) };
    const items = resolved.filter((r) => r.location?.id === location.id);
    const site: Site = {
      key: `loc-${location.id}`,
      label: location.name,
      def,
      location,
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
    roomsById,
    containersById,
    itemsByRoom,
    unassigned,
    typeCounts,
    totalItems: resolved.length,
  };
}

/**
 * Lay a Location's rooms onto the central dollhouse footprints: matched by name
 * to a known footprint, otherwise auto-placed as an extra cabin. Walls that back
 * onto another room on the same level become half walls so the camera sees in.
 */
export function placeRooms(rooms: RoomResponse[], itemsByRoom: Map<number, ResolvedItem[]>): PlacedRoom[] {
  const placed: PlacedRoom[] = [];
  let extraIndex = 0;
  for (const room of [...rooms].sort((a, b) => a.id - b.id)) {
    const key = room.name.trim().toLowerCase();
    const def = ROOM_DEFS.find((d) => d.match === key) ?? extraRoomDef(extraIndex++);
    placed.push({ room, def, items: itemsByRoom.get(room.id) ?? [], northInterior: false, westInterior: false });
  }
  for (const p of placed) {
    const r = p.def.rect;
    const neighbours = placed.filter((o) => o !== p && o.def.level === p.def.level);
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
  return placed;
}

/** World-space centre of a placed room, used as a camera fly-to target. */
export function roomCenter(placed: PlacedRoom): [number, number, number] {
  const { rect, level } = placed.def;
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
