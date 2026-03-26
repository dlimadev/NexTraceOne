/**
 * Cliente API do Publication Center.
 * Governa a exposição de contratos aprovados no Developer Portal.
 * Endpoints: POST /publication-center/publish, POST /publication-center/{id}/withdraw,
 *             GET /publication-center, GET /publication-center/contracts/{id}/status.
 */
import client from '../../../api/client';
import type {
  ContractPublicationEntry,
  PublishContractToPortalResponse,
  WithdrawContractFromPortalResponse,
  ContractPublicationStatusResponse,
  PublicationCenterListResponse,
  PublicationVisibility,
} from '../../../types';

export const publicationCenterApi = {
  /**
   * Publica uma versão de contrato aprovada no Developer Portal.
   * Cria uma ContractPublicationEntry em estado Published.
   */
  publishContract: (data: {
    contractVersionId: string;
    apiAssetId: string;
    contractTitle: string;
    semVer: string;
    publishedBy: string;
    lifecycleState: string;
    visibility?: PublicationVisibility;
    releaseNotes?: string;
  }) =>
    client.post<PublishContractToPortalResponse>('/publication-center/publish', data)
      .then((r) => r.data),

  /**
   * Retira a publicação de um contrato do Developer Portal.
   * Transiciona a entrada de Published → Withdrawn.
   */
  withdrawContract: (entryId: string, reason?: string) =>
    client.post<WithdrawContractFromPortalResponse>(
      `/publication-center/${entryId}/withdraw`,
      reason ? { reason } : {},
    ).then((r) => r.data),

  /**
   * Lista entradas do Publication Center com filtros opcionais.
   */
  listPublications: (params?: {
    status?: string;
    apiAssetId?: string;
    page?: number;
    pageSize?: number;
  }) =>
    client.get<PublicationCenterListResponse>('/publication-center', {
      params: {
        status: params?.status,
        apiAssetId: params?.apiAssetId,
        page: params?.page ?? 1,
        pageSize: params?.pageSize ?? 20,
      },
    }).then((r) => r.data),

  /**
   * Obtém o estado de publicação de uma versão de contrato específica.
   * Retorna NotPublished quando não existe entrada de publicação.
   */
  getPublicationStatus: (contractVersionId: string) =>
    client.get<ContractPublicationStatusResponse>(
      `/publication-center/contracts/${contractVersionId}/status`,
    ).then((r) => r.data),
};
