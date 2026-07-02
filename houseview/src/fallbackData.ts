import type { CatalogueData, ItemResponse, RoomResponse } from './types';

// Bundled demo data, used when the API is unreachable so the experience still
// works as a standalone showcase. Shapes mirror the new entity model:
// Location → Room (room.locationId) → Container (nestable) → Item, with an item
// living in a Room xor a Container. rowVersion values are placeholder base64.

const RV = 'AAAAAAAAB9E=';
const CREATED = '2026-01-15T10:00:00Z';

function room(id: number, name: string, locationId: number, description: string | null = null): RoomResponse {
  return { id, name, description, locationId, rowVersion: RV };
}

function mkItem(p: Partial<ItemResponse> & Pick<ItemResponse, 'id' | 'name' | 'itemTypes'>): ItemResponse {
  return {
    description: null,
    purchasePrice: null,
    currentValue: null,
    brand: null,
    model: null,
    serialNumber: null,
    purchasedFrom: null,
    quantity: 1,
    condition: null,
    acquisitionType: null,
    purchaseDate: null,
    warrantyExpiryDate: null,
    isStored: false,
    isDeleted: false,
    reasonForDeletion: null,
    roomId: null,
    containerId: null,
    ownerId: null,
    releaseDate: null,
    valuationDate: null,
    acquisitionReference: null,
    createdDate: CREATED,
    lastModifiedDate: null,
    rowVersion: RV,
    ...p,
  };
}

const rooms: RoomResponse[] = [
  // House (1)
  room(1, 'Living Room', 1, 'Main living area'),
  room(2, 'Kitchen', 1),
  room(3, 'Bedroom', 1, 'Main bedroom'),
  room(4, 'Garage', 1),
  room(5, 'Bathroom', 1),
  // Apartment (2)
  room(6, 'Bedroom', 2),
  room(7, 'Kitchen', 2),
  // Grandmas (3)
  room(8, 'Living Room', 3),
  // Storage Unit (4)
  room(9, 'Storage', 4),
  // Car (5)
  room(10, 'Glove box', 5),
  room(11, 'Trunk', 5),
];

export const FALLBACK_DATA: CatalogueData = {
  live: false,
  locations: [
    { id: 1, name: 'House', description: 'My house', rooms: [], rowVersion: RV },
    { id: 2, name: 'Apartment', description: 'My apartment', rooms: [], rowVersion: RV },
    { id: 3, name: 'Grandmas', description: "Grandma's house", rooms: [], rowVersion: RV },
    { id: 4, name: 'Storage Unit', description: '#223', rooms: [], rowVersion: RV },
    { id: 5, name: 'Car', description: 'Subaru Forester', rooms: [], rowVersion: RV },
  ],
  rooms,
  containers: [
    { id: 1, name: 'Toolbox', description: 'Red steel toolbox', roomId: 4, parentContainerId: null, rowVersion: RV },
    { id: 2, name: 'Small Parts Bin', description: 'Drawer organiser', roomId: null, parentContainerId: 1, rowVersion: RV },
    { id: 3, name: 'Storage Box A', description: 'Cardboard archive box', roomId: 9, parentContainerId: null, rowVersion: RV },
  ],
  items: [
    mkItem({
      id: 1,
      name: 'Laptop',
      description: 'High-performance laptop with 16GB RAM',
      itemTypes: [0],
      purchasePrice: 1299.99,
      currentValue: 950,
      brand: 'Dell',
      model: 'XPS 15',
      condition: 1,
      acquisitionType: 0,
      purchaseDate: '2025-03-10T00:00:00Z',
      roomId: 3,
      ownerId: 1,
    }),
    mkItem({
      id: 2,
      name: 'Mechanical Keyboard',
      description: 'RGB mechanical keyboard with Cherry MX switches',
      itemTypes: [0],
      purchasePrice: 149.99,
      currentValue: 110,
      condition: 2,
      roomId: 1,
      ownerId: 1,
    }),
    mkItem({
      id: 3,
      name: 'Cordless Drill',
      itemTypes: [0],
      purchasePrice: 129,
      brand: 'Makita',
      condition: 2,
      isStored: true,
      containerId: 1,
      ownerId: 1,
    }),
    mkItem({
      id: 4,
      name: 'Drill Bit Set',
      description: 'Assorted titanium bits',
      itemTypes: [0],
      purchasePrice: 24.5,
      quantity: 1,
      isStored: true,
      containerId: 2,
      ownerId: 1,
    }),
    mkItem({
      id: 5,
      name: 'Bath Towels',
      itemTypes: [1, 3],
      purchasePrice: 39.99,
      quantity: 4,
      roomId: 5,
    }),
    mkItem({
      id: 6,
      name: 'All-Purpose Cleaner',
      itemTypes: [2],
      purchasePrice: 6.99,
      quantity: 2,
      roomId: 2,
    }),
    mkItem({
      id: 7,
      name: 'Down Duvet',
      itemTypes: [3],
      purchasePrice: 180,
      currentValue: 120,
      condition: 2,
      roomId: 6,
      ownerId: 2,
    }),
    mkItem({
      id: 8,
      name: 'Paperback Novels',
      description: 'Box of assorted paperbacks',
      itemTypes: [4],
      quantity: 22,
      isStored: true,
      containerId: 3,
      ownerId: 3,
    }),
    mkItem({
      id: 9,
      name: 'Tire Inflator',
      itemTypes: [0],
      purchasePrice: 59.95,
      brand: 'Ryobi',
      isStored: true,
      roomId: 11,
      ownerId: 1,
    }),
    mkItem({
      id: 10,
      name: "Owner's Manual",
      itemTypes: [4],
      roomId: 10,
    }),
  ],
  persons: [
    { id: 1, name: 'Bill', rowVersion: RV },
    { id: 2, name: 'Jen', rowVersion: RV },
    { id: 3, name: 'Oscar', rowVersion: RV },
    { id: 4, name: 'Bowie', rowVersion: RV },
  ],
};
