import client from '../../../api/client';
import type {
  ContractDraft,
  ContractReviewEntry,
  DraftListResponse,
  ContractType,
  ContractProtocol,
  DraftStatus,
  SoapDraftCreateResponse,
  EventDraftCreateResponse,
  BackgroundServiceDraftCreateResponse,
} from '../types';

function readValue<T>(source: Record<string, unknown>, camel: string, pascal?: string): T | undefined {
  const pascalKey = pascal ?? `${camel.charAt(0).toUpperCase()}${camel.slice(1)}`;
  return (source[camel] as T | undefined) ?? (source[pascalKey] as T | undefined);
}

function mapDraft(raw: unknown): ContractDraft {
  const source = (raw ?? {}) as Record<string, unknown>;
  const exampleItems = (readValue<unknown[]>(source, 'examples') ?? []);

  return {
    id: readValue<string>(source, 'id') ?? readValue<string>(source, 'draftId') ?? '',
    title: readValue<string>(source, 'title') ?? '',
    description: readValue<string>(source, 'description') ?? '',
    serviceId: readValue<string>(source, 'serviceId'),
    contractType: readValue<ContractType>(source, 'contractType') ?? 'RestApi',
    protocol: readValue<ContractProtocol>(source, 'protocol') ?? 'OpenApi',
    specContent: readValue<string>(source, 'specContent') ?? '',
    format: readValue<string>(source, 'format') ?? 'yaml',
    proposedVersion: readValue<string>(source, 'proposedVersion') ?? '1.0.0',
    status: readValue<DraftStatus>(source, 'status') ?? 'Editing',
    author: readValue<string>(source, 'author') ?? '',
    baseContractVersionId: readValue<string>(source, 'baseContractVersionId'),
    isAiGenerated: readValue<boolean>(source, 'isAiGenerated') ?? false,
    aiGenerationPrompt: readValue<string>(source, 'aiGenerationPrompt'),
    lastEditedAt: readValue<string>(source, 'lastEditedAt'),
    lastEditedBy: readValue<string>(source, 'lastEditedBy'),
    createdAt: readValue<string>(source, 'createdAt') ?? '',
    examples: exampleItems.map((example: unknown) => {
      const item = example as Record<string, unknown>;
      return {
        id: readValue<string>(item, 'id') ?? '',
        name: readValue<string>(item, 'name') ?? '',
        description: readValue<string>(item, 'description') ?? '',
        content: readValue<string>(item, 'content') ?? '',
        contentFormat: readValue<string>(item, 'contentFormat') ?? '',
        exampleType: readValue<string>(item, 'exampleType') ?? '',
        createdBy: readValue<string>(item, 'createdBy') ?? '',
        createdAt: readValue<string>(item, 'createdAt') ?? '',
      };
    }),
  };
}

function mapReview(raw: unknown): ContractReviewEntry {
  const source = (raw ?? {}) as Record<string, unknown>;
  return {
    id: readValue<string>(source, 'id') ?? '',
    draftId: readValue<string>(source, 'draftId') ?? '',
    reviewedBy: readValue<string>(source, 'reviewedBy') ?? '',
    decision: readValue<ContractReviewEntry['decision']>(source, 'decision') ?? 'Approved',
    comment: readValue<string>(source, 'comment') ?? '',
    reviewedAt: readValue<string>(source, 'reviewedAt') ?? '',
  };
}

function mapDraftCreationResult(raw: unknown) {
  const source = (raw ?? {}) as Record<string, unknown>;
  return {
    draftId: readValue<string>(source, 'draftId') ?? '',
    title: readValue<string>(source, 'title') ?? '',
    status: readValue<string>(source, 'status') ?? 'Editing',
    createdAt: readValue<string>(source, 'createdAt') ?? '',
  };
}

export const contractStudioApi = {
  createDraft: (data: {
    title: string;
    author: string;
    contractType: ContractType;
    protocol: ContractProtocol;
    serviceId?: string;
    description?: string;
  }) =>
    client.post('/contracts/drafts', data).then((r) => mapDraftCreationResult(r.data)),

  getDraft: (draftId: string) =>
    client.get(`/contracts/drafts/${draftId}`).then((r) => mapDraft(r.data)),

  listDrafts: (params?: {
    status?: DraftStatus;
    serviceId?: string;
    author?: string;
    page?: number;
    pageSize?: number;
  }) =>
    client.get('/contracts/drafts', { params }).then((r) => {
      const payload = (r.data ?? {}) as Record<string, unknown>;
      return {
        items: ((payload.items as unknown[]) ?? []).map(mapDraft),
        totalCount: (payload.totalCount as number) ?? 0,
        page: (payload.page as number) ?? 1,
        pageSize: (payload.pageSize as number) ?? 20,
      } as DraftListResponse;
    }),

  updateContent: (draftId: string, data: {
    specContent: string;
    format: string;
    editedBy: string;
  }) =>
    client.patch(`/contracts/drafts/${draftId}/content`, data).then(r => r.data),

  updateMetadata: (draftId: string, data: {
    title?: string;
    description?: string;
    proposedVersion?: string;
    serviceId?: string;
    editedBy: string;
  }) =>
    client.patch(`/contracts/drafts/${draftId}/metadata`, data).then(r => r.data),

  submitForReview: (draftId: string) =>
    client.post(`/contracts/drafts/${draftId}/submit-review`).then(r => r.data),

  approve: (draftId: string, data: { approvedBy: string; comment?: string }) =>
    client.post(`/contracts/drafts/${draftId}/approve`, data).then(r => r.data),

  reject: (draftId: string, data: { rejectedBy: string; comment?: string }) =>
    client.post(`/contracts/drafts/${draftId}/reject`, data).then(r => r.data),

  publish: (draftId: string, data: { publishedBy: string }) =>
    client.post<{ contractVersionId: string }>(`/contracts/drafts/${draftId}/publish`, data).then(r => r.data),

  addExample: (draftId: string, data: {
    name: string;
    content: string;
    contentFormat: string;
    exampleType: string;
    createdBy: string;
    description?: string;
  }) =>
    client.post<{ exampleId: string }>(`/contracts/drafts/${draftId}/examples`, data).then(r => r.data),

  listReviews: (draftId: string) =>
    client.get(`/contracts/drafts/${draftId}/reviews`).then((r) => ((r.data as unknown[]) ?? []).map(mapReview)),

  // ── SOAP/WSDL draft workflow ──────────────────────────────────────

  /**
   * Cria um draft específico para contratos SOAP/WSDL com metadados SOAP inicializados.
   * Cria ContractDraft (ContractType=Soap, Protocol=Wsdl) + SoapDraftMetadata.
   */
  createSoapDraft: (data: {
    title: string;
    author: string;
    serviceName: string;
    targetNamespace: string;
    soapVersion?: '1.1' | '1.2';
    serviceId?: string;
    description?: string;
    endpointUrl?: string;
    portTypeName?: string;
    bindingName?: string;
    operationsJson?: string;
  }) =>
    client.post<SoapDraftCreateResponse>('/contracts/drafts/soap', {
      title: data.title,
      author: data.author,
      serviceName: data.serviceName,
      targetNamespace: data.targetNamespace,
      soapVersion: data.soapVersion ?? '1.1',
      serviceId: data.serviceId,
      description: data.description,
      endpointUrl: data.endpointUrl,
      portTypeName: data.portTypeName,
      bindingName: data.bindingName,
      operationsJson: data.operationsJson,
    }).then((r) => r.data),

  /**
   * Cria um draft específico para contratos Event/AsyncAPI com metadados de evento inicializados.
   * Cria ContractDraft (ContractType=Event, Protocol=AsyncApi) + EventDraftMetadata.
   */
  createEventDraft: (data: {
    title: string;
    author: string;
    asyncApiVersion?: string;
    serviceId?: string;
    description?: string;
    defaultContentType?: string;
    channelsJson?: string;
    messagesJson?: string;
  }) =>
    client.post<EventDraftCreateResponse>('/contracts/drafts/event', {
      title: data.title,
      author: data.author,
      asyncApiVersion: data.asyncApiVersion ?? '2.6.0',
      serviceId: data.serviceId,
      description: data.description,
      defaultContentType: data.defaultContentType ?? 'application/json',
      channelsJson: data.channelsJson,
      messagesJson: data.messagesJson,
    }).then((r) => r.data),

  /**
   * Cria um draft específico para Background Service Contracts com metadados do processo.
   * Cria ContractDraft (ContractType=BackgroundService) + BackgroundServiceDraftMetadata.
   */
  createBackgroundServiceDraft: (data: {
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
  }) =>
    client.post<BackgroundServiceDraftCreateResponse>('/contracts/drafts/background-service', {
      title: data.title,
      author: data.author,
      serviceName: data.serviceName,
      category: data.category ?? 'Job',
      triggerType: data.triggerType ?? 'OnDemand',
      serviceId: data.serviceId,
      description: data.description,
      scheduleExpression: data.scheduleExpression,
      inputsJson: data.inputsJson,
      outputsJson: data.outputsJson,
      sideEffectsJson: data.sideEffectsJson,
    }).then((r) => r.data),

  // ── AI Generation ─────────────────────────────────────────────────────────

  /**
   * Gera um draft de contrato assistido por IA.
   * O backend delega ao IAiDraftGenerator e cria um ContractDraft persistido.
   */
  generateFromAi: (data: {
    title: string;
    author: string;
    contractType: ContractType;
    protocol: ContractProtocol;
    prompt: string;
    serviceId?: string;
  }) =>
    client.post('/contracts/drafts/ai/generate', data).then((r) => mapDraftCreationResult(r.data)),

  // ── Draft Export ──────────────────────────────────────────────────────────

  /**
   * Exporta o conteúdo bruto da especificação de um draft para utilização
   * com ferramentas externas (dotnet-openapi, NSwag, Kiota) antes da publicação.
   */
  exportDraft: (draftId: string) =>
    client.get<{
      draftId: string;
      title: string;
      specContent: string;
      format: string;
      proposedVersion: string;
      protocol: string;
      contractType: string;
    }>(`/contracts/drafts/${draftId}/export`).then((r) => r.data),
};
