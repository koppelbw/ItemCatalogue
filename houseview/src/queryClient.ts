import { QueryClient } from '@tanstack/react-query';

// One shared client for the whole app. The catalogue is small and changes
// rarely, so we keep data fresh for a minute and avoid noisy refetches; reads
// fall back to bundled demo data inside the query fn, so retrying is pointless.
export const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 60_000,
      refetchOnWindowFocus: false,
      retry: false,
    },
  },
});
