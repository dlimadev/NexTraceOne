import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { contractsApi } from '../api/contracts';
import { contractQueryKeys } from './useContractDetail';

/**
 * Hook para listar entidades Canonical com filtros opcionais.
 */
export function useCanonicalEntities(params?: { domain?: string; state?: string; category?: string; searchTerm?: string }) {
  return useQuery({
    queryKey: contractQueryKeys.canonicalEntities(params as Record<string, unknown>),
    queryFn: () => contractsApi.listCanonicalEntities(params),
  });
}

/**
 * Hook para obter detalhe de uma entidade Canonical.
 */
export function useCanonicalEntity(entityId: string | undefined) {
  return useQuery({
    queryKey: contractQueryKeys.canonicalEntity(entityId ?? ''),
    queryFn: () => contractsApi.getCanonicalEntity(entityId!),
    enabled: !!entityId,
  });
}

/**
 * Hook para obter usos/referências de uma entidade Canonical.
 */
export function useCanonicalEntityUsages(entityId: string | undefined) {
  return useQuery({
    queryKey: contractQueryKeys.canonicalUsages(entityId ?? ''),
    queryFn: () => contractsApi.getCanonicalEntityUsages(entityId!),
    enabled: !!entityId,
  });
}

/**
 * Hook para criar uma nova entidade Canonical.
 */
export function useCreateCanonicalEntity() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: contractsApi.createCanonicalEntity,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contractQueryKeys.canonicalEntities() });
    },
  });
}

/**
 * Hook para atualizar uma entidade Canonical.
 */
export function useUpdateCanonicalEntity(entityId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: Parameters<typeof contractsApi.updateCanonicalEntity>[1]) =>
      contractsApi.updateCanonicalEntity(entityId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contractQueryKeys.canonicalEntities() });
      queryClient.invalidateQueries({ queryKey: contractQueryKeys.canonicalEntity(entityId) });
    },
  });
}

/**
 * Hook para promover um schema de contrato a entidade Canonical.
 */
export function usePromoteToCanonical() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: contractsApi.promoteToCanonical,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contractQueryKeys.canonicalEntities() });
    },
  });
}
