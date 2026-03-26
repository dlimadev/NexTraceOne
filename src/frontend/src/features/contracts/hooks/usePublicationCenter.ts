import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { publicationCenterApi } from '../api/publicationCenter';
import type { PublicationVisibility } from '../../../types';

const publicationCenterKeys = {
  all: ['publication-center'] as const,
  list: (filters?: { status?: string; apiAssetId?: string }) =>
    [...publicationCenterKeys.all, 'list', filters] as const,
  status: (contractVersionId: string) =>
    [...publicationCenterKeys.all, 'status', contractVersionId] as const,
};

/**
 * Hook para publicar um contrato no Developer Portal.
 * Chama POST /api/v1/publication-center/publish.
 */
export function usePublishContractToPortal() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: {
      contractVersionId: string;
      apiAssetId: string;
      contractTitle: string;
      semVer: string;
      publishedBy: string;
      lifecycleState: string;
      visibility?: PublicationVisibility;
      releaseNotes?: string;
    }) => publicationCenterApi.publishContract(data),
    onSuccess: (_data, variables) => {
      queryClient.invalidateQueries({ queryKey: publicationCenterKeys.all });
      queryClient.invalidateQueries({
        queryKey: publicationCenterKeys.status(variables.contractVersionId),
      });
    },
  });
}

/**
 * Hook para retirar a publicação de um contrato do Developer Portal.
 * Chama POST /api/v1/publication-center/{entryId}/withdraw.
 */
export function useWithdrawContractFromPortal() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ entryId, reason }: { entryId: string; reason?: string }) =>
      publicationCenterApi.withdrawContract(entryId, reason),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: publicationCenterKeys.all });
    },
  });
}

/**
 * Hook para listar entradas do Publication Center.
 * Chama GET /api/v1/publication-center.
 */
export function usePublicationCenterEntries(filters?: {
  status?: string;
  apiAssetId?: string;
  page?: number;
  pageSize?: number;
}) {
  return useQuery({
    queryKey: publicationCenterKeys.list({ status: filters?.status, apiAssetId: filters?.apiAssetId }),
    queryFn: () => publicationCenterApi.listPublications(filters),
    staleTime: 2 * 60 * 1000,
  });
}

/**
 * Hook para consultar o estado de publicação de uma versão de contrato.
 * Chama GET /api/v1/publication-center/contracts/{contractVersionId}/status.
 */
export function useContractPublicationStatus(contractVersionId: string | undefined) {
  return useQuery({
    queryKey: publicationCenterKeys.status(contractVersionId ?? ''),
    queryFn: () => publicationCenterApi.getPublicationStatus(contractVersionId!),
    enabled: Boolean(contractVersionId),
    staleTime: 1 * 60 * 1000,
  });
}

export { publicationCenterKeys };
