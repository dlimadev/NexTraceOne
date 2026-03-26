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
export type NavSection = 'home' | 'services' | 'knowledge' | 'contracts' | 'changes' | 'operations' | 'aiHub' | 'governance' | 'organization' | 'integrations' | 'analytics' | 'admin';

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
  /**
   * Maximum total sidebar nav items shown for this persona (F4-06).
   * Items beyond this limit are hidden; priority follows sectionOrder.
   * undefined = no limit (show all permitted items).
   */
  maxSidebarItems?: number;
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
  /** Indica que a ação aponta para uma capability ainda em preview. */
  preview?: boolean;
}

// ── Configurações por persona ──

const engineerConfig: PersonaConfig = {
  sectionOrder: ['home', 'services', 'operations', 'changes', 'contracts', 'knowledge', 'organization', 'aiHub', 'governance', 'integrations', 'analytics', 'admin'],
  highlightedSections: ['services', 'operations'],
  homeSubtitleKey: 'persona.Engineer.homeSubtitle',
  homeWidgets: [
    { id: 'my-services', titleKey: 'persona.Engineer.widgets.myServices', type: 'services' },
    { id: 'recent-changes', titleKey: 'persona.Engineer.widgets.recentChanges', type: 'changes' },
    { id: 'open-incidents', titleKey: 'persona.Engineer.widgets.openIncidents', type: 'incidents' },
    { id: 'my-contracts', titleKey: 'persona.Engineer.widgets.contracts', type: 'contracts' },
  ],
  quickActions: [
    { id: 'investigate-service', labelKey: 'persona.Engineer.actions.investigateService', icon: 'Search', to: '/services' },
    { id: 'open-runbook', labelKey: 'persona.Engineer.actions.openRunbook', icon: 'FileCode', to: '/operations/runbooks' },
    { id: 'view-contract', labelKey: 'persona.Engineer.actions.viewContract', icon: 'FileText', to: '/contracts' },
    { id: 'analyze-incident', labelKey: 'persona.Engineer.actions.analyzeIncident', icon: 'AlertTriangle', to: '/operations/incidents' },
  ],
  aiContextScopes: ['services', 'contracts', 'incidents', 'runbooks'],
  aiSuggestedPromptKeys: [
    'persona.Engineer.ai.prompt1',
    'persona.Engineer.ai.prompt2',
    'persona.Engineer.ai.prompt3',
  ],
  maxSidebarItems: 15,
};

const techLeadConfig: PersonaConfig = {
  sectionOrder: ['home', 'services', 'changes', 'operations', 'contracts', 'organization', 'knowledge', 'aiHub', 'governance', 'integrations', 'analytics', 'admin'],
  highlightedSections: ['services', 'changes', 'operations'],
  homeSubtitleKey: 'persona.TechLead.homeSubtitle',
  homeWidgets: [
    { id: 'team-services', titleKey: 'persona.TechLead.widgets.teamServices', type: 'services' },
    { id: 'change-risk', titleKey: 'persona.TechLead.widgets.changeRisk', type: 'changes' },
    { id: 'team-reliability', titleKey: 'persona.TechLead.widgets.teamReliability', type: 'reliability' },
    { id: 'team-incidents', titleKey: 'persona.TechLead.widgets.teamIncidents', type: 'incidents' },
    { id: 'ownership-gaps', titleKey: 'persona.TechLead.widgets.ownershipGaps', type: 'ownership' },
  ],
  quickActions: [
    { id: 'review-team-risk', labelKey: 'persona.TechLead.actions.reviewTeamRisk', icon: 'ShieldAlert', to: '/governance/risk' },
    { id: 'inspect-changes', labelKey: 'persona.TechLead.actions.inspectChanges', icon: 'Zap', to: '/changes' },
    { id: 'assign-owner', labelKey: 'persona.TechLead.actions.assignOwner', icon: 'UserCheck', to: '/services' },
    { id: 'open-team-services', labelKey: 'persona.TechLead.actions.openTeamServices', icon: 'Server', to: '/services' },
  ],
  aiContextScopes: ['services', 'changes', 'incidents', 'reliability'],
  aiSuggestedPromptKeys: [
    'persona.TechLead.ai.prompt1',
    'persona.TechLead.ai.prompt2',
    'persona.TechLead.ai.prompt3',
  ],
};

const architectConfig: PersonaConfig = {
  sectionOrder: ['home', 'contracts', 'services', 'knowledge', 'organization', 'changes', 'operations', 'governance', 'integrations', 'aiHub', 'analytics', 'admin'],
  highlightedSections: ['contracts', 'services', 'knowledge'],
  homeSubtitleKey: 'persona.Architect.homeSubtitle',
  homeWidgets: [
    { id: 'dependencies', titleKey: 'persona.Architect.widgets.dependencies', type: 'dependencies' },
    { id: 'contract-consistency', titleKey: 'persona.Architect.widgets.contractConsistency', type: 'contracts' },
    { id: 'arch-risk', titleKey: 'persona.Architect.widgets.architecturalRisk', type: 'risk' },
    { id: 'cross-team-changes', titleKey: 'persona.Architect.widgets.crossTeamChanges', type: 'changes' },
  ],
  quickActions: [
    { id: 'inspect-deps', labelKey: 'persona.Architect.actions.inspectDependencyMap', icon: 'Share2', to: '/services/graph' },
    { id: 'review-cross-impact', labelKey: 'persona.Architect.actions.reviewCrossImpact', icon: 'Zap', to: '/changes' },
    { id: 'analyze-contracts', labelKey: 'persona.Architect.actions.analyzeContractCompatibility', icon: 'FileText', to: '/contracts' },
    { id: 'source-of-truth', labelKey: 'persona.Architect.actions.sourceOfTruth', icon: 'Globe', to: '/source-of-truth' },
  ],
  aiContextScopes: ['contracts', 'services', 'dependencies', 'changes'],
  aiSuggestedPromptKeys: [
    'persona.Architect.ai.prompt1',
    'persona.Architect.ai.prompt2',
    'persona.Architect.ai.prompt3',
  ],
};

const productConfig: PersonaConfig = {
  sectionOrder: ['home', 'analytics', 'changes', 'services', 'operations', 'organization', 'governance', 'contracts', 'knowledge', 'aiHub', 'integrations', 'admin'],
  highlightedSections: ['changes', 'services', 'analytics'],
  homeSubtitleKey: 'persona.Product.homeSubtitle',
  homeWidgets: [
    { id: 'release-confidence', titleKey: 'persona.Product.widgets.releaseConfidence', type: 'releaseConfidence' },
    { id: 'critical-services', titleKey: 'persona.Product.widgets.criticalServices', type: 'services' },
    { id: 'operational-risk', titleKey: 'persona.Product.widgets.operationalRisk', type: 'risk' },
    { id: 'recent-incidents', titleKey: 'persona.Product.widgets.recentIncidents', type: 'incidents' },
  ],
  quickActions: [
    { id: 'review-release', labelKey: 'persona.Product.actions.reviewReleaseConfidence', icon: 'ShieldCheck', to: '/changes' },
    { id: 'critical-status', labelKey: 'persona.Product.actions.viewCriticalServiceStatus', icon: 'Activity', to: '/operations/reliability' },
    { id: 'product-incidents', labelKey: 'persona.Product.actions.inspectProductIncidents', icon: 'AlertTriangle', to: '/operations/incidents' },
    { id: 'view-reports', labelKey: 'persona.Product.actions.viewReports', icon: 'BarChart3', to: '/governance/reports' },
  ],
  aiContextScopes: ['services', 'changes', 'incidents'],
  aiSuggestedPromptKeys: [
    'persona.Product.ai.prompt1',
    'persona.Product.ai.prompt2',
    'persona.Product.ai.prompt3',
  ],
};

const executiveConfig: PersonaConfig = {
  sectionOrder: ['home', 'governance', 'analytics', 'organization', 'changes', 'services', 'operations', 'contracts', 'knowledge', 'aiHub', 'integrations', 'admin'],
  highlightedSections: ['governance', 'analytics'],
  homeSubtitleKey: 'persona.Executive.homeSubtitle',
  homeWidgets: [
    { id: 'operational-trend', titleKey: 'persona.Executive.widgets.operationalTrend', type: 'trend' },
    { id: 'critical-domains', titleKey: 'persona.Executive.widgets.criticalDomains', type: 'risk' },
    { id: 'risk-overview', titleKey: 'persona.Executive.widgets.riskOverview', type: 'risk', variant: 'expanded' },
    { id: 'confidence-indicators', titleKey: 'persona.Executive.widgets.confidenceIndicators', type: 'releaseConfidence' },
  ],
  quickActions: [
    { id: 'executive-overview', labelKey: 'persona.Executive.actions.openExecutiveOverview', icon: 'BarChart3', to: '/governance/reports' },
    { id: 'operational-trend', labelKey: 'persona.Executive.actions.reviewOperationalTrend', icon: 'Activity', to: '/governance/risk' },
    { id: 'inspect-domains', labelKey: 'persona.Executive.actions.inspectCriticalDomains', icon: 'Server', to: '/services' },
    { id: 'view-compliance', labelKey: 'persona.Executive.actions.viewCompliance', icon: 'Scale', to: '/governance/compliance' },
  ],
  aiContextScopes: ['risk', 'trends', 'services'],
  aiSuggestedPromptKeys: [
    'persona.Executive.ai.prompt1',
    'persona.Executive.ai.prompt2',
    'persona.Executive.ai.prompt3',
  ],
  maxSidebarItems: 6,
};

const platformAdminConfig: PersonaConfig = {
  sectionOrder: ['home', 'admin', 'organization', 'integrations', 'aiHub', 'governance', 'analytics', 'services', 'contracts', 'knowledge', 'changes', 'operations'],
  highlightedSections: ['admin', 'aiHub', 'governance', 'integrations'],
  homeSubtitleKey: 'persona.PlatformAdmin.homeSubtitle',
  homeWidgets: [
    { id: 'policy-health', titleKey: 'persona.PlatformAdmin.widgets.policyHealth', type: 'governance' },
    { id: 'platform-coverage', titleKey: 'persona.PlatformAdmin.widgets.platformCoverage', type: 'governance' },
    { id: 'ai-governance', titleKey: 'persona.PlatformAdmin.widgets.aiGovernance', type: 'aiInsights' },
    { id: 'integrations', titleKey: 'persona.PlatformAdmin.widgets.integrations', type: 'governance' },
  ],
  quickActions: [
    { id: 'manage-policies', labelKey: 'persona.PlatformAdmin.actions.managePolicies', icon: 'ShieldCheck', to: '/ai/policies' },
    { id: 'manage-models', labelKey: 'persona.PlatformAdmin.actions.manageAiModels', icon: 'Database', to: '/ai/models' },
    { id: 'configure-integrations', labelKey: 'persona.PlatformAdmin.actions.configureIntegrations', icon: 'Settings', to: '/users' },
    { id: 'review-coverage', labelKey: 'persona.PlatformAdmin.actions.reviewPlatformCoverage', icon: 'BarChart3', to: '/governance/reports' },
  ],
  aiContextScopes: ['governance', 'policies', 'models'],
  aiSuggestedPromptKeys: [
    'persona.PlatformAdmin.ai.prompt1',
    'persona.PlatformAdmin.ai.prompt2',
    'persona.PlatformAdmin.ai.prompt3',
  ],
};

const auditorConfig: PersonaConfig = {
  sectionOrder: ['home', 'governance', 'organization', 'admin', 'changes', 'operations', 'aiHub', 'analytics', 'services', 'contracts', 'knowledge', 'integrations'],
  highlightedSections: ['governance', 'admin'],
  homeSubtitleKey: 'persona.Auditor.homeSubtitle',
  homeWidgets: [
    { id: 'audit-activity', titleKey: 'persona.Auditor.widgets.auditActivity', type: 'audit' },
    { id: 'approval-traceability', titleKey: 'persona.Auditor.widgets.approvalTraceability', type: 'audit' },
    { id: 'evidence-exports', titleKey: 'persona.Auditor.widgets.evidenceExports', type: 'audit' },
    { id: 'ai-usage-audit', titleKey: 'persona.Auditor.widgets.aiUsageAudit', type: 'aiInsights' },
  ],
  quickActions: [
    { id: 'inspect-audit', labelKey: 'persona.Auditor.actions.inspectAuditTrail', icon: 'ClipboardList', to: '/audit' },
    { id: 'export-evidence', labelKey: 'persona.Auditor.actions.exportEvidence', icon: 'Download', to: '/audit' },
    { id: 'review-approvals', labelKey: 'persona.Auditor.actions.reviewApprovalHistory', icon: 'ClipboardCheck', to: '/governance/compliance' },
    { id: 'ai-audit', labelKey: 'persona.Auditor.actions.auditAiUsage', icon: 'Bot', to: '/ai/policies' },
  ],
  aiContextScopes: ['audit', 'compliance', 'approvals'],
  aiSuggestedPromptKeys: [
    'persona.Auditor.ai.prompt1',
    'persona.Auditor.ai.prompt2',
    'persona.Auditor.ai.prompt3',
  ],
  maxSidebarItems: 8,
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
