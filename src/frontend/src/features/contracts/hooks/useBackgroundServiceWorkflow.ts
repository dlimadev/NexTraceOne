import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { contractsApi } from '../api/contracts';
import { contractStudioApi } from '../api/contractStudio';

const backgroundServiceKeys = {
  all: ['background-service-contracts'] as const,
  detail: (contractVersionId: string) => [...backgroundServiceKeys.all, 'detail', contractVersionId] as const,
  draftAll: ['background-service-drafts'] as const,
};

/**
 * Hook para registar um Background Service Contract com metadados específicos do processo.
 * Chama POST /api/v1/contracts/background-services/register.
 */
export function useRegisterBackgroundService() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: {
      apiAssetId: string;
      semVer: string;
      serviceName: string;
      category: string;
      triggerType: string;
      scheduleExpression?: string;
      timeoutExpression?: string;
      allowsConcurrency?: boolean;
      inputsJson?: string;
      outputsJson?: string;
      sideEffectsJson?: string;
      specContent?: string;
    }) => contractsApi.registerBackgroundService(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contracts'] });
      queryClient.invalidateQueries({ queryKey: backgroundServiceKeys.all });
    },
  });
}

/**
 * Hook para criar um draft de Background Service Contract com metadados específicos.
 * Chama POST /api/v1/contracts/drafts/background-service.
 */
export function useCreateBackgroundServiceDraft() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: {
      title: string;
      author: string;
      serviceName: string;
      category?: string;
      triggerType?: string;
      serviceId?: string;
      description?: string;
      scheduleExpression?: string;
      inputsJson?: string;
      outputsJson?: string;
      sideEffectsJson?: string;
    }) => contractStudioApi.createBackgroundServiceDraft(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contract-drafts'] });
      queryClient.invalidateQueries({ queryKey: backgroundServiceKeys.draftAll });
    },
  });
}

/**
 * Hook para consultar os detalhes de Background Service de uma versão de contrato publicada.
 * Chama GET /api/v1/contracts/{contractVersionId}/background-service-detail.
 */
export function useBackgroundServiceContractDetail(contractVersionId: string | undefined) {
  return useQuery({
    queryKey: backgroundServiceKeys.detail(contractVersionId ?? ''),
    queryFn: () => contractsApi.getBackgroundServiceContractDetail(contractVersionId!),
    enabled: Boolean(contractVersionId),
    staleTime: 5 * 60 * 1000,
  });
}

export { backgroundServiceKeys };
