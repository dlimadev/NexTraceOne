import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { aiGovernanceApi } from '../api/aiGovernance';

export const aiPreferencesKeys = {
  all: ['ai-preferences'] as const,
  list: () => [...aiPreferencesKeys.all, 'list'] as const,
  detail: (featureKey: string) => [...aiPreferencesKeys.all, 'detail', featureKey] as const,
  availability: (featureKey: string) => [...aiPreferencesKeys.all, 'availability', featureKey] as const,
  preview: (featureKey: string, requestType?: string) =>
    [...aiPreferencesKeys.all, 'preview', featureKey, requestType ?? 'chat'] as const,
};

export interface UserAiPreferenceDto {
  id: string;
  featureKey: string;
  preferenceType: number;
  preferredModelName: string | null;
  preferredProviderId: string | null;
  externalProduct: number | null;
  externalProductModel: string | null;
  disableReason: string | null;
  isActive: boolean;
  createdAt: string;
}

export interface AiAvailabilityDto {
  featureKey: string;
  status: string;
  statusCode: number;
}

export interface AiExecutionPlanDto {
  providerType: number;
  providerId: string;
  modelId: string;
  modelDisplayName: string;
  isAvailable: boolean;
  unavailabilityReason: string | null;
  estimatedCost: number | null;
  appliedPolicies: string[];
}

export function useUserAiPreferences() {
  return useQuery({
    queryKey: aiPreferencesKeys.list(),
    queryFn: () => aiGovernanceApi.getUserAiPreferences() as Promise<{ items?: UserAiPreferenceDto[] } | UserAiPreferenceDto[]>,
  });
}

export function useUserAiPreference(featureKey: string) {
  return useQuery({
    queryKey: aiPreferencesKeys.detail(featureKey),
    queryFn: () => aiGovernanceApi.getUserAiPreference(featureKey) as Promise<UserAiPreferenceDto>,
    enabled: !!featureKey,
  });
}

export function useUpsertUserAiPreference() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (data: {
      featureKey: string;
      preferenceType: number;
      preferredModelId?: string | null;
      preferredProviderId?: string | null;
      externalProduct?: number | null;
      externalProductModel?: string | null;
      disableReason?: string | null;
    }) => aiGovernanceApi.upsertUserAiPreference(data),
    onSuccess: (_, variables) => {
      queryClient.invalidateQueries({ queryKey: aiPreferencesKeys.list() });
      queryClient.invalidateQueries({ queryKey: aiPreferencesKeys.detail(variables.featureKey) });
    },
  });
}

export function useDeleteUserAiPreference() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: (featureKey: string) => aiGovernanceApi.deleteUserAiPreference(featureKey),
    onSuccess: (_, featureKey) => {
      queryClient.invalidateQueries({ queryKey: aiPreferencesKeys.list() });
      queryClient.invalidateQueries({ queryKey: aiPreferencesKeys.detail(featureKey) });
    },
  });
}

export function useAiAvailability(featureKey: string) {
  return useQuery({
    queryKey: aiPreferencesKeys.availability(featureKey),
    queryFn: () => aiGovernanceApi.checkAiAvailability(featureKey) as Promise<AiAvailabilityDto>,
    enabled: !!featureKey,
  });
}

export function useAiExecutionPreview(featureKey: string, requestType = 'chat', enabled = true) {
  return useQuery({
    queryKey: aiPreferencesKeys.preview(featureKey, requestType),
    queryFn: () =>
      aiGovernanceApi.previewAiExecution({ featureKey, requestType }) as Promise<AiExecutionPlanDto>,
    enabled: enabled && !!featureKey,
  });
}
