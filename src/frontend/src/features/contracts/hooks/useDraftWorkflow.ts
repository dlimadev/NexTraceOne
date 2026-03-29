import { useMutation, useQueryClient } from '@tanstack/react-query';
import { contractStudioApi } from '../api/contractStudio';
import type { ContractType, ContractProtocol, DraftStatus } from '../types';

const draftKeys = {
  all: ['contract-drafts'] as const,
  lists: () => [...draftKeys.all, 'list'] as const,
  detail: (id: string) => [...draftKeys.all, 'detail', id] as const,
  reviews: (id: string) => [...draftKeys.all, 'reviews', id] as const,
};

/**
 * Hook para criar um draft de contrato (manual ou via IA).
 */
export function useCreateDraft() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: {
      title: string;
      author: string;
      contractType: ContractType;
      protocol: ContractProtocol;
      serviceId?: string;
      description?: string;
    }) => contractStudioApi.createDraft(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: draftKeys.lists() });
    },
  });
}

/**
 * Hook para submeter um draft para revisão.
 */
export function useSubmitForReview() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (draftId: string) => contractStudioApi.submitForReview(draftId),
    onSuccess: (_data, draftId) => {
      queryClient.invalidateQueries({ queryKey: draftKeys.detail(draftId) });
      queryClient.invalidateQueries({ queryKey: draftKeys.lists() });
    },
  });
}

/**
 * Hook para publicar um draft como versão oficial.
 */
export function usePublishDraft() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: ({ draftId, publishedBy }: { draftId: string; publishedBy: string }) =>
      contractStudioApi.publish(draftId, { publishedBy }),
    onSuccess: (_data, { draftId }) => {
      queryClient.invalidateQueries({ queryKey: draftKeys.detail(draftId) });
      queryClient.invalidateQueries({ queryKey: draftKeys.lists() });
    },
  });
}

/**
 * Hook para gerar um draft de contrato assistido por IA.
 * Chama o endpoint POST /contracts/drafts/ai/generate.
 */
export function useGenerateFromAi() {
  const queryClient = useQueryClient();

  return useMutation({
    mutationFn: (data: {
      title: string;
      author: string;
      contractType: ContractType;
      protocol: ContractProtocol;
      prompt: string;
      serviceId?: string;
    }) => contractStudioApi.generateFromAi(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: draftKeys.lists() });
    },
  });
}

export { draftKeys };
export type { DraftStatus };
