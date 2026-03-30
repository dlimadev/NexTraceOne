import { useQuery } from '@tanstack/react-query';
import { contractsApi } from '../api/contracts';
import { contractQueryKeys } from './useContractDetail';

/**
 * Hook para listar subscrições formais de uma API via Developer Portal.
 * Permite ao produtor do contrato ver quem subscreveu notificações de mudanças.
 */
export function useContractSubscribers(apiAssetId: string | undefined) {
  return useQuery({
    queryKey: contractQueryKeys.subscribers(apiAssetId ?? ''),
    queryFn: () => contractsApi.getSubscribers(apiAssetId!),
    enabled: !!apiAssetId,
  });
}
