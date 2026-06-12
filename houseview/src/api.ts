import { FALLBACK_DATA } from './fallbackData';
import type { CatalogueData, ItemResponse, LocationResponse, PagedResponse, PersonResponse, RoomResponse } from './types';

const PAGE_SIZE = 100;
const REQUEST_TIMEOUT_MS = 4000;

async function fetchPage<T>(path: string, page: number, signal: AbortSignal): Promise<PagedResponse<T>> {
  const res = await fetch(`/api/${path}?page=${page}&pageSize=${PAGE_SIZE}`, {
    signal,
    headers: { Accept: 'application/json' },
  });
  if (!res.ok) {
    throw new Error(`GET /api/${path} responded ${res.status}`);
  }
  return (await res.json()) as PagedResponse<T>;
}

async function fetchAll<T>(path: string, signal: AbortSignal): Promise<T[]> {
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
 */
export async function loadCatalogue(): Promise<CatalogueData> {
  const controller = new AbortController();
  const timeout = setTimeout(() => controller.abort(), REQUEST_TIMEOUT_MS);
  try {
    const [rooms, locations, items, persons] = await Promise.all([
      fetchAll<RoomResponse>('rooms', controller.signal),
      fetchAll<LocationResponse>('locations', controller.signal),
      fetchAll<ItemResponse>('items', controller.signal),
      fetchAll<PersonResponse>('persons', controller.signal),
    ]);
    return { rooms, locations, items, persons, live: true };
  } catch {
    return FALLBACK_DATA;
  } finally {
    clearTimeout(timeout);
  }
}
