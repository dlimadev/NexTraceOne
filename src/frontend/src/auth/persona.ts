/**
 * Sistema de personas do NexTraceOne.
 *
 * As personas representam o perfil funcional do utilizador — não se limitam ao cargo literal.
 * Cada persona define a experiência padrão do produto: Home, navegação, quick actions,
 * linguagem, profundidade técnica e contexto da IA.
 *
 * A derivação da persona é feita a partir do `roleName` retornado pelo backend.
 * Se o backend adicionar campo `persona` no futuro, esta derivação pode ser simplificada.
 *
 * @see docs/PERSONA-MATRIX.md
 * @see docs/PERSONA-UX-MAPPING.md
 */

import type { AppRole } from './permissions';

// ── Personas oficiais do NexTraceOne ──

export type Persona =
  | 'Engineer'
  | 'TechLead'
  | 'Architect'
  | 'Product'
  | 'Executive'
  | 'PlatformAdmin'
  | 'Auditor';

/**
 * Mapeia o roleName do backend para a persona do produto.
 *
 * O mapeamento é feito por convenção; se o backend evoluir para enviar persona
 * directamente, esta função pode ser simplificada.
 */
export function derivePersona(roleName: string): Persona {
  const mapping: Record<AppRole, Persona> = {
    PlatformAdmin: 'PlatformAdmin',
    TechLead: 'TechLead',
    Developer: 'Engineer',
    Viewer: 'Product',
    Auditor: 'Auditor',
    SecurityReview: 'Architect',
    ApprovalOnly: 'Executive',
  };
  return mapping[roleName as AppRole] ?? 'Engineer';
}

// ── Configuração UX por persona ──

/**
 * Define a ordem de prioridade das secções da sidebar para cada persona.
 * As secções listadas primeiro aparecem no topo da navegação.
 */
export type NavSection = 'home' | 'services' | 'knowledge' | 'contracts' | 'changes' | 'operations' | 'aiHub' | 'governance' | 'admin';

export interface PersonaConfig {
  /** Secções da sidebar na ordem de prioridade para esta persona. */
  sectionOrder: NavSection[];
  /** Secções destacadas visualmente (bold/accent). */
  highlightedSections: NavSection[];
  /** Chave i18n do subtítulo da Home. */
  homeSubtitleKey: string;
  /** Widgets da Home na ordem desejada. */
  homeWidgets: HomeWidget[];
  /** Quick actions visíveis na Home. */
  quickActions: QuickAction[];
  /** Contextos padrão da IA para esta persona. */
  aiContextScopes: string[];
  /** Prompts sugeridos na IA para esta persona. */
  aiSuggestedPromptKeys: string[];
}

export interface HomeWidget {
  /** Identificador único do widget. */
  id: string;
  /** Chave i18n do título. */
  titleKey: string;
  /** Tipo de widget para renderização. */
  type: 'services' | 'changes' | 'incidents' | 'contracts' | 'reliability'
    | 'dependencies' | 'risk' | 'trend' | 'governance' | 'audit' | 'ownership'
    | 'releaseConfidence' | 'aiInsights';
  /** Variante visual: compact, standard, expanded. */
  variant?: 'compact' | 'standard' | 'expanded';
}

export interface QuickAction {
  /** Identificador único. */
  id: string;
  /** Chave i18n do label. */
  labelKey: string;
  /** Ícone identificador (lucide-react icon name). */
  icon: string;
  /** Rota de destino. */
  to: string;
}

// ── Configurações por persona ──

const engineerConfig: PersonaConfig = {
  sectionOrder: ['home', 'services', 'operations', 'changes', 'contracts', 'knowledge', 'aiHub', 'governance', 'admin'],
  highlightedSections: ['services', 'operations'],
  homeSubtitleKey: 'persona.engineer.homeSubtitle',
  homeWidgets: [
    { id: 'my-services', titleKey: 'persona.engineer.widgets.myServices', type: 'services' },
    { id: 'recent-changes', titleKey: 'persona.engineer.widgets.recentChanges', type: 'changes' },
    { id: 'open-incidents', titleKey: 'persona.engineer.widgets.openIncidents', type: 'incidents' },
    { id: 'my-contracts', titleKey: 'persona.engineer.widgets.contracts', type: 'contracts' },
  ],
  quickActions: [
    { id: 'investigate-service', labelKey: 'persona.engineer.actions.investigateService', icon: 'Search', to: '/services' },
    { id: 'open-runbook', labelKey: 'persona.engineer.actions.openRunbook', icon: 'FileCode', to: '/operations/runbooks' },
    { id: 'view-contract', labelKey: 'persona.engineer.actions.viewContract', icon: 'FileText', to: '/contracts' },
    { id: 'analyze-incident', labelKey: 'persona.engineer.actions.analyzeIncident', icon: 'AlertTriangle', to: '/operations/incidents' },
  ],
  aiContextScopes: ['services', 'contracts', 'incidents', 'runbooks'],
  aiSuggestedPromptKeys: [
    'persona.engineer.ai.prompt1',
    'persona.engineer.ai.prompt2',
    'persona.engineer.ai.prompt3',
  ],
};

const techLeadConfig: PersonaConfig = {
  sectionOrder: ['home', 'services', 'changes', 'operations', 'contracts', 'knowledge', 'aiHub', 'governance', 'admin'],
  highlightedSections: ['services', 'changes', 'operations'],
  homeSubtitleKey: 'persona.techLead.homeSubtitle',
  homeWidgets: [
    { id: 'team-services', titleKey: 'persona.techLead.widgets.teamServices', type: 'services' },
    { id: 'change-risk', titleKey: 'persona.techLead.widgets.changeRisk', type: 'changes' },
    { id: 'team-reliability', titleKey: 'persona.techLead.widgets.teamReliability', type: 'reliability' },
    { id: 'team-incidents', titleKey: 'persona.techLead.widgets.teamIncidents', type: 'incidents' },
    { id: 'ownership-gaps', titleKey: 'persona.techLead.widgets.ownershipGaps', type: 'ownership' },
  ],
  quickActions: [
    { id: 'review-team-risk', labelKey: 'persona.techLead.actions.reviewTeamRisk', icon: 'ShieldAlert', to: '/governance/risk' },
    { id: 'inspect-changes', labelKey: 'persona.techLead.actions.inspectChanges', icon: 'Zap', to: '/changes' },
    { id: 'assign-owner', labelKey: 'persona.techLead.actions.assignOwner', icon: 'UserCheck', to: '/services' },
    { id: 'open-team-services', labelKey: 'persona.techLead.actions.openTeamServices', icon: 'Server', to: '/services' },
  ],
  aiContextScopes: ['services', 'changes', 'incidents', 'reliability'],
  aiSuggestedPromptKeys: [
    'persona.techLead.ai.prompt1',
    'persona.techLead.ai.prompt2',
    'persona.techLead.ai.prompt3',
  ],
};

const architectConfig: PersonaConfig = {
  sectionOrder: ['home', 'contracts', 'services', 'knowledge', 'changes', 'operations', 'governance', 'aiHub', 'admin'],
  highlightedSections: ['contracts', 'services', 'knowledge'],
  homeSubtitleKey: 'persona.architect.homeSubtitle',
  homeWidgets: [
    { id: 'dependencies', titleKey: 'persona.architect.widgets.dependencies', type: 'dependencies' },
    { id: 'contract-consistency', titleKey: 'persona.architect.widgets.contractConsistency', type: 'contracts' },
    { id: 'arch-risk', titleKey: 'persona.architect.widgets.architecturalRisk', type: 'risk' },
    { id: 'cross-team-changes', titleKey: 'persona.architect.widgets.crossTeamChanges', type: 'changes' },
  ],
  quickActions: [
    { id: 'inspect-deps', labelKey: 'persona.architect.actions.inspectDependencyMap', icon: 'Share2', to: '/services/graph' },
    { id: 'review-cross-impact', labelKey: 'persona.architect.actions.reviewCrossImpact', icon: 'Zap', to: '/changes' },
    { id: 'analyze-contracts', labelKey: 'persona.architect.actions.analyzeContractCompatibility', icon: 'FileText', to: '/contracts' },
    { id: 'source-of-truth', labelKey: 'persona.architect.actions.sourceOfTruth', icon: 'Globe', to: '/source-of-truth' },
  ],
  aiContextScopes: ['contracts', 'services', 'dependencies', 'changes'],
  aiSuggestedPromptKeys: [
    'persona.architect.ai.prompt1',
    'persona.architect.ai.prompt2',
    'persona.architect.ai.prompt3',
  ],
};

const productConfig: PersonaConfig = {
  sectionOrder: ['home', 'changes', 'services', 'operations', 'governance', 'contracts', 'knowledge', 'aiHub', 'admin'],
  highlightedSections: ['changes', 'services'],
  homeSubtitleKey: 'persona.product.homeSubtitle',
  homeWidgets: [
    { id: 'release-confidence', titleKey: 'persona.product.widgets.releaseConfidence', type: 'releaseConfidence' },
    { id: 'critical-services', titleKey: 'persona.product.widgets.criticalServices', type: 'services' },
    { id: 'operational-risk', titleKey: 'persona.product.widgets.operationalRisk', type: 'risk' },
    { id: 'recent-incidents', titleKey: 'persona.product.widgets.recentIncidents', type: 'incidents' },
  ],
  quickActions: [
    { id: 'review-release', labelKey: 'persona.product.actions.reviewReleaseConfidence', icon: 'ShieldCheck', to: '/changes' },
    { id: 'critical-status', labelKey: 'persona.product.actions.viewCriticalServiceStatus', icon: 'Activity', to: '/operations/reliability' },
    { id: 'product-incidents', labelKey: 'persona.product.actions.inspectProductIncidents', icon: 'AlertTriangle', to: '/operations/incidents' },
    { id: 'view-reports', labelKey: 'persona.product.actions.viewReports', icon: 'BarChart3', to: '/governance/reports' },
  ],
  aiContextScopes: ['services', 'changes', 'incidents'],
  aiSuggestedPromptKeys: [
    'persona.product.ai.prompt1',
    'persona.product.ai.prompt2',
    'persona.product.ai.prompt3',
  ],
};

const executiveConfig: PersonaConfig = {
  sectionOrder: ['home', 'governance', 'changes', 'services', 'operations', 'contracts', 'knowledge', 'aiHub', 'admin'],
  highlightedSections: ['governance'],
  homeSubtitleKey: 'persona.executive.homeSubtitle',
  homeWidgets: [
    { id: 'operational-trend', titleKey: 'persona.executive.widgets.operationalTrend', type: 'trend' },
    { id: 'critical-domains', titleKey: 'persona.executive.widgets.criticalDomains', type: 'risk' },
    { id: 'risk-overview', titleKey: 'persona.executive.widgets.riskOverview', type: 'risk', variant: 'expanded' },
    { id: 'confidence-indicators', titleKey: 'persona.executive.widgets.confidenceIndicators', type: 'releaseConfidence' },
  ],
  quickActions: [
    { id: 'executive-overview', labelKey: 'persona.executive.actions.openExecutiveOverview', icon: 'BarChart3', to: '/governance/reports' },
    { id: 'operational-trend', labelKey: 'persona.executive.actions.reviewOperationalTrend', icon: 'Activity', to: '/governance/risk' },
    { id: 'inspect-domains', labelKey: 'persona.executive.actions.inspectCriticalDomains', icon: 'Server', to: '/services' },
    { id: 'view-compliance', labelKey: 'persona.executive.actions.viewCompliance', icon: 'Scale', to: '/governance/compliance' },
  ],
  aiContextScopes: ['risk', 'trends', 'services'],
  aiSuggestedPromptKeys: [
    'persona.executive.ai.prompt1',
    'persona.executive.ai.prompt2',
    'persona.executive.ai.prompt3',
  ],
};

const platformAdminConfig: PersonaConfig = {
  sectionOrder: ['home', 'admin', 'aiHub', 'governance', 'services', 'contracts', 'knowledge', 'changes', 'operations'],
  highlightedSections: ['admin', 'aiHub', 'governance'],
  homeSubtitleKey: 'persona.platformAdmin.homeSubtitle',
  homeWidgets: [
    { id: 'policy-health', titleKey: 'persona.platformAdmin.widgets.policyHealth', type: 'governance' },
    { id: 'platform-coverage', titleKey: 'persona.platformAdmin.widgets.platformCoverage', type: 'governance' },
    { id: 'ai-governance', titleKey: 'persona.platformAdmin.widgets.aiGovernance', type: 'aiInsights' },
    { id: 'integrations', titleKey: 'persona.platformAdmin.widgets.integrations', type: 'governance' },
  ],
  quickActions: [
    { id: 'manage-policies', labelKey: 'persona.platformAdmin.actions.managePolicies', icon: 'ShieldCheck', to: '/ai/policies' },
    { id: 'manage-models', labelKey: 'persona.platformAdmin.actions.manageAiModels', icon: 'Database', to: '/ai/models' },
    { id: 'configure-integrations', labelKey: 'persona.platformAdmin.actions.configureIntegrations', icon: 'Settings', to: '/users' },
    { id: 'review-coverage', labelKey: 'persona.platformAdmin.actions.reviewPlatformCoverage', icon: 'BarChart3', to: '/governance/reports' },
  ],
  aiContextScopes: ['governance', 'policies', 'models'],
  aiSuggestedPromptKeys: [
    'persona.platformAdmin.ai.prompt1',
    'persona.platformAdmin.ai.prompt2',
    'persona.platformAdmin.ai.prompt3',
  ],
};

const auditorConfig: PersonaConfig = {
  sectionOrder: ['home', 'governance', 'admin', 'changes', 'operations', 'aiHub', 'services', 'contracts', 'knowledge'],
  highlightedSections: ['governance', 'admin'],
  homeSubtitleKey: 'persona.auditor.homeSubtitle',
  homeWidgets: [
    { id: 'audit-activity', titleKey: 'persona.auditor.widgets.auditActivity', type: 'audit' },
    { id: 'approval-traceability', titleKey: 'persona.auditor.widgets.approvalTraceability', type: 'audit' },
    { id: 'evidence-exports', titleKey: 'persona.auditor.widgets.evidenceExports', type: 'audit' },
    { id: 'ai-usage-audit', titleKey: 'persona.auditor.widgets.aiUsageAudit', type: 'aiInsights' },
  ],
  quickActions: [
    { id: 'inspect-audit', labelKey: 'persona.auditor.actions.inspectAuditTrail', icon: 'ClipboardList', to: '/audit' },
    { id: 'export-evidence', labelKey: 'persona.auditor.actions.exportEvidence', icon: 'Download', to: '/audit' },
    { id: 'review-approvals', labelKey: 'persona.auditor.actions.reviewApprovalHistory', icon: 'ClipboardCheck', to: '/governance/compliance' },
    { id: 'ai-audit', labelKey: 'persona.auditor.actions.auditAiUsage', icon: 'Bot', to: '/ai/policies' },
  ],
  aiContextScopes: ['audit', 'compliance', 'approvals'],
  aiSuggestedPromptKeys: [
    'persona.auditor.ai.prompt1',
    'persona.auditor.ai.prompt2',
    'persona.auditor.ai.prompt3',
  ],
};

/**
 * Registry central de configuração por persona.
 * Extensível para novas personas sem alterar a lógica dos componentes consumidores.
 */
export const personaConfigs: Record<Persona, PersonaConfig> = {
  Engineer: engineerConfig,
  TechLead: techLeadConfig,
  Architect: architectConfig,
  Product: productConfig,
  Executive: executiveConfig,
  PlatformAdmin: platformAdminConfig,
  Auditor: auditorConfig,
};
