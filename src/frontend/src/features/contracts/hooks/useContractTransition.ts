import { useMutation, useQueryClient } from '@tanstack/react-query';
import { contractsApi } from '../api/contracts';
import { contractQueryKeys } from './useContractDetail';
import type { ContractLifecycleState } from '../types';

/**
 * Hook para transicionar o lifecycle de uma versão de contrato.
 * Invalida automaticamente o detalhe após transição bem sucedida.
 */
export function useContractTransition(contractVersionId: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (targetState: ContractLifecycleState) => {
      if (!contractVersionId) return Promise.reject(new Error('Missing contractVersionId'));
      return contractsApi.transitionLifecycle(contractVersionId, targetState);
    },
    onSuccess: () => {
      if (contractVersionId) {
        queryClient.invalidateQueries({ queryKey: contractQueryKeys.detail(contractVersionId) });
        queryClient.invalidateQueries({ queryKey: contractQueryKeys.lists() });
      }
    },
  });
}
