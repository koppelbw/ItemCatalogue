import { z } from 'zod';

// Zod schemas mirroring the server-side FluentValidation rules
// (Application/Validation/*RequestValidators.cs). The server stays the source of
// truth — these just give fast, inline feedback. Forms convert empty inputs to
// null before validation, so nullable string fields never see ''.

const nullableString = (max: number) => z.string().max(max).nullable();

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

export const roomSchema = z.object({
  name: z.string().trim().min(1, 'Name is required').max(100),
  description: nullableString(500),
  locationId: z.number().int().min(1, 'Pick a location'),
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
  })
  .refine((d) => (d.roomId != null) !== (d.parentContainerId != null), {
    path: ['owner'],
    message: 'Specify exactly one of room or parent container.',
  });
export type ContainerFormValues = z.infer<typeof containerSchema>;

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
