import { useQuery } from '@tanstack/react-query';
import { contractsApi } from '../api/contracts';
import { contractQueryKeys } from './useContractDetail';

/**
 * Hook para listar contratos com filtros e paginação.
 */
export function useContractList(params?: {
  protocol?: string;
  lifecycleState?: string;
  searchTerm?: string;
  page?: number;
  pageSize?: number;
}) {
  return useQuery({
    queryKey: contractQueryKeys.list(params as Record<string, unknown>),
    queryFn: () => contractsApi.listContracts(params),
  });
}

/**
 * Hook para obter o resumo agregado de contratos.
 */
export function useContractsSummary() {
  return useQuery({
    queryKey: contractQueryKeys.summary(),
    queryFn: () => contractsApi.getContractsSummary(),
  });
}
