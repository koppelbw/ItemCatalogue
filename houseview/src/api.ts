import { useQuery } from '@tanstack/react-query';
import { FALLBACK_DATA } from './fallbackData';
import type {
  CatalogueData,
  ContainerResponse,
  DoorResponse,
  ItemResponse,
  LocationResponse,
  PagedResponse,
  PersonResponse,
  RoomResponse,
  StairResponse,
  TagResponse,
} from './types';

const PAGE_SIZE = 100;
const REQUEST_TIMEOUT_MS = 4000;

// ---------------------------------------------------------------------------
// Request core — shared by reads and the CRUD mutations. Surfaces the API's RFC
// 9457 ProblemDetails so callers can map 400/404/409 precisely.
// ---------------------------------------------------------------------------

export interface ProblemDetails {
  type?: string;
  title?: string;
  status?: number;
  detail?: string;
  /** validation failures keyed by PascalCase property name */
  errors?: Record<string, string[]>;
}

export class ApiError extends Error {
  constructor(
    readonly status: number,
    readonly problem: ProblemDetails | null,
    message: string,
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

async function parseProblem(res: Response): Promise<ProblemDetails | null> {
  try {
    const body = await res.json();
    return body as ProblemDetails;
  } catch {
    return null;
  }
}

async function request<T>(method: string, path: string, body?: unknown, signal?: AbortSignal): Promise<T> {
  const res = await fetch(`/api/${path}`, {
    method,
    signal,
    headers: body !== undefined ? { 'Content-Type': 'application/json', Accept: 'application/json' } : { Accept: 'application/json' },
    body: body !== undefined ? JSON.stringify(body) : undefined,
  });
  if (!res.ok) {
    const problem = await parseProblem(res);
    throw new ApiError(res.status, problem, problem?.title ?? `${method} /api/${path} responded ${res.status}`);
  }
  if (res.status === 204) {
    return undefined as T;
  }
  return (await res.json()) as T;
}

export const apiGet = <T>(path: string, signal?: AbortSignal) => request<T>('GET', path, undefined, signal);
export const apiPost = <T>(path: string, body: unknown) => request<T>('POST', path, body);
export const apiPut = <T>(path: string, body: unknown) => request<T>('PUT', path, body);
export const apiDelete = (path: string) => request<void>('DELETE', path);

// ---------------------------------------------------------------------------
// Paginated reads
// ---------------------------------------------------------------------------

async function fetchPage<T>(path: string, page: number, signal: AbortSignal): Promise<PagedResponse<T>> {
  const sep = path.includes('?') ? '&' : '?';
  return apiGet<PagedResponse<T>>(`${path}${sep}page=${page}&pageSize=${PAGE_SIZE}`, signal);
}

/** Walk every page of a paginated list endpoint into a single array. */
export async function fetchAll<T>(path: string, signal: AbortSignal): Promise<T[]> {
  const all: T[] = [];
  let page = 1;
  for (;;) {
    const result = await fetchPage<T>(path, page, signal);
    all.push(...result.items);
    if (!result.hasNext) {
      return all;
    }
    page += 1;
  }
}

/**
 * Loads the full catalogue from the API; falls back to bundled demo data when the
 * API is unreachable so the visualisation always has something to show.
 *
 * Everything comes from the flat list endpoints (rather than the per-location
 * /map tree) because the flat DTOs carry BOTH the geometry the scene needs and
 * the rowVersion tokens the edit forms need — one fetch feeds both worlds.
 * Floors ride embedded in LocationResponse, so they need no extra call.
 */
export async function loadCatalogue(): Promise<CatalogueData> {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);
  try {
    const [locations, rooms, containers, doors, stairs, items, persons, tags] = await Promise.all([
      fetchAll<LocationResponse>('locations', controller.signal),
      fetchAll<RoomResponse>('rooms', controller.signal),
      fetchAll<ContainerResponse>('containers', controller.signal),
      fetchAll<DoorResponse>('doors', controller.signal),
      fetchAll<StairResponse>('stairs', controller.signal),
      // the item list hides soft-deleted rows by default; ask for them so the
      // manage table can show (and the index can exclude) them explicitly
      fetchAll<ItemResponse>('items?includeDeleted=true', controller.signal),
      fetchAll<PersonResponse>('persons', controller.signal),
      fetchAll<TagResponse>('tags', controller.signal),
    ]);
    // Only items tagged "Furniture" are drawn inside rooms. Tag assignments have
    // no bulk endpoint, so resolve the tag by name and use the items?tagId filter.
    const furnitureTag = tags.find((t) => t.name.trim().toLowerCase() === 'furniture');
    const furnitureItems = furnitureTag
      ? await fetchAll<ItemResponse>(`items?tagId=${furnitureTag.id}`, controller.signal)
      : [];
    const floors = locations.flatMap((l) => l.floors);
    return {
      locations,
      floors,
      rooms,
      containers,
      doors,
      stairs,
      items,
      persons,
      furnitureItemIds: furnitureItems.map((i) => i.id),
      live: true,
    };
  } catch {
    return FALLBACK_DATA;
  } finally {
    clearTimeout(timeout);
  }
}

/** React Query hook for the whole catalogue. */
export function useCatalogue() {
  return useQuery({ queryKey: ['catalogue'], queryFn: loadCatalogue });
}
