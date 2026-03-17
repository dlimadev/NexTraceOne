import { useQuery } from '@tanstack/react-query';
import { contractsApi } from '../api/contracts';
import { contractQueryKeys } from './useContractDetail';

/**
 * Hook para obter violações de regras de um contrato.
 */
export function useContractViolations(contractVersionId: string | undefined) {
  return useQuery({
    queryKey: contractQueryKeys.violations(contractVersionId ?? ''),
    queryFn: () => contractsApi.listRuleViolations(contractVersionId!),
    enabled: !!contractVersionId,
  });
}
