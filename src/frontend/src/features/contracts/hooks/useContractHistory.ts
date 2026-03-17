import { useQuery } from '@tanstack/react-query';
import { contractsApi } from '../api/contracts';
import { contractQueryKeys } from './useContractDetail';

/**
 * Hook para obter o histórico de versões de um contrato (por apiAssetId).
 */
export function useContractHistory(apiAssetId: string | undefined) {
  return useQuery({
    queryKey: contractQueryKeys.history(apiAssetId ?? ''),
    queryFn: () => contractsApi.getHistory(apiAssetId!),
    enabled: !!apiAssetId,
  });
}
