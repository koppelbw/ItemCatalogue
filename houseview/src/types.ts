// Transport shapes mirroring the ItemCatalogue API DTOs (Application/DTOs/*Dtos.cs).
// ASP.NET Core serializes with camelCase property names and enums as NUMBERS
// (no JsonStringEnumConverter) — except ItemEvent.eventType, which is a string in
// its DTO. byte[] RowVersion rides the wire as a base64 string and must be sent
// back unchanged on every update (the server rejects an empty token).

/** byte[] rowversion, base64-encoded on the wire. Opaque concurrency token. */
export type RowVersion = string;

// --- response shapes -------------------------------------------------------

export interface RoomResponse {
  id: number;
  name: string;
  description: string | null;
  locationId: number;
  rowVersion: RowVersion;
}

export interface LocationResponse {
  id: number;
  name: string;
  description: string | null;
  rooms: RoomResponse[];
  rowVersion: RowVersion;
}

export interface ContainerResponse {
  id: number;
  name: string;
  description: string | null;
  // Exactly one of these is set: roomId for a top-level container, parentContainerId for a nested one.
  roomId: number | null;
  parentContainerId: number | null;
  rowVersion: RowVersion;
}

export interface ItemResponse {
  id: number;
  name: string;
  description: string | null;
  itemTypes: number[];
  purchasePrice: number | null;
  currentValue: number | null;
  brand: string | null;
  model: string | null;
  serialNumber: string | null;
  purchasedFrom: string | null;
  quantity: number;
  condition: number | null;
  acquisitionType: number | null;
  purchaseDate: string | null;
  warrantyExpiryDate: string | null;
  isStored: boolean;
  isDeleted: boolean;
  reasonForDeletion: number | null;
  // An item lives in a Room xor a Container (never both, never a Location directly).
  roomId: number | null;
  containerId: number | null;
  ownerId: number | null;
  releaseDate: string | null;
  valuationDate: string | null;
  acquisitionReference: string | null;
  createdDate: string;
  lastModifiedDate: string | null;
  rowVersion: RowVersion;
}

export interface PersonResponse {
  id: number;
  name: string;
  rowVersion: RowVersion;
}

export interface TagResponse {
  id: number;
  name: string;
  description: string | null;
  rowVersion: RowVersion;
}

export interface ItemTagsResponse {
  itemId: number;
  tags: TagResponse[];
}

export interface CollectionItemResponse {
  itemId: number;
  itemName: string;
  quantity: number;
  sortOrder: number;
  role: string | null;
}

export interface CollectionResponse {
  id: number;
  name: string;
  description: string | null;
  items: CollectionItemResponse[];
  rowVersion: RowVersion;
}

export interface ItemEventResponse {
  id: number;
  itemId: number;
  /** ItemEventType serialized as a string (unlike the numeric enums elsewhere). */
  eventType: string;
  occurredAt: string;
  oldValue: string | null;
  newValue: string | null;
  notes: string | null;
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

// --- write-request shapes (request bodies, also camelCase) -----------------

export interface CreateItemRequest {
  name: string;
  description: string | null;
  itemTypes: number[];
  purchasePrice: number | null;
  currentValue: number | null;
  brand: string | null;
  model: string | null;
  serialNumber: string | null;
  purchasedFrom: string | null;
  quantity: number;
  condition: number | null;
  acquisitionType: number | null;
  purchaseDate: string | null;
  warrantyExpiryDate: string | null;
  isStored: boolean;
  roomId: number | null;
  containerId: number | null;
  ownerId: number | null;
  releaseDate: string | null;
  valuationDate: string | null;
  acquisitionReference: string | null;
}

export interface UpdateItemRequest extends CreateItemRequest {
  id: number;
  rowVersion: RowVersion;
}

export interface CreateRoomRequest {
  name: string;
  description: string | null;
  locationId: number;
}

export interface UpdateRoomRequest extends CreateRoomRequest {
  id: number;
  rowVersion: RowVersion;
}

export interface CreateLocationRequest {
  name: string;
  description: string | null;
}

export interface UpdateLocationRequest extends CreateLocationRequest {
  id: number;
  rowVersion: RowVersion;
}

export interface CreateContainerRequest {
  name: string;
  description: string | null;
  roomId: number | null;
  parentContainerId: number | null;
}

export interface UpdateContainerRequest extends CreateContainerRequest {
  id: number;
  rowVersion: RowVersion;
}

export interface CreatePersonRequest {
  name: string;
}

export interface UpdatePersonRequest extends CreatePersonRequest {
  id: number;
  rowVersion: RowVersion;
}

export interface CreateTagRequest {
  name: string;
  description: string | null;
}

export interface UpdateTagRequest extends CreateTagRequest {
  id: number;
  rowVersion: RowVersion;
}

/** Replaces an item's full tag set; send an empty list to clear. */
export interface SetItemTagsRequest {
  tagIds: number[];
}

export interface CreateCollectionRequest {
  name: string;
  description: string | null;
}

export interface UpdateCollectionRequest extends CreateCollectionRequest {
  id: number;
  rowVersion: RowVersion;
}

export interface AddCollectionItemRequest {
  itemId: number;
  quantity: number;
  sortOrder: number | null;
  role: string | null;
}

export interface UpdateCollectionItemRequest {
  quantity: number;
  sortOrder: number | null;
  role: string | null;
}

// --- enum label maps (indexed by the numeric ordinal from Domain/Enums) -----

// Domain.Enums.ItemType
export const ITEM_TYPE_NAMES = ['Electronics', 'Bathroom', 'Cleaning supplies', 'Bedding', 'Books'] as const;

// Domain.Enums.Condition
export const CONDITION_NAMES = ['New', 'Like new', 'Good', 'Fair', 'Poor', 'For repair', 'Broken'] as const;

// Domain.Enums.AcquisitionType
export const ACQUISITION_TYPE_NAMES = ['Purchased', 'Gift', 'Inherited', 'Found', 'Built myself'] as const;

// Domain.Enums.DeletedReason
export const DELETED_REASON_NAMES = ['Used', 'Broken', 'Donated', 'Gifted', 'Lost'] as const;

// Domain.Enums.ItemEventType — keyed by the string the API returns.
export const ITEM_EVENT_TYPE_LABELS: Record<string, string> = {
  Created: 'Created',
  Moved: 'Moved',
  ValueChanged: 'Value changed',
  ConditionChanged: 'Condition changed',
  OwnerChanged: 'Owner changed',
  SoftDeleted: 'Deleted',
};

export const ITEM_TYPE_COLORS = ['#5b8def', '#3ec6b8', '#f2a93b', '#a78bdb', '#ef6f6c'] as const;

// --- assembled client-side shapes ------------------------------------------

export interface CatalogueData {
  locations: LocationResponse[];
  rooms: RoomResponse[];
  containers: ContainerResponse[];
  items: ItemResponse[];
  persons: PersonResponse[];
  /** true when the data came from the live API, false when bundled demo data is shown */
  live: boolean;
}

/**
 * An item joined to its physical chain. Per the new model an item sits in a Room
 * (room set) or inside a Container (container set, walked up to the room that owns
 * the chain); from the room we reach the Location. `breadcrumb` is the ordered
 * trail of names from the location down to the item's immediate parent.
 */
export interface ResolvedItem {
  item: ItemResponse;
  /** the container the item sits directly in, if any */
  container: ContainerResponse | null;
  room: RoomResponse | null;
  location: LocationResponse | null;
  owner: PersonResponse | null;
  /** e.g. ['House', 'Garage', 'Toolbox'] — location, room, then nested containers */
  breadcrumb: string[];
}

export type Selection =
  | { kind: 'item'; id: number }
  | { kind: 'container'; id: number }
  | { kind: 'room'; roomId: number }
  | { kind: 'location'; id: number }
  | null;
