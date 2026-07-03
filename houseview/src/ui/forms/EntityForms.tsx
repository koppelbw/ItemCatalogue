import { zodResolver } from '@hookform/resolvers/zod';
import { useMemo, useState } from 'react';
import { useForm, type UseFormSetError } from 'react-hook-form';
import { mapApiError, useCreate, useRemove, useUpdate } from '../../mutations';
import { ItemExtras } from '../ItemExtras';
import {
  collectionSchema,
  containerSchema,
  doorSchema,
  floorSchema,
  itemSchema,
  locationSchema,
  personSchema,
  roomSchema,
  stairSchema,
  tagSchema,
  type CollectionFormValues,
  type ContainerFormValues,
  type DoorFormValues,
  type FloorFormValues,
  type ItemFormValues,
  type LocationFormValues,
  type PersonFormValues,
  type RoomFormValues,
  type StairFormValues,
  type TagFormValues,
} from '../../schemas';
import {
  ACQUISITION_TYPE_NAMES,
  CONDITION_NAMES,
  CONTAINER_TYPE_NAMES,
  DELETED_REASON_NAMES,
  DOOR_KIND_NAMES,
  HINGE_SIDE_NAMES,
  ITEM_TYPE_NAMES,
  ROOM_TYPE_NAMES,
  STAIR_SHAPE_NAMES,
  SWING_NAMES,
  WALL_NAMES,
  type CollectionResponse,
  type ContainerResponse,
  type CreateCollectionRequest,
  type CreateContainerRequest,
  type CreateDoorRequest,
  type CreateFloorRequest,
  type CreateItemRequest,
  type CreateLocationRequest,
  type CreatePersonRequest,
  type CreateRoomRequest,
  type CreateStairRequest,
  type CreateTagRequest,
  type DoorResponse,
  type FloorResponse,
  type ItemResponse,
  type LocationResponse,
  type PersonResponse,
  type RoomResponse,
  type StairResponse,
  type TagResponse,
  type UpdateCollectionRequest,
  type UpdateContainerRequest,
  type UpdateDoorRequest,
  type UpdateFloorRequest,
  type UpdateItemRequest,
  type UpdateLocationRequest,
  type UpdatePersonRequest,
  type UpdateRoomRequest,
  type UpdateStairRequest,
  type UpdateTagRequest,
} from '../../types';
import { CheckboxField, ChipMultiField, DateField, NumberField, SelectField, TextAreaField, TextField, type Option } from './Fields';
import { Modal } from './Modal';

const enumOptions = (names: readonly string[]): Option[] => names.map((label, value) => ({ value, label }));

// eslint-disable-next-line @typescript-eslint/no-explicit-any
function applyServerError(e: unknown, setError: UseFormSetError<any>, setBanner: (s: string | null) => void) {
  const { fields, banner } = mapApiError(e);
  for (const [k, m] of Object.entries(fields)) {
    setError(k, { type: 'server', message: m });
  }
  setBanner(banner);
}

export interface RefData {
  locations: LocationResponse[];
  floors: FloorResponse[];
  rooms: RoomResponse[];
  containers: ContainerResponse[];
  persons: PersonResponse[];
}

/** dropdown labels that carry enough context to tell same-named entities apart */
function useLookupOptions(lookups: RefData) {
  return useMemo(() => {
    const locationsById = new Map(lookups.locations.map((l) => [l.id, l]));
    const floorsById = new Map(lookups.floors.map((f) => [f.id, f]));
    const floorLabel = (f: FloorResponse) => {
      const loc = locationsById.get(f.locationId);
      return loc ? `${loc.name} › ${f.name}` : f.name;
    };
    const roomLabel = (r: RoomResponse) => {
      const floor = floorsById.get(r.floorId);
      const loc = floor ? locationsById.get(floor.locationId) : undefined;
      return loc ? `${r.name} — ${loc.name}` : r.name;
    };
    return {
      locationOptions: lookups.locations.map((l) => ({ value: l.id, label: l.name })),
      floorOptions: lookups.floors.map((f) => ({ value: f.id, label: floorLabel(f) })),
      roomOptions: lookups.rooms.map((r) => ({ value: r.id, label: roomLabel(r) })),
      containerOptions: lookups.containers.map((c) => ({ value: c.id, label: c.name })),
      ownerOptions: lookups.persons.map((p) => ({ value: p.id, label: p.name })),
    };
  }, [lookups]);
}

interface FooterProps {
  pending: boolean;
  onClose: () => void;
  formId: string;
}

function FormFooter({ pending, onClose, formId }: FooterProps) {
  return (
    <>
      <button className="btn btn-ghost" type="button" onClick={onClose}>
        Cancel
      </button>
      <button className="btn btn-primary" type="submit" form={formId} disabled={pending}>
        {pending ? 'Saving…' : 'Save'}
      </button>
    </>
  );
}

// ---------------------------------------------------------------------------

export function ItemForm({
  initial,
  lookups,
  onClose,
  presetRoomId = null,
  presetContainerId = null,
}: {
  initial?: ItemResponse;
  lookups: RefData;
  onClose: () => void;
  presetRoomId?: number | null;
  presetContainerId?: number | null;
}) {
  const [banner, setBanner] = useState<string | null>(null);
  const create = useCreate<CreateItemRequest, ItemResponse>('items');
  const update = useUpdate<UpdateItemRequest, ItemResponse>('items');
  const {
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<ItemFormValues>({
    resolver: zodResolver(itemSchema),
    defaultValues: initial
      ? {
          name: initial.name,
          description: initial.description,
          itemTypes: initial.itemTypes,
          purchasePrice: initial.purchasePrice,
          currentValue: initial.currentValue,
          brand: initial.brand,
          model: initial.model,
          serialNumber: initial.serialNumber,
          purchasedFrom: initial.purchasedFrom,
          quantity: initial.quantity,
          condition: initial.condition,
          acquisitionType: initial.acquisitionType,
          purchaseDate: initial.purchaseDate,
          warrantyExpiryDate: initial.warrantyExpiryDate,
          releaseDate: initial.releaseDate,
          valuationDate: initial.valuationDate,
          acquisitionReference: initial.acquisitionReference,
          isStored: initial.isStored,
          isShownInUI: initial.isShownInUI ?? false,
          roomId: initial.roomId,
          containerId: initial.containerId,
          ownerId: initial.ownerId,
        }
      : {
          name: '',
          description: null,
          itemTypes: [],
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
          releaseDate: null,
          valuationDate: null,
          acquisitionReference: null,
          isStored: false,
          isShownInUI: false,
          roomId: presetRoomId,
          containerId: presetContainerId,
          ownerId: null,
        },
  });

  const { roomOptions, containerOptions, ownerOptions } = useLookupOptions(lookups);

  const onSubmit = async (v: ItemFormValues) => {
    try {
      if (initial) await update.mutateAsync({ ...v, id: initial.id, rowVersion: initial.rowVersion });
      else await create.mutateAsync({ ...v });
      onClose();
    } catch (e) {
      applyServerError(e, setError, setBanner);
    }
  };

  const placementError = (errors as Record<string, { message?: string }>).placement?.message;

  return (
    <Modal title={initial ? `Edit item #${initial.id}` : 'New item'} onClose={onClose} banner={banner} footer={<FormFooter pending={create.isPending || update.isPending} onClose={onClose} formId="itemForm" />}>
      <form id="itemForm" onSubmit={handleSubmit(onSubmit)} className="entity-form">
        <TextField control={control} errors={errors} name="name" label="Name" />
        <TextAreaField control={control} errors={errors} name="description" label="Description" nullable />
        <ChipMultiField control={control} errors={errors} name="itemTypes" label="Types" options={enumOptions(ITEM_TYPE_NAMES)} />
        <div className="form-row">
          <NumberField control={control} errors={errors} name="purchasePrice" label="Purchase price" />
          <NumberField control={control} errors={errors} name="currentValue" label="Current value" />
        </div>
        <div className="form-row">
          <NumberField control={control} errors={errors} name="quantity" label="Quantity" integer />
          <SelectField control={control} errors={errors} name="condition" label="Condition" options={enumOptions(CONDITION_NAMES)} />
        </div>
        <div className="form-row">
          <SelectField control={control} errors={errors} name="acquisitionType" label="Acquisition" options={enumOptions(ACQUISITION_TYPE_NAMES)} />
          <SelectField control={control} errors={errors} name="ownerId" label="Owner" options={ownerOptions} />
        </div>
        <div className="form-row">
          <SelectField control={control} errors={errors} name="roomId" label="Room" options={roomOptions} placeholder="(none)" />
          <SelectField control={control} errors={errors} name="containerId" label="Container" options={containerOptions} placeholder="(none)" />
        </div>
        {placementError && <em className="form-error">{placementError}</em>}
        <div className="form-row">
          <TextField control={control} errors={errors} name="brand" label="Brand" nullable />
          <TextField control={control} errors={errors} name="model" label="Model" nullable />
        </div>
        <div className="form-row">
          <TextField control={control} errors={errors} name="serialNumber" label="Serial number" nullable />
          <TextField control={control} errors={errors} name="purchasedFrom" label="Purchased from" nullable />
        </div>
        <div className="form-row">
          <DateField control={control} errors={errors} name="purchaseDate" label="Purchase date" />
          <DateField control={control} errors={errors} name="warrantyExpiryDate" label="Warranty expiry" />
        </div>
        <div className="form-row">
          <DateField control={control} errors={errors} name="releaseDate" label="Release date" />
          <DateField control={control} errors={errors} name="valuationDate" label="Valuation date" />
        </div>
        <TextField control={control} errors={errors} name="acquisitionReference" label="Acquisition reference" nullable />
        <div className="form-row">
          <CheckboxField control={control} name="isStored" label="In storage" />
          <CheckboxField control={control} name="isShownInUI" label="Show in 3D view" />
        </div>
      </form>
      {initial && <ItemExtras itemId={initial.id} />}
    </Modal>
  );
}

// ---------------------------------------------------------------------------

export function FloorForm({
  initial,
  lookups,
  onClose,
  presetLocationId = null,
}: {
  initial?: FloorResponse;
  lookups: RefData;
  onClose: () => void;
  presetLocationId?: number | null;
}) {
  const [banner, setBanner] = useState<string | null>(null);
  const create = useCreate<CreateFloorRequest, FloorResponse>('floors');
  const update = useUpdate<UpdateFloorRequest, FloorResponse>('floors');
  const {
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<FloorFormValues>({
    resolver: zodResolver(floorSchema),
    defaultValues: initial
      ? {
          name: initial.name,
          locationId: initial.locationId,
          levelIndex: initial.levelIndex,
          elevationInches: initial.elevationInches,
          ceilingHeightInches: initial.ceilingHeightInches,
        }
      : { name: '', locationId: presetLocationId ?? 0, levelIndex: 0, elevationInches: null, ceilingHeightInches: null },
  });
  const { locationOptions } = useLookupOptions(lookups);

  const onSubmit = async (v: FloorFormValues) => {
    try {
      if (initial) await update.mutateAsync({ ...v, id: initial.id, rowVersion: initial.rowVersion });
      else await create.mutateAsync({ ...v });
      onClose();
    } catch (e) {
      applyServerError(e, setError, setBanner);
    }
  };

  return (
    <Modal title={initial ? `Edit floor #${initial.id}` : 'New floor'} onClose={onClose} banner={banner} footer={<FormFooter pending={create.isPending || update.isPending} onClose={onClose} formId="floorForm" />}>
      <form id="floorForm" onSubmit={handleSubmit(onSubmit)} className="entity-form">
        <TextField control={control} errors={errors} name="name" label="Name" />
        <SelectField control={control} errors={errors} name="locationId" label="Location" options={locationOptions} placeholder="Pick a location" required />
        <p className="form-hint">Level index orders the stories: basement −1, ground 0, upstairs 1…</p>
        <div className="form-row">
          <NumberField control={control} errors={errors} name="levelIndex" label="Level index" integer />
          <NumberField control={control} errors={errors} name="ceilingHeightInches" label="Ceiling height (in)" />
        </div>
        <NumberField control={control} errors={errors} name="elevationInches" label="Elevation (in, from grade)" />
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------

export function RoomForm({
  initial,
  lookups,
  onClose,
  presetFloorId = null,
}: {
  initial?: RoomResponse;
  lookups: RefData;
  onClose: () => void;
  presetFloorId?: number | null;
}) {
  const [banner, setBanner] = useState<string | null>(null);
  const create = useCreate<CreateRoomRequest, RoomResponse>('rooms');
  const update = useUpdate<UpdateRoomRequest, RoomResponse>('rooms');
  const {
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<RoomFormValues>({
    resolver: zodResolver(roomSchema),
    defaultValues: initial
      ? {
          name: initial.name,
          description: initial.description,
          floorId: initial.floorId,
          roomType: initial.roomType,
          originXInches: initial.originXInches,
          originYInches: initial.originYInches,
          widthInches: initial.widthInches,
          depthInches: initial.depthInches,
          heightInches: initial.heightInches,
          rotation: initial.rotation,
          wallColor: initial.wallColor,
          floorColor: initial.floorColor,
          ceilingColor: initial.ceilingColor,
        }
      : {
          name: '',
          description: null,
          floorId: presetFloorId ?? 0,
          roomType: null,
          originXInches: null,
          originYInches: null,
          widthInches: null,
          depthInches: null,
          heightInches: null,
          rotation: null,
          wallColor: null,
          floorColor: null,
          ceilingColor: null,
        },
  });
  const { floorOptions } = useLookupOptions(lookups);

  const onSubmit = async (v: RoomFormValues) => {
    try {
      if (initial) await update.mutateAsync({ ...v, id: initial.id, rowVersion: initial.rowVersion });
      else await create.mutateAsync({ ...v });
      onClose();
    } catch (e) {
      applyServerError(e, setError, setBanner);
    }
  };

  return (
    <Modal title={initial ? `Edit room #${initial.id}` : 'New room'} onClose={onClose} banner={banner} footer={<FormFooter pending={create.isPending || update.isPending} onClose={onClose} formId="roomForm" />}>
      <form id="roomForm" onSubmit={handleSubmit(onSubmit)} className="entity-form">
        <TextField control={control} errors={errors} name="name" label="Name" />
        <TextAreaField control={control} errors={errors} name="description" label="Description" nullable />
        <div className="form-row">
          <SelectField control={control} errors={errors} name="floorId" label="Floor" options={floorOptions} placeholder="Pick a floor" required />
          <SelectField control={control} errors={errors} name="roomType" label="Room type" options={enumOptions(ROOM_TYPE_NAMES)} />
        </div>
        <p className="form-hint">Geometry is optional (inches, plan view). The 3D house lays rooms out from it.</p>
        <div className="form-row">
          <NumberField control={control} errors={errors} name="originXInches" label="Origin X (in)" />
          <NumberField control={control} errors={errors} name="originYInches" label="Origin Y (in)" />
        </div>
        <div className="form-row">
          <NumberField control={control} errors={errors} name="widthInches" label="Width (in)" />
          <NumberField control={control} errors={errors} name="depthInches" label="Depth (in)" />
        </div>
        <div className="form-row">
          <NumberField control={control} errors={errors} name="heightInches" label="Height (in)" />
          <NumberField control={control} errors={errors} name="rotation" label="Rotation (°)" />
        </div>
        <div className="form-row">
          <TextField control={control} errors={errors} name="wallColor" label="Wall color" nullable placeholder="#RRGGBB" />
          <TextField control={control} errors={errors} name="floorColor" label="Floor color" nullable placeholder="#RRGGBB" />
        </div>
        <TextField control={control} errors={errors} name="ceilingColor" label="Ceiling color" nullable placeholder="#RRGGBB" />
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------

export function LocationForm({ initial, onClose }: { initial?: LocationResponse; onClose: () => void }) {
  const [banner, setBanner] = useState<string | null>(null);
  const create = useCreate<CreateLocationRequest, LocationResponse>('locations');
  const update = useUpdate<UpdateLocationRequest, LocationResponse>('locations');
  const {
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<LocationFormValues>({
    resolver: zodResolver(locationSchema),
    defaultValues: initial ? { name: initial.name, description: initial.description } : { name: '', description: null },
  });

  const onSubmit = async (v: LocationFormValues) => {
    try {
      if (initial) await update.mutateAsync({ ...v, id: initial.id, rowVersion: initial.rowVersion });
      else await create.mutateAsync({ ...v });
      onClose();
    } catch (e) {
      applyServerError(e, setError, setBanner);
    }
  };

  return (
    <Modal title={initial ? `Edit location #${initial.id}` : 'New location'} onClose={onClose} banner={banner} footer={<FormFooter pending={create.isPending || update.isPending} onClose={onClose} formId="locationForm" />}>
      <form id="locationForm" onSubmit={handleSubmit(onSubmit)} className="entity-form">
        <TextField control={control} errors={errors} name="name" label="Name" />
        <TextAreaField control={control} errors={errors} name="description" label="Description" nullable />
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------

export function ContainerForm({
  initial,
  lookups,
  onClose,
  presetRoomId = null,
  presetParentContainerId = null,
}: {
  initial?: ContainerResponse;
  lookups: RefData;
  onClose: () => void;
  presetRoomId?: number | null;
  presetParentContainerId?: number | null;
}) {
  const [banner, setBanner] = useState<string | null>(null);
  const create = useCreate<CreateContainerRequest, ContainerResponse>('containers');
  const update = useUpdate<UpdateContainerRequest, ContainerResponse>('containers');
  const {
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<ContainerFormValues>({
    resolver: zodResolver(containerSchema),
    defaultValues: initial
      ? {
          name: initial.name,
          description: initial.description,
          roomId: initial.roomId,
          parentContainerId: initial.parentContainerId,
          containerType: initial.containerType,
          positionXInches: initial.positionXInches,
          positionYInches: initial.positionYInches,
          positionZInches: initial.positionZInches,
          rotation: initial.rotation,
          widthInches: initial.widthInches,
          depthInches: initial.depthInches,
          heightInches: initial.heightInches,
          color: initial.color,
          isShownInUI: initial.isShownInUI ?? true,
        }
      : {
          name: '',
          description: null,
          roomId: presetRoomId,
          parentContainerId: presetParentContainerId,
          containerType: null,
          positionXInches: null,
          positionYInches: null,
          positionZInches: null,
          rotation: null,
          widthInches: null,
          depthInches: null,
          heightInches: null,
          color: null,
          isShownInUI: true,
        },
  });
  const { roomOptions } = useLookupOptions(lookups);
  // a container cannot be its own parent
  const parentOptions: Option[] = lookups.containers.filter((c) => c.id !== initial?.id).map((c) => ({ value: c.id, label: c.name }));
  const ownerError = (errors as Record<string, { message?: string }>).owner?.message;

  const onSubmit = async (v: ContainerFormValues) => {
    try {
      if (initial) await update.mutateAsync({ ...v, id: initial.id, rowVersion: initial.rowVersion });
      else await create.mutateAsync({ ...v });
      onClose();
    } catch (e) {
      applyServerError(e, setError, setBanner);
    }
  };

  return (
    <Modal title={initial ? `Edit container #${initial.id}` : 'New container'} onClose={onClose} banner={banner} footer={<FormFooter pending={create.isPending || update.isPending} onClose={onClose} formId="containerForm" />}>
      <form id="containerForm" onSubmit={handleSubmit(onSubmit)} className="entity-form">
        <TextField control={control} errors={errors} name="name" label="Name" />
        <TextAreaField control={control} errors={errors} name="description" label="Description" nullable />
        <SelectField control={control} errors={errors} name="containerType" label="Type" options={enumOptions(CONTAINER_TYPE_NAMES)} />
        <p className="form-hint">A container sits in exactly one of: a room (top-level) or another container (nested).</p>
        <div className="form-row">
          <SelectField control={control} errors={errors} name="roomId" label="Room" options={roomOptions} placeholder="(none)" />
          <SelectField control={control} errors={errors} name="parentContainerId" label="Parent container" options={parentOptions} placeholder="(none)" />
        </div>
        {ownerError && <em className="form-error">{ownerError}</em>}
        <p className="form-hint">Placement is optional (inches: X/Y across the room plan, Z up).</p>
        <div className="form-row">
          <NumberField control={control} errors={errors} name="positionXInches" label="Position X (in)" />
          <NumberField control={control} errors={errors} name="positionYInches" label="Position Y (in)" />
        </div>
        <div className="form-row">
          <NumberField control={control} errors={errors} name="positionZInches" label="Position Z (in)" />
          <NumberField control={control} errors={errors} name="rotation" label="Rotation (°)" />
        </div>
        <div className="form-row">
          <NumberField control={control} errors={errors} name="widthInches" label="Width (in)" />
          <NumberField control={control} errors={errors} name="depthInches" label="Depth (in)" />
        </div>
        <div className="form-row">
          <NumberField control={control} errors={errors} name="heightInches" label="Height (in)" />
          <TextField control={control} errors={errors} name="color" label="Color" nullable placeholder="#RRGGBB" />
        </div>
        <CheckboxField control={control} name="isShownInUI" label="Show in 3D view" />
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------

export function DoorForm({
  initial,
  lookups,
  onClose,
  presetFromRoomId = null,
}: {
  initial?: DoorResponse;
  lookups: RefData;
  onClose: () => void;
  presetFromRoomId?: number | null;
}) {
  const [banner, setBanner] = useState<string | null>(null);
  const create = useCreate<CreateDoorRequest, DoorResponse>('doors');
  const update = useUpdate<UpdateDoorRequest, DoorResponse>('doors');
  const {
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<DoorFormValues>({
    resolver: zodResolver(doorSchema),
    defaultValues: initial
      ? {
          name: initial.name,
          kind: initial.kind,
          fromRoomId: initial.fromRoomId,
          toRoomId: initial.toRoomId,
          wall: initial.wall,
          offsetInches: initial.offsetInches,
          widthInches: initial.widthInches,
          heightInches: initial.heightInches,
          hingeSide: initial.hingeSide,
          swing: initial.swing,
        }
      : {
          name: null,
          kind: 0,
          fromRoomId: presetFromRoomId ?? 0,
          toRoomId: null,
          wall: 0,
          offsetInches: 0,
          widthInches: 32,
          heightInches: 80,
          hingeSide: null,
          swing: null,
        },
  });
  const { roomOptions } = useLookupOptions(lookups);

  const onSubmit = async (v: DoorFormValues) => {
    try {
      if (initial) await update.mutateAsync({ ...v, id: initial.id, rowVersion: initial.rowVersion });
      else await create.mutateAsync({ ...v });
      onClose();
    } catch (e) {
      applyServerError(e, setError, setBanner);
    }
  };

  return (
    <Modal title={initial ? `Edit door #${initial.id}` : 'New door'} onClose={onClose} banner={banner} footer={<FormFooter pending={create.isPending || update.isPending} onClose={onClose} formId="doorForm" />}>
      <form id="doorForm" onSubmit={handleSubmit(onSubmit)} className="entity-form">
        <TextField control={control} errors={errors} name="name" label="Name" nullable />
        <div className="form-row">
          <SelectField control={control} errors={errors} name="kind" label="Kind" options={enumOptions(DOOR_KIND_NAMES)} required />
          <SelectField control={control} errors={errors} name="wall" label="Wall" options={enumOptions(WALL_NAMES)} required />
        </div>
        <p className="form-hint">From room owns the door; leave “to room” empty when it leads outside.</p>
        <div className="form-row">
          <SelectField control={control} errors={errors} name="fromRoomId" label="From room" options={roomOptions} placeholder="Pick a room" required />
          <SelectField control={control} errors={errors} name="toRoomId" label="To room" options={roomOptions} placeholder="(outside)" />
        </div>
        <div className="form-row">
          <NumberField control={control} errors={errors} name="offsetInches" label="Offset along wall (in)" />
          <NumberField control={control} errors={errors} name="widthInches" label="Width (in)" />
        </div>
        <div className="form-row">
          <NumberField control={control} errors={errors} name="heightInches" label="Height (in)" />
          <SelectField control={control} errors={errors} name="hingeSide" label="Hinge side" options={enumOptions(HINGE_SIDE_NAMES)} />
        </div>
        <SelectField control={control} errors={errors} name="swing" label="Swing" options={enumOptions(SWING_NAMES)} />
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------

export function StairForm({
  initial,
  lookups,
  onClose,
  presetFromRoomId = null,
}: {
  initial?: StairResponse;
  lookups: RefData;
  onClose: () => void;
  presetFromRoomId?: number | null;
}) {
  const [banner, setBanner] = useState<string | null>(null);
  const create = useCreate<CreateStairRequest, StairResponse>('stairs');
  const update = useUpdate<UpdateStairRequest, StairResponse>('stairs');
  const {
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<StairFormValues>({
    resolver: zodResolver(stairSchema),
    defaultValues: initial
      ? {
          name: initial.name,
          fromRoomId: initial.fromRoomId,
          toRoomId: initial.toRoomId,
          shape: initial.shape,
          positionXInches: initial.positionXInches,
          positionYInches: initial.positionYInches,
          rotation: initial.rotation,
          runInches: initial.runInches,
          widthInches: initial.widthInches,
          riseInches: initial.riseInches,
          stepCount: initial.stepCount,
        }
      : {
          name: null,
          fromRoomId: presetFromRoomId ?? 0,
          toRoomId: null,
          shape: 0,
          positionXInches: null,
          positionYInches: null,
          rotation: null,
          runInches: null,
          widthInches: null,
          riseInches: null,
          stepCount: null,
        },
  });
  const { roomOptions } = useLookupOptions(lookups);

  const onSubmit = async (v: StairFormValues) => {
    try {
      if (initial) await update.mutateAsync({ ...v, id: initial.id, rowVersion: initial.rowVersion });
      else await create.mutateAsync({ ...v });
      onClose();
    } catch (e) {
      applyServerError(e, setError, setBanner);
    }
  };

  return (
    <Modal title={initial ? `Edit stair #${initial.id}` : 'New stair'} onClose={onClose} banner={banner} footer={<FormFooter pending={create.isPending || update.isPending} onClose={onClose} formId="stairForm" />}>
      <form id="stairForm" onSubmit={handleSubmit(onSubmit)} className="entity-form">
        <TextField control={control} errors={errors} name="name" label="Name" nullable />
        <SelectField control={control} errors={errors} name="shape" label="Shape" options={enumOptions(STAIR_SHAPE_NAMES)} required />
        <p className="form-hint">From room is the lower story; leave “to room” empty for an exterior level.</p>
        <div className="form-row">
          <SelectField control={control} errors={errors} name="fromRoomId" label="From room (lower)" options={roomOptions} placeholder="Pick a room" required />
          <SelectField control={control} errors={errors} name="toRoomId" label="To room (upper)" options={roomOptions} placeholder="(outside)" />
        </div>
        <div className="form-row">
          <NumberField control={control} errors={errors} name="positionXInches" label="Position X (in)" />
          <NumberField control={control} errors={errors} name="positionYInches" label="Position Y (in)" />
        </div>
        <div className="form-row">
          <NumberField control={control} errors={errors} name="runInches" label="Run (in)" />
          <NumberField control={control} errors={errors} name="widthInches" label="Width (in)" />
        </div>
        <div className="form-row">
          <NumberField control={control} errors={errors} name="riseInches" label="Rise (in)" />
          <NumberField control={control} errors={errors} name="stepCount" label="Steps" integer />
        </div>
        <NumberField control={control} errors={errors} name="rotation" label="Rotation (°)" />
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------

export function PersonForm({ initial, onClose }: { initial?: PersonResponse; onClose: () => void }) {
  const [banner, setBanner] = useState<string | null>(null);
  const create = useCreate<CreatePersonRequest, PersonResponse>('persons');
  const update = useUpdate<UpdatePersonRequest, PersonResponse>('persons');
  const {
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<PersonFormValues>({
    resolver: zodResolver(personSchema),
    defaultValues: initial ? { name: initial.name } : { name: '' },
  });

  const onSubmit = async (v: PersonFormValues) => {
    try {
      if (initial) await update.mutateAsync({ ...v, id: initial.id, rowVersion: initial.rowVersion });
      else await create.mutateAsync({ ...v });
      onClose();
    } catch (e) {
      applyServerError(e, setError, setBanner);
    }
  };

  return (
    <Modal title={initial ? `Edit person #${initial.id}` : 'New person'} onClose={onClose} banner={banner} footer={<FormFooter pending={create.isPending || update.isPending} onClose={onClose} formId="personForm" />}>
      <form id="personForm" onSubmit={handleSubmit(onSubmit)} className="entity-form">
        <TextField control={control} errors={errors} name="name" label="Name" />
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------

export function TagForm({ initial, onClose }: { initial?: TagResponse; onClose: () => void }) {
  const [banner, setBanner] = useState<string | null>(null);
  // renaming a tag to/from "Furniture" changes which items the scene renders
  const create = useCreate<CreateTagRequest, TagResponse>('tags', [['tags'], ['catalogue']]);
  const update = useUpdate<UpdateTagRequest, TagResponse>('tags', [['tags'], ['catalogue']]);
  const {
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<TagFormValues>({
    resolver: zodResolver(tagSchema),
    defaultValues: initial ? { name: initial.name, description: initial.description } : { name: '', description: null },
  });

  const onSubmit = async (v: TagFormValues) => {
    try {
      if (initial) await update.mutateAsync({ ...v, id: initial.id, rowVersion: initial.rowVersion });
      else await create.mutateAsync({ ...v });
      onClose();
    } catch (e) {
      applyServerError(e, setError, setBanner);
    }
  };

  return (
    <Modal title={initial ? `Edit tag #${initial.id}` : 'New tag'} onClose={onClose} banner={banner} footer={<FormFooter pending={create.isPending || update.isPending} onClose={onClose} formId="tagForm" />}>
      <form id="tagForm" onSubmit={handleSubmit(onSubmit)} className="entity-form">
        <TextField control={control} errors={errors} name="name" label="Name" />
        <TextAreaField control={control} errors={errors} name="description" label="Description" nullable />
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------

export function CollectionForm({ initial, onClose }: { initial?: CollectionResponse; onClose: () => void }) {
  const [banner, setBanner] = useState<string | null>(null);
  const create = useCreate<CreateCollectionRequest, CollectionResponse>('collections', [['collections']]);
  const update = useUpdate<UpdateCollectionRequest, CollectionResponse>('collections', [['collections']]);
  const {
    control,
    handleSubmit,
    setError,
    formState: { errors },
  } = useForm<CollectionFormValues>({
    resolver: zodResolver(collectionSchema),
    defaultValues: initial ? { name: initial.name, description: initial.description } : { name: '', description: null },
  });

  const onSubmit = async (v: CollectionFormValues) => {
    try {
      if (initial) await update.mutateAsync({ ...v, id: initial.id, rowVersion: initial.rowVersion });
      else await create.mutateAsync({ ...v });
      onClose();
    } catch (e) {
      applyServerError(e, setError, setBanner);
    }
  };

  return (
    <Modal title={initial ? `Edit collection #${initial.id}` : 'New collection'} onClose={onClose} banner={banner} footer={<FormFooter pending={create.isPending || update.isPending} onClose={onClose} formId="collectionForm" />}>
      <form id="collectionForm" onSubmit={handleSubmit(onSubmit)} className="entity-form">
        <TextField control={control} errors={errors} name="name" label="Name" />
        <TextAreaField control={control} errors={errors} name="description" label="Description" nullable />
      </form>
    </Modal>
  );
}

// ---------------------------------------------------------------------------

// Item delete is a soft delete that requires a DeletedReason. Self-contained so
// both the manager table and the 3D detail panel can reuse it.
export function DeleteItemDialog({ item, onClose }: { item: ItemResponse; onClose: () => void }) {
  const remove = useRemove('items');
  const [reason, setReason] = useState(0);
  const [banner, setBanner] = useState<string | null>(null);

  const submit = async () => {
    setBanner(null);
    try {
      await remove.mutateAsync({ id: item.id, query: `?reason=${reason}` });
      onClose();
    } catch (e) {
      setBanner(mapApiError(e).banner);
    }
  };

  return (
    <Modal
      title={`Delete “${item.name}”`}
      onClose={onClose}
      banner={banner}
      footer={
        <>
          <button className="btn btn-ghost" type="button" onClick={onClose}>
            Cancel
          </button>
          <button className="btn btn-danger" type="button" onClick={submit} disabled={remove.isPending}>
            {remove.isPending ? 'Deleting…' : 'Delete'}
          </button>
        </>
      }
    >
      <p>Items are soft-deleted with a reason, so their history is kept.</p>
      <label className="form-field">
        <span>Reason</span>
        <select value={reason} onChange={(e) => setReason(Number(e.target.value))}>
          {DELETED_REASON_NAMES.map((label, value) => (
            <option key={value} value={value}>
              {label}
            </option>
          ))}
        </select>
      </label>
    </Modal>
  );
}
