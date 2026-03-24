/**
 * API client para o módulo de Configuration.
 * Comunicação com os endpoints REST do backend de configuração.
 */
import apiClient from '@/api/client';
import type {
  ConfigurationDefinitionDto,
  ConfigurationEntryDto,
  EffectiveConfigurationDto,
  ConfigurationAuditEntryDto,
  SetConfigurationValueRequest,
  ToggleConfigurationRequest,
} from '../types';

export const configurationApi = {
  getDefinitions: async (): Promise<ConfigurationDefinitionDto[]> => {
    const { data } = await apiClient.get<ConfigurationDefinitionDto[]>(
      '/configuration/definitions',
    );
    return data;
  },

  getEntries: async (
    scope: string,
    scopeReferenceId?: string | null,
  ): Promise<ConfigurationEntryDto[]> => {
    const params: Record<string, string> = { scope };
    if (scopeReferenceId) {
      params.scopeReferenceId = scopeReferenceId;
    }
    const { data } = await apiClient.get<ConfigurationEntryDto[]>(
      '/configuration/entries',
      { params },
    );
    return data;
  },

  getEffectiveSettings: async (
    scope: string,
    scopeReferenceId?: string | null,
    key?: string | null,
  ): Promise<EffectiveConfigurationDto[]> => {
    const params: Record<string, string> = { scope };
    if (scopeReferenceId) {
      params.scopeReferenceId = scopeReferenceId;
    }
    if (key) {
      params.key = key;
    }
    const { data } = await apiClient.get<
      | EffectiveConfigurationDto[]
      | { setting: EffectiveConfigurationDto | null; settings: EffectiveConfigurationDto[] | null }
    >('/configuration/effective', { params });

    // Backend returns { settings: [...] } for list or { setting: {...} } for single key
    if (Array.isArray(data)) {
      return data;
    }
    if (data.settings) {
      return data.settings;
    }
    if (data.setting) {
      return [data.setting];
    }
    return [];
  },

  setConfigurationValue: async (
    key: string,
    body: SetConfigurationValueRequest,
  ): Promise<void> => {
    await apiClient.put(`/configuration/${encodeURIComponent(key)}`, body);
  },

  removeOverride: async (
    key: string,
    scope: string,
    scopeReferenceId?: string | null,
    changeReason?: string,
  ): Promise<void> => {
    const params: Record<string, string> = { scope };
    if (scopeReferenceId) {
      params.scopeReferenceId = scopeReferenceId;
    }
    if (changeReason) {
      params.changeReason = changeReason;
    }
    await apiClient.delete(
      `/configuration/${encodeURIComponent(key)}/override`,
      { params },
    );
  },

  toggleConfiguration: async (
    key: string,
    body: ToggleConfigurationRequest,
  ): Promise<void> => {
    await apiClient.post(
      `/configuration/${encodeURIComponent(key)}/toggle`,
      body,
    );
  },

  getAuditHistory: async (
    key: string,
    limit?: number,
  ): Promise<ConfigurationAuditEntryDto[]> => {
    const params: Record<string, string | number> = {};
    if (limit) {
      params.limit = limit;
    }
    const { data } = await apiClient.get<ConfigurationAuditEntryDto[]>(
      `/configuration/${encodeURIComponent(key)}/audit`,
      { params },
    );
    return data;
  },
};
