import { zodResolver } from '@hookform/resolvers/zod';
import { useState } from 'react';
import { useForm, type UseFormSetError } from 'react-hook-form';
import { mapApiError, useCreate, useRemove, useUpdate } from '../../mutations';
import { ItemExtras } from '../ItemExtras';
import {
  collectionSchema,
  containerSchema,
  itemSchema,
  locationSchema,
  personSchema,
  roomSchema,
  tagSchema,
  type CollectionFormValues,
  type ContainerFormValues,
  type ItemFormValues,
  type LocationFormValues,
  type PersonFormValues,
  type RoomFormValues,
  type TagFormValues,
} from '../../schemas';
import {
  ACQUISITION_TYPE_NAMES,
  CONDITION_NAMES,
  DELETED_REASON_NAMES,
  ITEM_TYPE_NAMES,
  type CollectionResponse,
  type ContainerResponse,
  type CreateCollectionRequest,
  type CreateContainerRequest,
  type CreateItemRequest,
  type CreateLocationRequest,
  type CreatePersonRequest,
  type CreateRoomRequest,
  type CreateTagRequest,
  type ItemResponse,
  type LocationResponse,
  type PersonResponse,
  type RoomResponse,
  type TagResponse,
  type UpdateCollectionRequest,
  type UpdateContainerRequest,
  type UpdateItemRequest,
  type UpdateLocationRequest,
  type UpdatePersonRequest,
  type UpdateRoomRequest,
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
  rooms: RoomResponse[];
  containers: ContainerResponse[];
  persons: PersonResponse[];
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
          roomId: presetRoomId,
          containerId: presetContainerId,
          ownerId: null,
        },
  });

  const roomOptions: Option[] = lookups.rooms.map((r) => ({ value: r.id, label: r.name }));
  const containerOptions: Option[] = lookups.containers.map((c) => ({ value: c.id, label: c.name }));
  const ownerOptions: Option[] = lookups.persons.map((p) => ({ value: p.id, label: p.name }));

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
        <CheckboxField control={control} name="isStored" label="In storage" />
      </form>
      {initial && <ItemExtras itemId={initial.id} />}
    </Modal>
  );
}

// ---------------------------------------------------------------------------

export function RoomForm({ initial, lookups, onClose }: { initial?: RoomResponse; lookups: RefData; onClose: () => void }) {
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
      ? { name: initial.name, description: initial.description, locationId: initial.locationId }
      : { name: '', description: null, locationId: 0 },
  });
  const locationOptions: Option[] = lookups.locations.map((l) => ({ value: l.id, label: l.name }));

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
        <SelectField control={control} errors={errors} name="locationId" label="Location" options={locationOptions} placeholder="Pick a location" required />
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

export function ContainerForm({ initial, lookups, onClose }: { initial?: ContainerResponse; lookups: RefData; onClose: () => void }) {
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
      ? { name: initial.name, description: initial.description, roomId: initial.roomId, parentContainerId: initial.parentContainerId }
      : { name: '', description: null, roomId: null, parentContainerId: null },
  });
  const roomOptions: Option[] = lookups.rooms.map((r) => ({ value: r.id, label: r.name }));
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
        <p className="form-hint">A container sits in exactly one of: a room (top-level) or another container (nested).</p>
        <div className="form-row">
          <SelectField control={control} errors={errors} name="roomId" label="Room" options={roomOptions} placeholder="(none)" />
          <SelectField control={control} errors={errors} name="parentContainerId" label="Parent container" options={parentOptions} placeholder="(none)" />
        </div>
        {ownerError && <em className="form-error">{ownerError}</em>}
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
  const create = useCreate<CreateTagRequest, TagResponse>('tags', [['tags']]);
  const update = useUpdate<UpdateTagRequest, TagResponse>('tags', [['tags']]);
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
