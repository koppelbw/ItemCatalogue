// Transport shapes mirroring the ItemCatalogue API response DTOs.
// ASP.NET Core serializes with camelCase property names and enums as numbers.

export interface RoomResponse {
  id: number;
  name: string;
  description: string | null;
}

export interface LocationResponse {
  id: number;
  name: string;
  description: string | null;
  roomId: number;
}

export interface ItemResponse {
  id: number;
  name: string;
  description: string | null;
  itemTypes: number[];
  price: number | null;
  isStored: boolean;
  isDeleted: boolean;
  reasonForDeletion: number | null;
  locationId: number | null;
  ownerId: number | null;
  createdDate: string;
  lastModifiedDate: string | null;
}

export interface PersonResponse {
  id: number;
  name: string;
}

export interface PagedResponse<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
  hasNext: boolean;
  hasPrevious: boolean;
}

// Domain.Enums.ItemType ordinals.
export const ITEM_TYPE_NAMES = ['Electronics', 'Bathroom', 'Cleaning supplies', 'Bedding', 'Books'] as const;

// Domain.Enums.DeletedReason is project-specific; show the ordinal if we don't know a label.
export const DELETED_REASON_NAMES: Record<number, string> = {
  0: 'Unknown',
  1: 'Broken',
  2: 'Sold',
  3: 'Donated',
  4: 'Lost',
};

export const ITEM_TYPE_COLORS = ['#5b8def', '#3ec6b8', '#f2a93b', '#a78bdb', '#ef6f6c'] as const;

export interface CatalogueData {
  rooms: RoomResponse[];
  locations: LocationResponse[];
  items: ItemResponse[];
  persons: PersonResponse[];
  /** true when the data came from the live API, false when bundled demo data is shown */
  live: boolean;
}

/** An item joined to its location/room/owner for easy display. */
export interface ResolvedItem {
  item: ItemResponse;
  location: LocationResponse | null;
  room: RoomResponse | null;
  owner: PersonResponse | null;
}

export type Selection =
  | { kind: 'item'; id: number }
  | { kind: 'room'; roomId: number }
  | null;
