import { useQuery } from '@tanstack/react-query';
import { configurationApi } from '@/features/configuration/api/configurationApi';
import type { EffectiveConfigurationDto } from '@/features/configuration/types';

interface UseConfigValueOptions {
  scope?: string;
  scopeReferenceId?: string;
  enabled?: boolean;
}

/**
 * Hook to resolve the effective configuration value for a given key.
 * Uses the hierarchical resolution service (System → Tenant → Environment → Role → Team → User).
 *
 * @param key - Configuration key (e.g., "catalog.service.creation.approval_required")
 * @param options - Optional scope and reference for targeted resolution
 * @returns TanStack Query result with the effective value
 *
 * @example
 * ```tsx
 * const { data, isLoading } = useConfigValue('catalog.service.creation.approval_required');
 * const isApprovalRequired = data?.effectiveValue === 'true';
 * ```
 */
export function useConfigValue(key: string, options: UseConfigValueOptions = {}) {
  const { scope = 'System', scopeReferenceId, enabled = true } = options;

  return useQuery<EffectiveConfigurationDto | null>({
    queryKey: ['configuration', 'effective-single', key, scope, scopeReferenceId],
    queryFn: async () => {
      const results = await configurationApi.getEffectiveSettings(
        scope,
        scopeReferenceId ?? null,
        key,
      );
      return results[0] ?? null;
    },
    enabled,
    staleTime: 5 * 60 * 1000,
    gcTime: 30 * 60 * 1000,
  });
}

/**
 * Convenience function to check if a boolean config parameter is enabled.
 * Returns false while loading or on error.
 */
export function useConfigEnabled(key: string, options: UseConfigValueOptions = {}) {
  const query = useConfigValue(key, options);
  return {
    ...query,
    isEnabled: query.data?.effectiveValue === 'true',
  };
}
