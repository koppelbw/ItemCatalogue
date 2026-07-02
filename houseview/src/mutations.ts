import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ApiError, apiDelete, apiGet, apiPost, apiPut, fetchAll } from './api';
import type {
  CollectionResponse,
  ItemEventResponse,
  ItemTagsResponse,
  TagResponse,
} from './types';

// ---------------------------------------------------------------------------
// Entity list queries that the scene catalogue does not cover
// ---------------------------------------------------------------------------

export function useTags() {
  return useQuery({
    queryKey: ['tags'],
    queryFn: () => fetchAll<TagResponse>('tags', new AbortController().signal),
  });
}

export function useCollections() {
  return useQuery({
    queryKey: ['collections'],
    queryFn: () => fetchAll<CollectionResponse>('collections', new AbortController().signal),
  });
}

export function useItemTags(itemId: number | null) {
  return useQuery({
    queryKey: ['itemTags', itemId],
    queryFn: () => apiGet<ItemTagsResponse>(`items/${itemId}/tags`),
    enabled: itemId != null,
  });
}

export function useItemEvents(itemId: number | null) {
  return useQuery({
    queryKey: ['itemEvents', itemId],
    // events endpoint returns a plain array (newest first), not a paged envelope
    queryFn: () => apiGet<ItemEventResponse[]>(`items/${itemId}/events`),
    enabled: itemId != null,
  });
}

// ---------------------------------------------------------------------------
// Generic CRUD mutations. Most writes touch the scene catalogue, so they all
// invalidate ['catalogue'] by default; pass extra keys for derived lists.
// ---------------------------------------------------------------------------

function useInvalidator(keys: string[][]) {
  const qc = useQueryClient();
  return () => keys.forEach((k) => qc.invalidateQueries({ queryKey: k }));
}

export function useCreate<TReq, TRes>(path: string, keys: string[][] = [['catalogue']]) {
  const invalidate = useInvalidator(keys);
  return useMutation<TRes, ApiError, TReq>({
    mutationFn: (body) => apiPost<TRes>(path, body),
    onSuccess: invalidate,
  });
}

export function useUpdate<TReq extends { id: number }, TRes>(path: string, keys: string[][] = [['catalogue']]) {
  const invalidate = useInvalidator(keys);
  return useMutation<TRes, ApiError, TReq>({
    mutationFn: (body) => apiPut<TRes>(`${path}/${body.id}`, body),
    onSuccess: invalidate,
  });
}

/** Hard/soft delete. Items pass `query` = `?reason=<ordinal>`; others omit it. */
export function useRemove(path: string, keys: string[][] = [['catalogue']]) {
  const invalidate = useInvalidator(keys);
  return useMutation<void, ApiError, { id: number; query?: string }>({
    mutationFn: ({ id, query }) => apiDelete(`${path}/${id}${query ?? ''}`),
    onSuccess: invalidate,
  });
}

// ---------------------------------------------------------------------------
// Relationship mutations: item tags and collection membership
// ---------------------------------------------------------------------------

export function useSetItemTags() {
  const qc = useQueryClient();
  return useMutation<ItemTagsResponse, ApiError, { itemId: number; tagIds: number[] }>({
    mutationFn: ({ itemId, tagIds }) => apiPut<ItemTagsResponse>(`items/${itemId}/tags`, { tagIds }),
    onSuccess: (_res, vars) => {
      qc.invalidateQueries({ queryKey: ['itemTags', vars.itemId] });
    },
  });
}

export function useAddCollectionItem() {
  const qc = useQueryClient();
  return useMutation<
    unknown,
    ApiError,
    { collectionId: number; itemId: number; quantity: number; sortOrder: number | null; role: string | null }
  >({
    mutationFn: ({ collectionId, ...body }) => apiPost(`collections/${collectionId}/items`, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['collections'] }),
  });
}

export function useUpdateCollectionItem() {
  const qc = useQueryClient();
  return useMutation<
    unknown,
    ApiError,
    { collectionId: number; itemId: number; quantity: number; sortOrder: number | null; role: string | null }
  >({
    mutationFn: ({ collectionId, itemId, ...body }) => apiPut(`collections/${collectionId}/items/${itemId}`, body),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['collections'] }),
  });
}

export function useRemoveCollectionItem() {
  const qc = useQueryClient();
  return useMutation<void, ApiError, { collectionId: number; itemId: number }>({
    mutationFn: ({ collectionId, itemId }) => apiDelete(`collections/${collectionId}/items/${itemId}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['collections'] }),
  });
}

// ---------------------------------------------------------------------------
// ProblemDetails → form/banner mapping
// ---------------------------------------------------------------------------

/** lowercases the first character: PascalCase API property → camelCase form field */
function toCamel(key: string): string {
  return key.length > 0 ? key.charAt(0).toLowerCase() + key.slice(1) : key;
}

export interface MappedApiError {
  /** field-level messages keyed by camelCase form field name */
  fields: Record<string, string>;
  /** a human banner message (concurrency/in-use/duplicate/unmapped 400s/etc.) */
  banner: string | null;
}

/**
 * Turns an ApiError (RFC 9457 ProblemDetails) into per-field messages plus a
 * banner. 400 validation errors map onto form fields by camelCased property
 * name; synthetic names (Placement, Owner, date cross-rules) land in `fields`
 * too and are surfaced in the banner so they are never silently dropped.
 */
export function mapApiError(err: unknown): MappedApiError {
  if (err instanceof ApiError) {
    if (err.status === 400 && err.problem?.errors) {
      const fields: Record<string, string> = {};
      const messages: string[] = [];
      for (const [key, msgs] of Object.entries(err.problem.errors)) {
        const joined = msgs.join(' ');
        fields[toCamel(key)] = joined;
        messages.push(joined);
      }
      return { fields, banner: messages.join(' ') || null };
    }
    // 409 concurrency / in-use / duplicate, 404, etc.
    return { fields: {}, banner: err.problem?.detail ?? err.problem?.title ?? err.message };
  }
  return { fields: {}, banner: err instanceof Error ? err.message : 'Something went wrong' };
}
