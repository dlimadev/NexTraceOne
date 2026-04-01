/**
 * Constantes partilhadas do módulo de contratos.
 * Centraliza valores de protocolo, tipo, lifecycle, tokens visuais NTO
 * e navegação do workspace.
 */
import type { ContractProtocol, ContractLifecycleState } from '../types/index';

// ── Contract Types ─────────────────────────────────────────────────────────

export const CONTRACT_TYPES = [
  { value: 'RestApi', labelKey: 'contracts.contractTypes.RestApi', icon: 'Globe' },
  { value: 'Soap', labelKey: 'contracts.contractTypes.Soap', icon: 'Server' },
  { value: 'Event', labelKey: 'contracts.contractTypes.Event', icon: 'Zap' },
  { value: 'KafkaProducer', labelKey: 'contracts.contractTypes.KafkaProducer', icon: 'Radio' },
  { value: 'KafkaConsumer', labelKey: 'contracts.contractTypes.KafkaConsumer', icon: 'Antenna' },
  { value: 'BackgroundService', labelKey: 'contracts.contractTypes.BackgroundService', icon: 'Cog' },
  { value: 'SharedSchema', labelKey: 'contracts.contractTypes.SharedSchema', icon: 'Database' },
] as const;

/** @deprecated Use CONTRACT_TYPES instead. */
export const SERVICE_TYPES = CONTRACT_TYPES;

export type ContractTypeValue = (typeof CONTRACT_TYPES)[number]['value'];

/** @deprecated Use ContractTypeValue instead. */
export type ServiceTypeValue = ContractTypeValue;

// ── Protocols ─────────────────────────────────────────────────────────────

export const PROTOCOLS: readonly ContractProtocol[] = [
  'OpenApi', 'Swagger', 'Wsdl', 'AsyncApi',
];

/** Protocolos reservados — declarados no domínio mas sem implementação completa nesta versão. */
export const RESERVED_PROTOCOLS: readonly ContractProtocol[] = ['Protobuf', 'GraphQl'];

export const PROTOCOL_BY_TYPE: Record<ContractTypeValue, ContractProtocol[]> = {
  RestApi: ['OpenApi', 'Swagger'],
  Soap: ['Wsdl'],
  Event: ['AsyncApi'],
  KafkaProducer: ['AsyncApi'],
  KafkaConsumer: ['AsyncApi'],
  BackgroundService: ['OpenApi'],
  SharedSchema: ['OpenApi'],
};

// ── Lifecycle States ──────────────────────────────────────────────────────

export const LIFECYCLE_STATES: readonly ContractLifecycleState[] = [
  'Draft', 'InReview', 'Approved', 'Locked', 'Deprecated', 'Sunset', 'Retired',
];

export const LIFECYCLE_TRANSITIONS: Record<
  ContractLifecycleState,
  { state: ContractLifecycleState; actionKey: string }[]
> = {
  Draft: [{ state: 'InReview', actionKey: 'contracts.lifecycle.submitForReview' }],
  InReview: [
    { state: 'Approved', actionKey: 'contracts.lifecycle.approve' },
    { state: 'Draft', actionKey: 'contracts.lifecycle.returnToDraft' },
  ],
  Approved: [
    { state: 'Locked', actionKey: 'contracts.lifecycle.lock' },
    { state: 'InReview', actionKey: 'contracts.lifecycle.submitForReview' },
  ],
  Locked: [{ state: 'Deprecated', actionKey: 'contracts.lifecycle.deprecate' }],
  Deprecated: [{ state: 'Sunset', actionKey: 'contracts.lifecycle.sunset' }],
  Sunset: [{ state: 'Retired', actionKey: 'contracts.lifecycle.retire' }],
  Retired: [],
};

// ── Visual Variants — NTO Design Tokens ───────────────────────────────────
// Usa variáveis CSS do design system NTO (index.css) em vez de cores Tailwind arbitrárias.
// Padrão: bg-<token>/opacity text-<token> border border-<token>/opacity

export const PROTOCOL_COLORS: Record<string, string> = {
  OpenApi: 'bg-mint/15 text-mint border border-mint/25',
  Swagger: 'bg-mint/10 text-mint/80 border border-mint/20',
  Wsdl: 'bg-accent/15 text-accent border border-accent/25',
  AsyncApi: 'bg-cyan/15 text-cyan border border-cyan/25',
  Protobuf: 'bg-warning/15 text-warning border border-warning/25',
  GraphQl: 'bg-accent/10 text-accent/80 border border-accent/20',
};

export const LIFECYCLE_COLORS: Record<string, string> = {
  Draft: 'bg-muted/15 text-muted border border-muted/25',
  InReview: 'bg-cyan/15 text-cyan border border-cyan/25',
  Approved: 'bg-mint/15 text-mint border border-mint/25',
  Locked: 'bg-accent/15 text-accent border border-accent/25',
  Deprecated: 'bg-warning/15 text-warning border border-warning/25',
  Sunset: 'bg-danger/15 text-danger border border-danger/25',
  Retired: 'bg-muted/10 text-muted/60 border border-muted/15',
};

export const SERVICE_TYPE_COLORS: Record<string, string> = {
  RestApi: 'bg-mint/15 text-mint border border-mint/25',
  Soap: 'bg-accent/15 text-accent border border-accent/25',
  Event: 'bg-cyan/15 text-cyan border border-cyan/25',
  KafkaProducer: 'bg-cyan/10 text-cyan/80 border border-cyan/20',
  KafkaConsumer: 'bg-accent/10 text-accent/80 border border-accent/20',
  BackgroundService: 'bg-warning/15 text-warning border border-warning/25',
  SharedSchema: 'bg-muted/15 text-muted border border-muted/25',
};

/** Cores para métodos HTTP — usado em builders e operations. */
export const METHOD_COLORS: Record<string, string> = {
  GET: 'bg-mint/15 text-mint border border-mint/25',
  POST: 'bg-cyan/15 text-cyan border border-cyan/25',
  PUT: 'bg-warning/15 text-warning border border-warning/25',
  PATCH: 'bg-accent/15 text-accent border border-accent/25',
  DELETE: 'bg-danger/15 text-danger border border-danger/25',
  OPTIONS: 'bg-muted/15 text-muted border border-muted/25',
  HEAD: 'bg-muted/10 text-muted/60 border border-muted/15',
};

/** Cores para severidades de violações/compliance. */
export const SEVERITY_COLORS: Record<string, string> = {
  Error: 'bg-danger/15 text-danger border border-danger/25',
  Warning: 'bg-warning/15 text-warning border border-warning/25',
  Info: 'bg-cyan/15 text-cyan border border-cyan/25',
  Hint: 'bg-muted/15 text-muted border border-muted/25',
  Blocked: 'bg-danger/20 text-danger border border-danger/30',
};

// ── Workspace Navigation — 16 Sections ────────────────────────────────────

export const WORKSPACE_SECTIONS = [
  { id: 'summary', labelKey: 'contracts.workspace.summary', icon: 'LayoutDashboard', group: 'overview' },
  { id: 'definition', labelKey: 'contracts.workspace.definition', icon: 'FileText', group: 'overview' },

  { id: 'contract', labelKey: 'contracts.workspace.contract', icon: 'Code', group: 'contract' },
  { id: 'operations', labelKey: 'contracts.workspace.operations', icon: 'GitBranch', group: 'contract' },
  { id: 'schemas', labelKey: 'contracts.workspace.schemas', icon: 'Database', group: 'contract' },
  { id: 'security', labelKey: 'contracts.workspace.security', icon: 'Shield', group: 'contract' },

  { id: 'validation', labelKey: 'contracts.workspace.validation', icon: 'ScanSearch', group: 'governance' },
  { id: 'versioning', labelKey: 'contracts.workspace.versioning', icon: 'GitCompare', group: 'governance' },
  { id: 'changelog', labelKey: 'contracts.workspace.changelog', icon: 'History', group: 'governance' },
  { id: 'approvals', labelKey: 'contracts.workspace.approvals', icon: 'CheckCircle', group: 'governance' },
  { id: 'compliance', labelKey: 'contracts.workspace.compliance', icon: 'ShieldCheck', group: 'governance' },
  { id: 'scorecard', labelKey: 'contracts.workspace.scorecard', icon: 'BarChart2', group: 'governance' },

  { id: 'consumers', labelKey: 'contracts.workspace.consumers', icon: 'Users', group: 'relationships' },
  { id: 'dependencies', labelKey: 'contracts.workspace.dependencies', icon: 'Network', group: 'relationships' },
  { id: 'deployments', labelKey: 'contracts.workspace.deployments', icon: 'Rocket', group: 'relationships' },
  { id: 'ai-agents', labelKey: 'contracts.workspace.aiAgents', icon: 'Bot', group: 'ai' },
] as const;

/** Agrupa secções por grupo funcional para rendering do sidebar. */
export const WORKSPACE_SECTION_GROUPS = [
  { key: 'overview', labelKey: 'contracts.workspace.groups.overview' },
  { key: 'contract', labelKey: 'contracts.workspace.groups.contract' },
  { key: 'governance', labelKey: 'contracts.workspace.groups.governance' },
  { key: 'relationships', labelKey: 'contracts.workspace.groups.relationships' },
  { key: 'ai', labelKey: 'contracts.workspace.groups.ai' },
] as const;

// Re-export tipo para conveniência
export type { WorkspaceSectionId } from '../types/workspace';
