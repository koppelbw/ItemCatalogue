import { z } from 'zod';

// Zod schemas mirroring the server-side FluentValidation rules
// (Application/Validation/*RequestValidators.cs). The server stays the source of
// truth — these just give fast, inline feedback. Forms convert empty inputs to
// null before validation, so nullable string fields never see ''.

const nullableString = (max: number) => z.string().max(max).nullable();

// Application/Validation/GeometryRules.cs: "#RRGGBB" or "#RRGGBBAA"
const hexColor = z
  .string()
  .regex(/^#([0-9a-fA-F]{6}|[0-9a-fA-F]{8})$/, 'Use #RRGGBB or #RRGGBBAA')
  .nullable();

const nonNegative = z.number().min(0, 'Must be 0 or more').nullable();
const positive = z.number().gt(0, 'Must be more than 0').nullable();
/** degrees on [0, 360) */
const rotation = z.number().min(0, '0–359').lt(360, '0–359').nullable();

export const itemSchema = z
  .object({
    name: z.string().trim().min(1, 'Name is required').max(255),
    description: nullableString(4000),
    itemTypes: z.array(z.number().int()).min(1, 'Pick at least one type'),
    purchasePrice: z.number().min(0, 'Must be 0 or more').nullable(),
    currentValue: z.number().min(0, 'Must be 0 or more').nullable(),
    brand: nullableString(100),
    model: nullableString(100),
    serialNumber: nullableString(100),
    purchasedFrom: nullableString(150),
    quantity: z.number().int().min(1, 'At least 1'),
    condition: z.number().int().nullable(),
    acquisitionType: z.number().int().nullable(),
    purchaseDate: z.string().nullable(),
    warrantyExpiryDate: z.string().nullable(),
    releaseDate: z.string().nullable(),
    valuationDate: z.string().nullable(),
    acquisitionReference: nullableString(100),
    isStored: z.boolean(),
    roomId: z.number().int().nullable(),
    containerId: z.number().int().nullable(),
    ownerId: z.number().int().nullable(),
  })
  .refine((d) => !(d.roomId != null && d.containerId != null), {
    path: ['placement'],
    message: 'An item cannot be in both a room and a container.',
  })
  .refine((d) => !(d.purchaseDate && d.warrantyExpiryDate) || d.warrantyExpiryDate >= d.purchaseDate, {
    path: ['warrantyExpiryDate'],
    message: 'Warranty expiry cannot precede the purchase date.',
  })
  .refine((d) => !(d.releaseDate && d.purchaseDate) || d.purchaseDate >= d.releaseDate, {
    path: ['purchaseDate'],
    message: 'Purchase date cannot precede the release date.',
  });

export type ItemFormValues = z.infer<typeof itemSchema>;

export const floorSchema = z.object({
  name: z.string().trim().min(1, 'Name is required').max(100),
  locationId: z.number().int().min(1, 'Pick a location'),
  // signed: basement = -1, ground = 0, upstairs = 1, …
  levelIndex: z.number().int(),
  elevationInches: z.number().nullable(),
  ceilingHeightInches: positive,
});
export type FloorFormValues = z.infer<typeof floorSchema>;

export const roomSchema = z.object({
  name: z.string().trim().min(1, 'Name is required').max(100),
  description: nullableString(500),
  floorId: z.number().int().min(1, 'Pick a floor'),
  roomType: z.number().int().nullable(),
  originXInches: nonNegative,
  originYInches: nonNegative,
  widthInches: positive,
  depthInches: positive,
  heightInches: positive,
  rotation,
  wallColor: hexColor,
  floorColor: hexColor,
  ceilingColor: hexColor,
});
export type RoomFormValues = z.infer<typeof roomSchema>;

export const locationSchema = z.object({
  name: z.string().trim().min(1, 'Name is required').max(100),
  description: nullableString(500),
});
export type LocationFormValues = z.infer<typeof locationSchema>;

export const containerSchema = z
  .object({
    name: z.string().trim().min(1, 'Name is required').max(100),
    description: nullableString(500),
    roomId: z.number().int().nullable(),
    parentContainerId: z.number().int().nullable(),
    containerType: z.number().int().nullable(),
    positionXInches: nonNegative,
    positionYInches: nonNegative,
    positionZInches: nonNegative,
    rotation,
    widthInches: positive,
    depthInches: positive,
    heightInches: positive,
    color: hexColor,
  })
  .refine((d) => (d.roomId != null) !== (d.parentContainerId != null), {
    path: ['owner'],
    message: 'Specify exactly one of room or parent container.',
  });
export type ContainerFormValues = z.infer<typeof containerSchema>;

export const doorSchema = z
  .object({
    name: nullableString(100),
    kind: z.number().int(),
    fromRoomId: z.number().int().min(1, 'Pick the room the door belongs to'),
    toRoomId: z.number().int().nullable(),
    wall: z.number().int(),
    offsetInches: z.number().min(0, 'Must be 0 or more'),
    widthInches: z.number().gt(0, 'Must be more than 0'),
    heightInches: z.number().gt(0, 'Must be more than 0'),
    hingeSide: z.number().int().nullable(),
    swing: z.number().int().nullable(),
  })
  .refine((d) => d.toRoomId == null || d.toRoomId !== d.fromRoomId, {
    path: ['toRoomId'],
    message: 'A door cannot connect a room to itself.',
  });
export type DoorFormValues = z.infer<typeof doorSchema>;

export const stairSchema = z
  .object({
    name: nullableString(100),
    fromRoomId: z.number().int().min(1, 'Pick the lower room'),
    toRoomId: z.number().int().nullable(),
    shape: z.number().int(),
    positionXInches: nonNegative,
    positionYInches: nonNegative,
    rotation,
    runInches: positive,
    widthInches: positive,
    riseInches: positive,
    stepCount: z.number().int().gt(0, 'Must be more than 0').nullable(),
  })
  .refine((d) => d.toRoomId == null || d.toRoomId !== d.fromRoomId, {
    path: ['toRoomId'],
    message: 'A stair cannot connect a room to itself.',
  });
export type StairFormValues = z.infer<typeof stairSchema>;

export const personSchema = z.object({
  name: z.string().trim().min(1, 'Name is required').max(100),
});
export type PersonFormValues = z.infer<typeof personSchema>;

export const tagSchema = z.object({
  name: z.string().trim().min(1, 'Name is required').max(100),
  description: nullableString(500),
});
export type TagFormValues = z.infer<typeof tagSchema>;

export const collectionSchema = z.object({
  name: z.string().trim().min(1, 'Name is required').max(100),
  description: nullableString(500),
});
export type CollectionFormValues = z.infer<typeof collectionSchema>;

export const collectionItemSchema = z.object({
  itemId: z.number().int().min(1, 'Pick an item'),
  quantity: z.number().int().min(1, 'At least 1'),
  sortOrder: z.number().int().min(0).nullable(),
  role: nullableString(100),
});
export type CollectionItemFormValues = z.infer<typeof collectionItemSchema>;
