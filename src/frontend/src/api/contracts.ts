import client from './client';
import type { ContractVersion, ContractVersionDetail, SignatureVerificationResult, SemanticDiff, ContractProtocol } from '../types';

/**
 * Detecta o formato da especificação (json, yaml ou xml) a partir do conteúdo bruto.
 * Utilizado para preencher automaticamente o campo format ao importar contratos.
 */
function detectFormat(content: string): 'json' | 'yaml' | 'xml' {
  const trimmed = content.trim();
  if (trimmed.startsWith('{') || trimmed.startsWith('[')) return 'json';
  if (trimmed.startsWith('<') || trimmed.startsWith('<?xml')) return 'xml';
  return 'yaml';
}

export const contractsApi = {
  importContract: (data: {
    apiAssetId: string;
    content: string;
    version: string;
    protocol?: ContractProtocol;
    format?: string;
    importedFrom?: string;
  }) =>
    client.post<{ id: string }>('/contracts', {
      apiAssetId: data.apiAssetId,
      semVer: data.version,
      specContent: data.content,
      format: data.format || detectFormat(data.content),
      importedFrom: data.importedFrom || 'upload',
      protocol: data.protocol || 'OpenApi',
    }).then((r) => r.data),

  createVersion: (data: {
    apiAssetId: string;
    content: string;
    version: string;
    protocol?: ContractProtocol;
    format?: string;
    importedFrom?: string;
  }) =>
    client.post<{ id: string }>('/contracts/versions', {
      apiAssetId: data.apiAssetId,
      semVer: data.version,
      specContent: data.content,
      format: data.format || detectFormat(data.content),
      importedFrom: data.importedFrom || 'upload',
      protocol: data.protocol,
    }).then((r) => r.data),

  computeDiff: (fromVersionId: string, toVersionId: string) =>
    client
      .post<SemanticDiff>('/contracts/diff', { baseVersionId: fromVersionId, targetVersionId: toVersionId })
      .then((r) => r.data),

  getClassification: (contractVersionId: string) =>
    client
      .get(`/contracts/${contractVersionId}/classification`)
      .then((r) => r.data),

  suggestVersion: (apiAssetId: string, changeLevel: number) =>
    client
      .get<{ suggestedVersion: string }>('/contracts/suggest-version', {
        params: { apiAssetId, changeLevel },
      })
      .then((r) => r.data),

  getHistory: (apiAssetId: string) =>
    client
      .get<ContractVersion[]>(`/contracts/history/${apiAssetId}`)
      .then((r) => r.data),

  getDetail: (contractVersionId: string) =>
    client
      .get<ContractVersionDetail>(`/contracts/${contractVersionId}/detail`)
      .then((r) => r.data),

  lockVersion: (contractVersionId: string, lockedBy: string) =>
    client
      .post(`/contracts/${contractVersionId}/lock`, { lockedBy })
      .then((r) => r.data),

  transitionLifecycle: (contractVersionId: string, newState: string) =>
    client
      .post(`/contracts/${contractVersionId}/lifecycle`, { contractVersionId, newState })
      .then((r) => r.data),

  deprecateVersion: (contractVersionId: string, deprecationNotice: string, sunsetDate?: string) =>
    client
      .post(`/contracts/${contractVersionId}/deprecate`, { contractVersionId, deprecationNotice, sunsetDate })
      .then((r) => r.data),

  signVersion: (contractVersionId: string) =>
    client
      .post(`/contracts/${contractVersionId}/sign`, { contractVersionId })
      .then((r) => r.data),

  verifySignature: (contractVersionId: string) =>
    client
      .get<SignatureVerificationResult>(`/contracts/${contractVersionId}/verify`)
      .then((r) => r.data),

  exportVersion: (contractVersionId: string) =>
    client.get<{ specContent: string; format: string }>(`/contracts/${contractVersionId}/export`).then((r) => r.data),
};
