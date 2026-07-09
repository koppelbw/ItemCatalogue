import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ApiError, apiGet, apiUpload } from './api';
import { IMPORT_STATUS_COMPLETED, type ImportJobResponse, type PagedResponse } from './types';

// How often a still-running job is re-fetched. Chunks land in bursts (25 rows per
// queue message), so a couple of seconds keeps the progress bar honest without spam.
const POLL_MS = 2000;

/**
 * POST imports — multipart upload of a CSV. The response is the freshly created
 * job (202 Accepted), which is seeded into the job cache so the progress panel
 * renders instantly instead of waiting for the first poll.
 */
export function useStartImport() {
  const qc = useQueryClient();
  return useMutation<ImportJobResponse, ApiError, File>({
    mutationFn: (file) => {
      const form = new FormData();
      // field name must be "file" — it binds to ImportController.Start(IFormFile? file)
      form.append('file', file, file.name);
      return apiUpload<ImportJobResponse>('imports', form);
    },
    onSuccess: (job) => {
      qc.setQueryData(['importJob', job.id], job);
      void qc.invalidateQueries({ queryKey: ['importJobs'] });
    },
  });
}

/**
 * One job, polled while its chunks are still being processed in the background;
 * polling stops on Completed (status is derived server-side from chunk markers,
 * so Completed is terminal). Pass null to pause (no selection / demo mode).
 */
export function useImportJob(id: number | null) {
  return useQuery<ImportJobResponse, ApiError>({
    queryKey: ['importJob', id],
    queryFn: () => apiGet<ImportJobResponse>(`imports/${id}`),
    enabled: id != null,
    refetchInterval: (query) =>
      query.state.data && query.state.data.status !== IMPORT_STATUS_COMPLETED ? POLL_MS : false,
  });
}

/** Recent jobs, newest first (server-paged, unlike the client-paged entity tables). */
export function useImportJobs(page: number, enabled: boolean) {
  return useQuery<PagedResponse<ImportJobResponse>, ApiError>({
    queryKey: ['importJobs', page],
    queryFn: () => apiGet<PagedResponse<ImportJobResponse>>(`imports?page=${page}&pageSize=10`),
    enabled,
    // keep the previous page on screen while the next one loads (no table flicker)
    placeholderData: (previous) => previous,
  });
}
