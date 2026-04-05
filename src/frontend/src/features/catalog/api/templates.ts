import axios from 'axios';

// ── Types ─────────────────────────────────────────────────────────────────────

export type TemplateServiceType =
  | 'RestApi'
  | 'EventDriven'
  | 'BackgroundWorker'
  | 'Grpc'
  | 'Soap'
  | 'Generic';

export type TemplateLanguage =
  | 'DotNet'
  | 'NodeJs'
  | 'Java'
  | 'Go'
  | 'Python'
  | 'Agnostic';

export interface TemplateSummary {
  templateId: string;
  slug: string;
  displayName: string;
  description: string;
  version: string;
  serviceType: TemplateServiceType;
  language: TemplateLanguage;
  defaultDomain: string;
  defaultTeam: string;
  tags: string[];
  isActive: boolean;
  usageCount: number;
  hasBaseContract: boolean;
  hasScaffoldingManifest: boolean;
  createdAt: string;
  updatedAt?: string;
}

export interface TemplateDetail extends TemplateSummary {
  governancePolicyIds: string[];
  baseContractSpec?: string;
  scaffoldingManifestJson?: string;
  repositoryTemplateUrl?: string;
  repositoryTemplateBranch?: string;
  tenantId?: string;
}

export interface ScaffoldedFile {
  path: string;
  content?: string;
}

export interface ScaffoldResult {
  scaffoldingId: string;
  serviceName: string;
  templateId: string;
  templateSlug: string;
  templateVersion: string;
  serviceType: string;
  language: string;
  domain: string;
  teamName: string;
  governancePolicyIds: string[];
  baseContractSpec?: string;
  files: ScaffoldedFile[];
  repositoryUrl?: string;
  variables: Record<string, string>;
}

export interface AiScaffoldResult {
  scaffoldId: string;
  serviceName: string;
  templateId: string;
  templateSlug: string;
  language: string;
  serviceType: string;
  domain: string;
  teamName: string;
  files: ScaffoldedFile[];
  isFallback: boolean;
  generatedAt: string;
}

// ── Request types ──────────────────────────────────────────────────────────────

export interface CreateTemplateRequest {
  slug: string;
  displayName: string;
  description: string;
  version: string;
  serviceType: TemplateServiceType;
  language: TemplateLanguage;
  defaultDomain: string;
  defaultTeam: string;
  tags?: string[];
  governancePolicyIds?: string[];
  baseContractSpec?: string;
  scaffoldingManifestJson?: string;
  repositoryTemplateUrl?: string;
  repositoryTemplateBranch?: string;
  tenantId?: string;
}

export interface UpdateTemplateRequest {
  displayName: string;
  description: string;
  version: string;
  defaultDomain: string;
  defaultTeam: string;
  tags?: string[];
  governancePolicyIds?: string[];
  baseContractSpec?: string;
  scaffoldingManifestJson?: string;
  repositoryTemplateUrl?: string;
  repositoryTemplateBranch?: string;
}

export interface ScaffoldRequest {
  serviceName: string;
  teamName?: string;
  domain?: string;
  repositoryUrl?: string;
  extraVariables?: Record<string, string>;
}

export interface AiScaffoldRequest {
  templateId?: string;
  templateSlug?: string;
  serviceName: string;
  serviceDescription: string;
  teamName?: string;
  domain?: string;
  languageOverride?: string;
  mainEntities?: string;
  additionalRequirements?: string;
  preferredProvider?: string;
}

export interface ListTemplatesParams {
  isActive?: boolean;
  serviceType?: TemplateServiceType;
  language?: TemplateLanguage;
  search?: string;
  tenantId?: string;
}

// ── API client ────────────────────────────────────────────────────────────────

const BASE = '/api/v1/catalog/templates';
const AI_BASE = '/api/v1/aiorchestration/generate';

export const templatesApi = {
  list: (params?: ListTemplatesParams): Promise<TemplateSummary[]> =>
    axios.get(BASE, { params }).then(r => r.data),

  getById: (id: string): Promise<TemplateDetail> =>
    axios.get(`${BASE}/${id}`).then(r => r.data),

  getBySlug: (slug: string): Promise<TemplateDetail> =>
    axios.get(`${BASE}/slug/${slug}`).then(r => r.data),

  create: (body: CreateTemplateRequest): Promise<{ templateId: string; slug: string; displayName: string }> =>
    axios.post(BASE, body).then(r => r.data),

  update: (id: string, body: UpdateTemplateRequest): Promise<{ templateId: string; slug: string; displayName: string }> =>
    axios.put(`${BASE}/${id}`, body).then(r => r.data),

  activate: (id: string): Promise<{ templateId: string; slug: string; isActive: boolean }> =>
    axios.post(`${BASE}/${id}/activate`).then(r => r.data),

  deactivate: (id: string): Promise<{ templateId: string; slug: string; isActive: boolean }> =>
    axios.post(`${BASE}/${id}/deactivate`).then(r => r.data),

  scaffold: (id: string, body: ScaffoldRequest): Promise<ScaffoldResult> =>
    axios.post(`${BASE}/${id}/scaffold`, body).then(r => r.data),

  scaffoldBySlug: (slug: string, body: ScaffoldRequest): Promise<ScaffoldResult> =>
    axios.post(`${BASE}/slug/${slug}/scaffold`, body).then(r => r.data),

  generateWithAi: (body: AiScaffoldRequest): Promise<AiScaffoldResult> =>
    axios.post(`${AI_BASE}/scaffold`, body).then(r => r.data),
};
