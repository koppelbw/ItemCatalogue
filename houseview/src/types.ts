// Transport shapes mirroring the ItemCatalogue API DTOs (Application/DTOs/*Dtos.cs).
// ASP.NET Core serializes with camelCase property names and enums as NUMBERS
// (no JsonStringEnumConverter) — except ItemEvent.eventType, which is a string in
// its DTO. byte[] RowVersion rides the wire as a base64 string and must be sent
// back unchanged on every update (the server rejects an empty token).
//
// Spatial model: Location → Floor (levelIndex) → Room → Container (nestable) → Item.
// Rooms, containers, doors and stairs carry optional real-world geometry in INCHES;
// geometry is nullable because an entity may be catalogued before it is measured.

/** byte[] rowversion, base64-encoded on the wire. Opaque concurrency token. */
export type RowVersion = string;

// --- response shapes -------------------------------------------------------

export interface FloorResponse {
  id: number;
  name: string;
  locationId: number;
  /** signed vertical order within the location: basement = -1, ground = 0, … */
  levelIndex: number;
  elevationInches: number | null;
  ceilingHeightInches: number | null;
  rowVersion: RowVersion;
}

export interface LocationResponse {
  id: number;
  name: string;
  description: string | null;
  floors: FloorResponse[];
  rowVersion: RowVersion;
}

export interface RoomResponse {
  id: number;
  name: string;
  description: string | null;
  floorId: number;
  /** Domain.Enums.RoomType ordinal */
  roomType: number | null;
  // Plan-view footprint in inches: origin is the room's NW corner within the floor.
  originXInches: number | null;
  originYInches: number | null;
  widthInches: number | null;
  depthInches: number | null;
  heightInches: number | null;
  /** degrees on [0, 360) */
  rotation: number | null;
  /** "#RRGGBB" or "#RRGGBBAA" */
  wallColor: string | null;
  floorColor: string | null;
  ceilingColor: string | null;
  rowVersion: RowVersion;
}

export interface ContainerResponse {
  id: number;
  name: string;
  description: string | null;
  // Exactly one of these is set: roomId for a top-level container, parentContainerId for a nested one.
  roomId: number | null;
  parentContainerId: number | null;
  /** Domain.Enums.ContainerType ordinal */
  containerType: number | null;
  // Placement in inches: top-level containers in room space, nested ones in parent space.
  positionXInches: number | null;
  positionYInches: number | null;
  positionZInches: number | null;
  rotation: number | null;
  widthInches: number | null;
  depthInches: number | null;
  heightInches: number | null;
  color: string | null;
  rowVersion: RowVersion;
}

export interface DoorResponse {
  id: number;
  name: string | null;
  /** Domain.Enums.DoorKind ordinal */
  kind: number;
  fromRoomId: number;
  /** null = the door leads outside */
  toRoomId: number | null;
  /** Domain.Enums.Wall ordinal: which wall of the FromRoom the door sits on */
  wall: number;
  /** distance along the wall from its min corner, in inches */
  offsetInches: number;
  widthInches: number;
  heightInches: number;
  /** Domain.Enums.HingeSide ordinal */
  hingeSide: number | null;
  /** Domain.Enums.Swing ordinal */
  swing: number | null;
  rowVersion: RowVersion;
}

export interface StairResponse {
  id: number;
  name: string | null;
  /** the lower room */
  fromRoomId: number;
  /** null = leads to an exterior level */
  toRoomId: number | null;
  /** Domain.Enums.StairShape ordinal */
  shape: number;
  positionXInches: number | null;
  positionYInches: number | null;
  rotation: number | null;
  runInches: number | null;
  widthInches: number | null;
  riseInches: number | null;
  stepCount: number | null;
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

export interface CreateFloorRequest {
  name: string;
  locationId: number;
  levelIndex: number;
  elevationInches: number | null;
  ceilingHeightInches: number | null;
}

export interface UpdateFloorRequest extends CreateFloorRequest {
  id: number;
  rowVersion: RowVersion;
}

export interface CreateRoomRequest {
  name: string;
  description: string | null;
  floorId: number;
  roomType: number | null;
  originXInches: number | null;
  originYInches: number | null;
  widthInches: number | null;
  depthInches: number | null;
  heightInches: number | null;
  rotation: number | null;
  wallColor: string | null;
  floorColor: string | null;
  ceilingColor: string | null;
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
  containerType: number | null;
  positionXInches: number | null;
  positionYInches: number | null;
  positionZInches: number | null;
  rotation: number | null;
  widthInches: number | null;
  depthInches: number | null;
  heightInches: number | null;
  color: string | null;
}

export interface UpdateContainerRequest extends CreateContainerRequest {
  id: number;
  rowVersion: RowVersion;
}

export interface CreateDoorRequest {
  name: string | null;
  kind: number;
  fromRoomId: number;
  toRoomId: number | null;
  wall: number;
  offsetInches: number;
  widthInches: number;
  heightInches: number;
  hingeSide: number | null;
  swing: number | null;
}

export interface UpdateDoorRequest extends CreateDoorRequest {
  id: number;
  rowVersion: RowVersion;
}

export interface CreateStairRequest {
  name: string | null;
  fromRoomId: number;
  toRoomId: number | null;
  shape: number;
  positionXInches: number | null;
  positionYInches: number | null;
  rotation: number | null;
  runInches: number | null;
  widthInches: number | null;
  riseInches: number | null;
  stepCount: number | null;
}

export interface UpdateStairRequest extends CreateStairRequest {
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

// Domain.Enums.RoomType
export const ROOM_TYPE_NAMES = [
  'Bedroom',
  'Bathroom',
  'Kitchen',
  'Living room',
  'Dining room',
  'Hallway',
  'Office',
  'Garage',
  'Laundry',
  'Closet',
  'Basement',
  'Attic',
  'Other',
] as const;

// Domain.Enums.ContainerType
export const CONTAINER_TYPE_NAMES = ['Box', 'Shelf', 'Cabinet', 'Drawer', 'Bin', 'Wardrobe', 'Chest', 'Crate', 'Other'] as const;

// Domain.Enums.DoorKind
export const DOOR_KIND_NAMES = ['Door', 'Doorway', 'Sliding door', 'Garage'] as const;

// Domain.Enums.Wall — which wall of the room a door sits on
export const WALL_NAMES = ['North', 'East', 'South', 'West'] as const;
export const WALL_NORTH = 0;
export const WALL_EAST = 1;
export const WALL_SOUTH = 2;
export const WALL_WEST = 3;

// Domain.Enums.HingeSide
export const HINGE_SIDE_NAMES = ['Left', 'Right'] as const;

// Domain.Enums.Swing
export const SWING_NAMES = ['Inward', 'Outward'] as const;

// Domain.Enums.StairShape
export const STAIR_SHAPE_NAMES = ['Straight', 'L-shaped', 'U-shaped', 'Spiral', 'Winder'] as const;

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
  /** flattened from locations[].floors for direct lookups */
  floors: FloorResponse[];
  rooms: RoomResponse[];
  containers: ContainerResponse[];
  doors: DoorResponse[];
  stairs: StairResponse[];
  items: ItemResponse[];
  persons: PersonResponse[];
  /** ids of items carrying the "Furniture" tag — the only items drawn inside rooms */
  furnitureItemIds: number[];
  /** true when the data came from the live API, false when bundled demo data is shown */
  live: boolean;
}

/**
 * An item joined to its physical chain. Per the entity model an item sits in a
 * Room (room set) or inside a Container (container set, walked up to the room
 * that owns the chain); from the room we reach the Floor and then the Location.
 * `breadcrumb` is the ordered trail of names from the location down to the
 * item's immediate parent.
 */
export interface ResolvedItem {
  item: ItemResponse;
  /** the container the item sits directly in, if any */
  container: ContainerResponse | null;
  room: RoomResponse | null;
  floor: FloorResponse | null;
  location: LocationResponse | null;
  owner: PersonResponse | null;
  /** e.g. ['House', 'First Floor', 'Garage', 'Toolbox'] */
  breadcrumb: string[];
}

export type Selection =
  | { kind: 'item'; id: number }
  | { kind: 'container'; id: number }
  | { kind: 'room'; roomId: number }
  | { kind: 'location'; id: number }
  | null;
