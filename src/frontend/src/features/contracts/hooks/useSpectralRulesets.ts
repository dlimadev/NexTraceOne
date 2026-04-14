import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { contractsApi } from '../api/contracts';
import { contractQueryKeys } from './useContractDetail';

/**
 * Hook para listar rulesets Spectral com filtros opcionais.
 */
export function useSpectralRulesets(params?: { isActive?: boolean }) {
  return useQuery({
    queryKey: contractQueryKeys.spectralRulesets(params as Record<string, unknown>),
    queryFn: () => contractsApi.listSpectralRulesets(params),
  });
}

/**
 * Hook para obter detalhe de um ruleset Spectral.
 */
export function useSpectralRuleset(rulesetId: string | undefined) {
  return useQuery({
    queryKey: contractQueryKeys.spectralRuleset(rulesetId ?? ''),
    queryFn: () => contractsApi.getSpectralRuleset(rulesetId!),
    enabled: !!rulesetId,
  });
}

/**
 * Hook para criar um novo ruleset Spectral.
 */
export function useCreateSpectralRuleset() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: contractsApi.createSpectralRuleset,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contractQueryKeys.spectralRulesets() });
    },
  });
}

/**
 * Hook para atualizar um ruleset Spectral.
 */
export function useUpdateSpectralRuleset(rulesetId: string) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: Parameters<typeof contractsApi.updateSpectralRuleset>[1]) =>
      contractsApi.updateSpectralRuleset(rulesetId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contractQueryKeys.spectralRulesets() });
      queryClient.invalidateQueries({ queryKey: contractQueryKeys.spectralRuleset(rulesetId) });
    },
  });
}

/**
 * Hook para alternar estado ativo/inativo de um ruleset.
 * Usa archive (desativar) ou activate (reativar) conforme o estado desejado.
 */
export function useToggleSpectralRuleset() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ rulesetId, isActive }: { rulesetId: string; isActive: boolean }) =>
      isActive
        ? contractsApi.activateSpectralRuleset(rulesetId)
        : contractsApi.archiveSpectralRuleset(rulesetId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contractQueryKeys.spectralRulesets() });
    },
  });
}

/**
 * Hook para eliminar um ruleset Spectral.
 */
export function useDeleteSpectralRuleset() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: contractsApi.deleteSpectralRuleset,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: contractQueryKeys.spectralRulesets() });
    },
  });
}
