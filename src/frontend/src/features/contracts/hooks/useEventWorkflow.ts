import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { contractsApi } from '../api/contracts';
import { contractStudioApi } from '../api/contractStudio';

const eventKeys = {
  all: ['event-contracts'] as const,
  detail: (contractVersionId: string) => [...eventKeys.all, 'detail', contractVersionId] as const,
  draftAll: ['event-drafts'] as const,
};

/**
 * Hook para importar uma spec AsyncAPI com extração real de metadados de evento.
 * Chama POST /api/v1/contracts/asyncapi/import e retorna ContractVersionId + EventContractDetail.
 */
export function useAsyncApiImport() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: {
      apiAssetId: string;
      semVer: string;
      asyncApiContent: string;
      importedFrom: string;
      defaultContentType?: string;
    }) => contractsApi.importAsyncApi(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contracts'] });
      queryClient.invalidateQueries({ queryKey: eventKeys.all });
    },
  });
}

/**
 * Hook para criar um draft de evento/AsyncAPI com metadados específicos.
 * Chama POST /api/v1/contracts/drafts/event.
 */
export function useCreateEventDraft() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: {
      title: string;
      author: string;
      asyncApiVersion?: string;
      serviceId?: string;
      description?: string;
      defaultContentType?: string;
      channelsJson?: string;
      messagesJson?: string;
    }) => contractStudioApi.createEventDraft(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contract-drafts'] });
      queryClient.invalidateQueries({ queryKey: eventKeys.draftAll });
    },
  });
}

/**
 * Hook para consultar os detalhes AsyncAPI de uma versão de contrato de evento publicada.
 * Chama GET /api/v1/contracts/{contractVersionId}/event-detail.
 */
export function useEventContractDetail(contractVersionId: string | undefined) {
  return useQuery({
    queryKey: eventKeys.detail(contractVersionId ?? ''),
    queryFn: () => contractsApi.getEventContractDetail(contractVersionId!),
    enabled: Boolean(contractVersionId),
    staleTime: 5 * 60 * 1000,
  });
}

export { eventKeys };
