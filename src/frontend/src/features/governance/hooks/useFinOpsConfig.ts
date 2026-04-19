import { useQuery } from '@tanstack/react-query';
import { finOpsApi } from '../api/finOps';
import { queryKeys } from '../../../shared/api/queryKeys';

/** Hook que lê a configuração operacional de FinOps (moeda, gate, aprovadores). */
export function useFinOpsConfig() {
  return useQuery({
    queryKey: queryKeys.governance.finops.configuration(),
    queryFn: finOpsApi.getConfiguration,
    staleTime: 5 * 60_000, // 5 minutos — configuração muda pouco
    retry: 1,
  });
}

/** Devolve a moeda configurada ou 'USD' como fallback. */
export function useFinOpsCurrency(): string {
  const { data } = useFinOpsConfig();
  return data?.currency ?? 'USD';
}
