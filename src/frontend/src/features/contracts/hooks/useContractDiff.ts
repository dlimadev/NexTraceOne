import { useMutation } from '@tanstack/react-query';
import { contractsApi } from '../api/contracts';

/**
 * Hook para computar diff semântico entre duas versões.
 */
export function useContractDiff() {
  return useMutation({
    mutationFn: ({ fromVersionId, toVersionId }: { fromVersionId: string; toVersionId: string }) =>
      contractsApi.computeDiff(fromVersionId, toVersionId),
  });
}
