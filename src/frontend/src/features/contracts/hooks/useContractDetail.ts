import { useQuery } from '@tanstack/react-query';
import { contractsApi } from '../api/contracts';

/** Query keys centralizadas para invalidação consistente. */
export const contractQueryKeys = {
  all: ['contracts'] as const,
  lists: () => [...contractQueryKeys.all, 'list'] as const,
  list: (filters?: Record<string, unknown>) => [...contractQueryKeys.lists(), filters] as const,
  details: () => [...contractQueryKeys.all, 'detail'] as const,
  detail: (id: string) => [...contractQueryKeys.details(), id] as const,
  violations: (id: string) => [...contractQueryKeys.all, 'violations', id] as const,
  history: (apiAssetId: string) => [...contractQueryKeys.all, 'history', apiAssetId] as const,
  summary: () => [...contractQueryKeys.all, 'summary'] as const,
  integrity: (id: string) => [...contractQueryKeys.all, 'integrity', id] as const,
  validationSummary: (id: string) => [...contractQueryKeys.all, 'validation-summary', id] as const,
  spectralRulesets: (filters?: Record<string, unknown>) => [...contractQueryKeys.all, 'spectral-rulesets', filters] as const,
  spectralRuleset: (id: string) => [...contractQueryKeys.all, 'spectral-ruleset', id] as const,
  canonicalEntities: (filters?: Record<string, unknown>) => [...contractQueryKeys.all, 'canonical-entities', filters] as const,
  canonicalEntity: (id: string) => [...contractQueryKeys.all, 'canonical-entity', id] as const,
  canonicalUsages: (id: string) => [...contractQueryKeys.all, 'canonical-usages', id] as const,
};

/**
 * Hook para obter o detalhe de uma versão de contrato.
 */
export function useContractDetail(contractVersionId: string | undefined) {
  return useQuery({
    queryKey: contractQueryKeys.detail(contractVersionId ?? ''),
    queryFn: () => contractsApi.getDetail(contractVersionId!),
    enabled: !!contractVersionId,
  });
}
