/**
 * Hooks React Query para o módulo de Configuration.
 * Segue o padrão de query keys factory e invalidação de cache nas mutations.
 */
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import axios from 'axios';
import { configurationApi } from '../api/configurationApi';
import type {
  SetConfigurationValueRequest,
  ToggleConfigurationRequest,
} from '../types';

/**
 * Verifica se um erro é uma resposta HTTP 409 (conflito de concorrência optimista).
 * O backend retorna 409 com código CONFIG_CONCURRENCY_CONFLICT ou
 * FEATURE_FLAG_CONCURRENCY_CONFLICT quando a versão (xmin) do registo diverge.
 */
function isConcurrencyConflict(error: unknown): boolean {
  return axios.isAxiosError(error) && error.response?.status === 409;
}

export const configurationKeys = {
  all: ['configuration'] as const,
  definitions: () => [...configurationKeys.all, 'definitions'] as const,
  entries: (scope: string, scopeReferenceId?: string | null) =>
    [...configurationKeys.all, 'entries', scope, scopeReferenceId] as const,
  effective: (scope: string, scopeReferenceId?: string | null) =>
    [...configurationKeys.all, 'effective', scope, scopeReferenceId] as const,
  audit: (key: string) =>
    [...configurationKeys.all, 'audit', key] as const,
};

export function useConfigurationDefinitions() {
  return useQuery({
    queryKey: configurationKeys.definitions(),
    queryFn: () => configurationApi.getDefinitions(),
  });
}

export function useConfigurationEntries(
  scope: string,
  scopeReferenceId?: string | null,
) {
  return useQuery({
    queryKey: configurationKeys.entries(scope, scopeReferenceId),
    queryFn: () => configurationApi.getEntries(scope, scopeReferenceId),
    enabled: !!scope,
  });
}

export function useEffectiveSettings(
  scope: string,
  scopeReferenceId?: string | null,
) {
  return useQuery({
    queryKey: configurationKeys.effective(scope, scopeReferenceId),
    queryFn: () =>
      configurationApi.getEffectiveSettings(scope, scopeReferenceId),
    enabled: !!scope,
  });
}

export function useSetConfigurationValue() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      key,
      data,
    }: {
      key: string;
      data: SetConfigurationValueRequest;
    }) => configurationApi.setConfigurationValue(key, data),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: configurationKeys.all,
      });
    },
    onError: (error: unknown) => {
      if (isConcurrencyConflict(error)) {
        throw Object.assign(new Error('CONFIG_CONCURRENCY_CONFLICT'), {
          isConcurrencyConflict: true,
        });
      }
    },
  });
}

export function useRemoveOverride() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      key,
      scope,
      scopeReferenceId,
      changeReason,
    }: {
      key: string;
      scope: string;
      scopeReferenceId?: string | null;
      changeReason?: string;
    }) =>
      configurationApi.removeOverride(
        key,
        scope,
        scopeReferenceId,
        changeReason,
      ),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: configurationKeys.all,
      });
    },
    onError: (error: unknown) => {
      if (isConcurrencyConflict(error)) {
        throw Object.assign(new Error('CONFIG_CONCURRENCY_CONFLICT'), {
          isConcurrencyConflict: true,
        });
      }
    },
  });
}

export function useToggleConfiguration() {
  const queryClient = useQueryClient();
  return useMutation({
    mutationFn: ({
      key,
      data,
    }: {
      key: string;
      data: ToggleConfigurationRequest;
    }) => configurationApi.toggleConfiguration(key, data),
    onSuccess: async () => {
      await queryClient.invalidateQueries({
        queryKey: configurationKeys.all,
      });
    },
    onError: (error: unknown) => {
      if (isConcurrencyConflict(error)) {
        throw Object.assign(new Error('CONFIG_CONCURRENCY_CONFLICT'), {
          isConcurrencyConflict: true,
        });
      }
    },
  });
}

export function useAuditHistory(key: string | null) {
  return useQuery({
    queryKey: configurationKeys.audit(key ?? ''),
    queryFn: () => configurationApi.getAuditHistory(key!, 50),
    enabled: !!key,
  });
}
