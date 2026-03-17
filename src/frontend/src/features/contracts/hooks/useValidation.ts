import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { contractsApi } from '../api/contracts';
import { contractQueryKeys } from './useContractDetail';

/**
 * Hook para obter o resumo de validação de um contrato.
 */
export function useValidationSummary(contractVersionId: string | undefined) {
  return useQuery({
    queryKey: contractQueryKeys.validationSummary(contractVersionId ?? ''),
    queryFn: () => contractsApi.getValidationSummary(contractVersionId!),
    enabled: !!contractVersionId,
  });
}

/**
 * Hook para executar validação Spectral + interna num contrato.
 */
export function useExecuteValidation(contractVersionId: string | undefined) {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: () => contractsApi.executeValidation(contractVersionId!),
    onSuccess: () => {
      if (contractVersionId) {
        queryClient.invalidateQueries({ queryKey: contractQueryKeys.validationSummary(contractVersionId) });
        queryClient.invalidateQueries({ queryKey: contractQueryKeys.violations(contractVersionId) });
      }
    },
  });
}

/**
 * Hook para validar conteúdo de spec ad-hoc (sem contrato persistido).
 */
export function useValidateSpec() {
  return useMutation({
    mutationFn: contractsApi.validateSpecContent,
  });
}
