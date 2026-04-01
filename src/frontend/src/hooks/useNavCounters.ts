import { useQuery } from '@tanstack/react-query';
import { incidentsApi } from '../features/operations/api/incidents';

/**
 * Returns alert counters for sidebar nav items, fetched via React Query.
 * Refreshes every 60 seconds in background; stale data is preferred to loading spinners.
 *
 * Usage:
 *   const { openIncidents } = useNavCounters();
 *
 * @see docs/frontend-audit/frontend-prioritized-improvement-roadmap.md §F4-01
 */
export function useNavCounters() {
  const { data } = useQuery({
    queryKey: ['incidents-summary'],
    queryFn: () => incidentsApi.getIncidentSummary(),
    staleTime: 60_000,
    refetchInterval: 60_000,
    // Don't throw / show error UI — counters are optional enhancement
    retry: false,
    throwOnError: false,
  });

  return {
    /** Number of currently open incidents (Critical + Major + Minor). */
    openIncidents: data?.totalOpen ?? 0,
    /** Number of currently critical severity incidents. */
    criticalIncidents: data?.criticalIncidents ?? 0,
  };
}
