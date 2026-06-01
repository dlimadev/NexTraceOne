import { useState, useEffect, useMemo } from 'react';
import { useLocation, NavLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { cn } from '../../lib/cn';
import { usePermissions } from '../../hooks/usePermissions';
import { usePersona } from '../../contexts/PersonaContext';
import type { Permission } from '../../auth/permissions';
import type { NavSection } from '../../auth/persona';
import { SIDEBAR_RAIL_WIDTH, SIDEBAR_CONTENT_WIDTH, SIDEBAR_WIDTH_COLLAPSED, SIDEBAR_WIDTH_EXPANDED } from './constants';
import { useNavCounters } from '../../hooks/useNavCounters';
import {
  LayoutDashboard, FileText, Zap, Users, Users2, CheckSquare, ArrowUpCircle,
  AlertTriangle, Clock, UserCheck,
  ClipboardCheck, Monitor, Bot,
  Share2, Server,
  Settings, SlidersHorizontal,
  PanelLeftClose, PanelLeftOpen,
  Cable, TrendingUp, BookOpen, Briefcase,
  Workflow, StickyNote, BookMarked, Radar,
  CalendarDays, Award, BrainCircuit, Palette, Cpu,
  Archive, HardDrive, Gauge, Bell, RotateCcw, Leaf, Lock,
  Train, MapPin, GitCommit, Target, Download, Sliders, MessageSquare, Eye, BookText,
  PackageCheck, GitMerge, History, Building2,
  Bug, Flame, TrendingDown, Store, FileSearch, Lightbulb,
  HeartPulse, ArrowRightLeft, FlaskConical, Star, MonitorDot,
  Waypoints, LineChart, Stethoscope, PieChart, Diff,
  ListChecks, FileLock2, KeyRound, ShieldAlert, BookOpenCheck,
  Layers, ScanEye, Scale, DoorOpen, Tag, PhoneCall, Send,
  // Wave 1 — icon revision
  Boxes, Sparkles, Globe, LayoutGrid, ScrollText, GitBranch, Activity,
  // Wave 2 — new nav items (confirmed available in lucide-react ^0.577)
  FileCode, BarChart3, BarChart2, Database, Brain,
  ShieldCheck, Package, Edit3, Coins, Code2, Map,
  Fingerprint, Network, Shield, Settings2,
} from 'lucide-react';

interface NavItem {
  labelKey: string;
  to: string;
  icon: React.ReactNode;
  permission?: Permission;
  section: NavSection;
  /** Sub-group label key for visual separation within the same section. */
  subGroup?: string;
  /** Módulo em preview — não homologável. Mostra badge e estilo atenuado. */
  preview?: boolean;
}

const navItems: NavItem[] = [
  // ── HOME ──────────────────────────────────────────────────────────────────
  { labelKey: 'sidebar.dashboard', to: '/', icon: <LayoutDashboard size={18} />, section: 'home' },

  // ── SERVIÇOS ──────────────────────────────────────────────────────────────
  { labelKey: 'sidebar.serviceCatalog', to: '/services', icon: <Server size={18} />, permission: 'catalog:assets:read', section: 'services' },
  { labelKey: 'sidebar.dependencyGraph', to: '/services/graph', icon: <Share2 size={18} />, permission: 'catalog:assets:read', section: 'services' },
  { labelKey: 'sidebar.serviceDiscovery', to: '/services/discovery', icon: <Radar size={18} />, permission: 'catalog:assets:read', section: 'services' },
  { labelKey: 'sidebar.scoreMaturity', to: '/services/maturity', icon: <Award size={18} />, permission: 'catalog:assets:read', section: 'services' },
  { labelKey: 'sidebar.developerPortal', to: '/portal', icon: <BookMarked size={18} />, permission: 'developer-portal:read', section: 'services' },
  { labelKey: 'sidebar.sourceOfTruth', to: '/source-of-truth', icon: <BookOpenCheck size={18} />, permission: 'catalog:assets:read', section: 'services' },
  { labelKey: 'sidebar.developerExperienceScore', to: '/catalog/developer-experience-score', icon: <Star size={18} />, permission: 'catalog:assets:read', section: 'services' },
  { labelKey: 'sidebar.knowledgeHub', to: '/knowledge', icon: <BookOpen size={18} />, permission: 'catalog:assets:read', section: 'services' },
  { labelKey: 'sidebar.featureFlags', to: '/services/feature-flags', icon: <Sliders size={18} />, permission: 'catalog:assets:read', section: 'services' },
  { labelKey: 'sidebar.legacyAssets', to: '/services/legacy', icon: <Archive size={18} />, permission: 'catalog:assets:read', section: 'services' },

  // ── CONTRATOS ─────────────────────────────────────────────────────────────
  { labelKey: 'sidebar.contractCatalog', to: '/contracts', icon: <FileText size={18} />, permission: 'contracts:read', section: 'contracts' },
  { labelKey: 'sidebar.contractPipeline', to: '/catalog/contracts/pipeline', icon: <GitBranch size={18} />, permission: 'catalog:contracts:pipeline:read', section: 'contracts' },
  { labelKey: 'sidebar.contractsHealth', to: '/contracts/health', icon: <Stethoscope size={18} />, permission: 'contracts:read', section: 'contracts' },
  { labelKey: 'sidebar.cdct', to: '/contracts/cdct', icon: <FlaskConical size={18} />, permission: 'contracts:read', section: 'contracts' },
  { labelKey: 'sidebar.spectralRulesets', to: '/contracts/spectral', icon: <ListChecks size={18} />, permission: 'rulesets:read', section: 'contracts' },
  { labelKey: 'sidebar.canonicalEntities', to: '/contracts/canonical', icon: <Boxes size={18} />, permission: 'contracts:read', section: 'contracts' },
  { labelKey: 'sidebar.publicationCenter', to: '/contracts/publication', icon: <Send size={18} />, permission: 'contracts:write', section: 'contracts' },

  // ── MUDANÇAS ──────────────────────────────────────────────────────────────
  { labelKey: 'sidebar.changes', to: '/changes', icon: <Diff size={18} />, permission: 'change-intelligence:read', section: 'changes' },
  { labelKey: 'sidebar.releases', to: '/releases', icon: <Tag size={18} />, permission: 'change-intelligence:read', section: 'changes' },
  { labelKey: 'sidebar.releaseCalendar', to: '/release-calendar', icon: <CalendarDays size={18} />, permission: 'change-intelligence:read', section: 'changes' },
  { labelKey: 'sidebar.doraMetrics', to: '/dora-metrics', icon: <LineChart size={18} />, permission: 'change-intelligence:read', section: 'changes' },
  // Ciclo de Vida do Release
  { labelKey: 'sidebar.releaseTrain', to: '/release-train', icon: <Train size={18} />, permission: 'change-intelligence:read', section: 'changes', subGroup: 'sidebar.subGroupReleaseDeeperNav' },
  { labelKey: 'sidebar.releaseCommitPool', to: '/releases/commit-pool', icon: <GitCommit size={18} />, permission: 'change-intelligence:read', section: 'changes', subGroup: 'sidebar.subGroupReleaseDeeperNav' },
  { labelKey: 'sidebar.releaseImpactReport', to: '/releases/impact-report', icon: <BarChart3 size={18} />, permission: 'change-intelligence:read', section: 'changes', subGroup: 'sidebar.subGroupReleaseDeeperNav' },
  { labelKey: 'sidebar.releaseNotes', to: '/releases/notes', icon: <BookOpen size={18} />, permission: 'change-intelligence:read', section: 'changes', subGroup: 'sidebar.subGroupReleaseDeeperNav' },
  { labelKey: 'sidebar.postReleaseReview', to: '/releases/post-review', icon: <PackageCheck size={18} />, permission: 'change-intelligence:read', section: 'changes', subGroup: 'sidebar.subGroupReleaseDeeperNav' },
  { labelKey: 'sidebar.releaseExternalIngest', to: '/releases/ingest-external', icon: <Download size={18} />, permission: 'change-intelligence:write', section: 'changes', subGroup: 'sidebar.subGroupReleaseDeeperNav' },
  { labelKey: 'sidebar.releaseParameterAudit', to: '/releases/parameter-audit', icon: <History size={18} />, permission: 'change-intelligence:read', section: 'changes', subGroup: 'sidebar.subGroupReleaseDeeperNav' },
  { labelKey: 'sidebar.evidencePackViewer', to: '/releases/evidence-pack', icon: <GitBranch size={18} />, permission: 'workflow:instances:read', section: 'changes', subGroup: 'sidebar.subGroupReleaseDeeperNav' },
  // Aprovação e Governança
  { labelKey: 'sidebar.promotion', to: '/promotion', icon: <ArrowUpCircle size={18} />, permission: 'promotion:requests:read', section: 'changes', subGroup: 'sidebar.subGroupApprovalGovernance' },
  { labelKey: 'sidebar.workflow', to: '/workflow', icon: <CheckSquare size={18} />, permission: 'workflow:instances:read', section: 'changes', subGroup: 'sidebar.subGroupApprovalGovernance' },
  { labelKey: 'sidebar.workflowConfiguration', to: '/workflow/configuration', icon: <SlidersHorizontal size={18} />, permission: 'workflow:instances:write', section: 'changes', subGroup: 'sidebar.subGroupApprovalGovernance' },
  { labelKey: 'sidebar.releaseApprovalGateway', to: '/releases/approval-gateway', icon: <DoorOpen size={18} />, permission: 'change-intelligence:write', section: 'changes', subGroup: 'sidebar.subGroupApprovalGovernance' },
  { labelKey: 'sidebar.releaseApprovalPolicies', to: '/releases/approval-policies', icon: <FileLock2 size={18} />, permission: 'change-intelligence:write', section: 'changes', subGroup: 'sidebar.subGroupApprovalGovernance' },
  { labelKey: 'sidebar.releaseControlParameters', to: '/releases/control-parameters', icon: <Sliders size={18} />, permission: 'change-intelligence:write', section: 'changes', subGroup: 'sidebar.subGroupApprovalGovernance' },
  // Risco e Rollback
  { labelKey: 'sidebar.releaseGatesDashboard', to: '/releases/gates', icon: <GitMerge size={18} />, permission: 'change-intelligence:read', section: 'changes', subGroup: 'sidebar.subGroupRiskRollback' },
  { labelKey: 'sidebar.releaseRollback', to: '/releases/rollback', icon: <RotateCcw size={18} />, permission: 'change-intelligence:write', section: 'changes', subGroup: 'sidebar.subGroupRiskRollback' },

  // ── OPERAÇÕES ─────────────────────────────────────────────────────────────
  { labelKey: 'sidebar.incidents', to: '/operations/incidents', icon: <AlertTriangle size={18} />, permission: 'operations:incidents:read', section: 'operations' },
  { labelKey: 'sidebar.runbooks', to: '/operations/runbooks', icon: <ScrollText size={18} />, permission: 'operations:runbooks:read', section: 'operations' },
  { labelKey: 'sidebar.reliabilityAndSlos', to: '/operations/reliability', icon: <HeartPulse size={18} />, permission: 'operations:reliability:read', section: 'operations' },
  { labelKey: 'sidebar.sloManagement', to: '/operations/reliability/slos', icon: <Target size={18} />, permission: 'operations:reliability:read', section: 'operations' },
  { labelKey: 'sidebar.onCallSchedule', to: '/operations/on-call-schedule', icon: <PhoneCall size={18} />, permission: 'operations:incidents:read', section: 'operations' },
  { labelKey: 'sidebar.onCallIntelligence', to: '/operations/on-call-intelligence', icon: <Brain size={18} />, permission: 'operations:incidents:read', section: 'operations' },
  { labelKey: 'sidebar.postIncident', to: '/operations/post-incident', icon: <FileSearch size={18} />, permission: 'operations:incidents:read', section: 'operations' },
  // Telemetria
  { labelKey: 'sidebar.requestExplorer', to: '/operations/request-explorer', icon: <ArrowRightLeft size={18} />, permission: 'operations:telemetry:read', section: 'operations', subGroup: 'sidebar.subGroupTelemetry' },
  { labelKey: 'sidebar.traceExplorer', to: '/operations/telemetry/traces', icon: <Waypoints size={18} />, permission: 'operations:telemetry:read', section: 'operations', subGroup: 'sidebar.subGroupTelemetry' },
  { labelKey: 'sidebar.logExplorer', to: '/operations/telemetry/logs', icon: <FileText size={18} />, permission: 'operations:telemetry:read', section: 'operations', subGroup: 'sidebar.subGroupTelemetry' },
  { labelKey: 'sidebar.profilingExplorer', to: '/operations/profiling-explorer', icon: <BarChart2 size={18} />, permission: 'operations:telemetry:read', section: 'operations', subGroup: 'sidebar.subGroupTelemetry' },
  { labelKey: 'sidebar.dbExplorer', to: '/operations/db-explorer', icon: <Database size={18} />, permission: 'operations:telemetry:read', section: 'operations', subGroup: 'sidebar.subGroupTelemetry' },
  { labelKey: 'sidebar.errorTracking', to: '/operations/error-tracking', icon: <Bug size={18} />, permission: 'operations:incidents:read', section: 'operations', subGroup: 'sidebar.subGroupTelemetry' },
  // SRE & Inteligência
  { labelKey: 'sidebar.sreDashboard', to: '/operations/sre-dashboard', icon: <Monitor size={18} />, permission: 'operations:reliability:read', section: 'operations', subGroup: 'sidebar.subGroupSreIntelligence' },
  { labelKey: 'sidebar.syntheticMonitoring', to: '/operations/synthetic-monitoring', icon: <Gauge size={18} />, permission: 'operations:reliability:read', section: 'operations', subGroup: 'sidebar.subGroupSreIntelligence' },
  { labelKey: 'sidebar.runtimeIntelligence', to: '/operations/runtime-comparison', icon: <Cpu size={18} />, permission: 'operations:runtime:read', section: 'operations', subGroup: 'sidebar.subGroupSreIntelligence' },
  { labelKey: 'sidebar.predictiveIntelligence', to: '/operations/predictive-intelligence', icon: <Lightbulb size={18} />, permission: 'operations:runtime:read', section: 'operations', subGroup: 'sidebar.subGroupSreIntelligence' },
  { labelKey: 'sidebar.serviceMaturitySre', to: '/operations/service-maturity-sre', icon: <Award size={18} />, permission: 'operations:reliability:read', section: 'operations', subGroup: 'sidebar.subGroupSreIntelligence' },
  // Ferramentas SLO
  { labelKey: 'sidebar.sloBurnRate', to: '/operations/slo-burn-rate', icon: <Flame size={18} />, permission: 'operations:reliability:read', section: 'operations', subGroup: 'sidebar.subGroupSloTools' },
  { labelKey: 'sidebar.sloMarketplace', to: '/operations/slo-marketplace', icon: <Store size={18} />, permission: 'operations:reliability:read', section: 'operations', subGroup: 'sidebar.subGroupSloTools' },
  { labelKey: 'sidebar.dependencyRisk', to: '/operations/dependency-risk', icon: <ShieldAlert size={18} />, permission: 'operations:reliability:read', section: 'operations', subGroup: 'sidebar.subGroupSloTools' },
  // AI-Powered Ops
  { labelKey: 'sidebar.aiAnomaly', to: '/operations/ai-anomaly', icon: <Sparkles size={18} />, permission: 'operations:runtime:read', section: 'operations', subGroup: 'sidebar.subGroupAiOps' },
  { labelKey: 'sidebar.aiIncidentSummarizer', to: '/operations/ai-incident-summarizer', icon: <Bot size={18} />, permission: 'operations:incidents:read', section: 'operations', subGroup: 'sidebar.subGroupAiOps' },
  { labelKey: 'sidebar.aiRunbookSuggester', to: '/operations/ai-runbook-suggester', icon: <BrainCircuit size={18} />, permission: 'operations:runbooks:read', section: 'operations', subGroup: 'sidebar.subGroupAiOps' },
  // Outros
  { labelKey: 'sidebar.chaosEngineering', to: '/operations/chaos-engineering', icon: <AlertTriangle size={18} />, permission: 'operations:runtime:write', section: 'operations' },
  { labelKey: 'sidebar.loadTesting', to: '/operations/load-testing', icon: <Activity size={18} />, permission: 'operations:telemetry:read', section: 'operations' },
  { labelKey: 'sidebar.apiRegression', to: '/operations/api-regression', icon: <FlaskConical size={18} />, permission: 'operations:telemetry:read', section: 'operations' },
  { labelKey: 'sidebar.automation', to: '/operations/automation', icon: <Workflow size={18} />, permission: 'operations:automation:read', section: 'operations' },
  { labelKey: 'sidebar.automationAdmin', to: '/operations/automation/admin', icon: <Settings size={18} />, permission: 'operations:automation:read', section: 'operations' },
  { labelKey: 'sidebar.operationalNotes', to: '/knowledge/notes', icon: <StickyNote size={18} />, permission: 'catalog:assets:read', section: 'operations' },

  // ── HUB DE IA ─────────────────────────────────────────────────────────────
  { labelKey: 'sidebar.aiAssistant', to: '/ai/assistant', icon: <Bot size={18} />, permission: 'ai:assistant:read', section: 'aiHub' },
  { labelKey: 'sidebar.aiAgents', to: '/ai/agents', icon: <Sparkles size={18} />, permission: 'ai:assistant:read', section: 'aiHub' },
  { labelKey: 'sidebar.agentMarketplace', to: '/ai/marketplace', icon: <Store size={18} />, permission: 'ai:runtime:read', section: 'aiHub' },
  { labelKey: 'sidebar.aiAnalysis', to: '/ai/analysis', icon: <BarChart3 size={18} />, permission: 'ai:runtime:write', section: 'aiHub' },
  { labelKey: 'sidebar.aiMemoryIntelligence', to: '/ai/intelligence', icon: <Brain size={18} />, permission: 'ai:governance:read', section: 'aiHub' },
  // Governança de Modelos
  { labelKey: 'sidebar.modelGovernance', to: '/ai/models', icon: <BrainCircuit size={18} />, permission: 'ai:governance:read', section: 'aiHub', subGroup: 'sidebar.subGroupModelGovernance' },
  { labelKey: 'sidebar.aiPolicies', to: '/ai/policies', icon: <FileLock2 size={18} />, permission: 'ai:governance:read', section: 'aiHub', subGroup: 'sidebar.subGroupModelGovernance' },
  { labelKey: 'sidebar.aiRouting', to: '/ai/routing', icon: <ArrowRightLeft size={18} />, permission: 'ai:governance:read', section: 'aiHub', subGroup: 'sidebar.subGroupModelGovernance' },
  { labelKey: 'sidebar.featureModelBindings', to: '/ai/feature-bindings', icon: <SlidersHorizontal size={18} />, permission: 'ai:governance:write', section: 'aiHub', subGroup: 'sidebar.subGroupModelGovernance' },
  { labelKey: 'sidebar.userModelPolicies', to: '/ai/user-model-policies', icon: <Settings2 size={18} />, permission: 'ai:governance:write', section: 'aiHub', subGroup: 'sidebar.subGroupModelGovernance' },
  // Ferramentas de IA
  { labelKey: 'sidebar.aiBudgets', to: '/ai/budgets', icon: <Coins size={18} />, permission: 'ai:governance:read', section: 'aiHub', subGroup: 'sidebar.subGroupAiTools' },
  { labelKey: 'sidebar.userTokenQuotas', to: '/ai/user-token-quotas', icon: <Clock size={18} />, permission: 'ai:governance:write', section: 'aiHub', subGroup: 'sidebar.subGroupAiTools' },
  { labelKey: 'sidebar.aiMcp', to: '/ai/mcp', icon: <Network size={18} />, permission: 'ai:runtime:read', section: 'aiHub', subGroup: 'sidebar.subGroupAiTools' },
  { labelKey: 'sidebar.aiIde', to: '/ai/ide', icon: <Code2 size={18} />, permission: 'ai:governance:read', section: 'aiHub', subGroup: 'sidebar.subGroupAiTools' },
  { labelKey: 'sidebar.aiAudit', to: '/ai/audit', icon: <ScanEye size={18} />, permission: 'ai:governance:read', section: 'aiHub' },

  // ── GOVERNANÇA ────────────────────────────────────────────────────────────
  { labelKey: 'sidebar.executiveOverview', to: '/governance/executive', icon: <Briefcase size={18} />, permission: 'governance:reports:read', section: 'governance' },
  { labelKey: 'sidebar.executiveIntelligence', to: '/governance/executive-intelligence', icon: <Award size={18} />, permission: 'governance:reports:read', section: 'governance' },
  { labelKey: 'sidebar.reports', to: '/governance/reports', icon: <PieChart size={18} />, permission: 'governance:reports:read', section: 'governance' },
  { labelKey: 'sidebar.scheduledReports', to: '/governance/scheduled-reports', icon: <CalendarDays size={18} />, permission: 'governance:reports:read', section: 'governance' },
  { labelKey: 'sidebar.customDashboards', to: '/governance/custom-dashboards', icon: <LayoutGrid size={18} />, permission: 'governance:reports:read', section: 'governance' },
  { labelKey: 'sidebar.dashboardTemplates', to: '/governance/dashboard-templates', icon: <LayoutGrid size={18} />, permission: 'governance:reports:read', section: 'governance' },
  { labelKey: 'sidebar.dashboardReports', to: '/governance/dashboard-reports', icon: <LineChart size={18} />, permission: 'governance:reports:read', section: 'governance' },
  { labelKey: 'sidebar.notebooks', to: '/governance/notebooks', icon: <Edit3 size={18} />, permission: 'governance:reports:read', section: 'governance' },
  { labelKey: 'sidebar.dashboardsAsCode', to: '/governance/dashboards-as-code', icon: <FileCode size={18} />, permission: 'governance:reports:write', section: 'governance' },
  { labelKey: 'sidebar.finops', to: '/governance/finops', icon: <TrendingUp size={18} />, permission: 'governance:finops:read', section: 'governance' },
  { labelKey: 'sidebar.wasteDetection', to: '/governance/waste-detection', icon: <TrendingDown size={18} />, permission: 'governance:finops:read', section: 'governance' },
  { labelKey: 'sidebar.greenOps', to: '/governance/greenops', icon: <Leaf size={18} />, permission: 'governance:finops:read', section: 'governance' },
  // Compliance & Controles
  { labelKey: 'sidebar.compliance', to: '/governance/compliance', icon: <ClipboardCheck size={18} />, permission: 'governance:compliance:read', section: 'governance', subGroup: 'sidebar.subGroupComplianceControls' },
  { labelKey: 'sidebar.complianceScorecardCenter', to: '/governance/centers/compliance-scorecard', icon: <ShieldCheck size={18} />, permission: 'governance:compliance:read', section: 'governance', subGroup: 'sidebar.subGroupComplianceControls' },
  { labelKey: 'sidebar.riskCenter', to: '/governance/risk', icon: <ShieldAlert size={18} />, permission: 'governance:risk:read', section: 'governance', subGroup: 'sidebar.subGroupComplianceControls' },
  { labelKey: 'sidebar.policies', to: '/governance/policies', icon: <Scale size={18} />, permission: 'governance:policies:read', section: 'governance', subGroup: 'sidebar.subGroupComplianceControls' },
  { labelKey: 'sidebar.enterpriseControls', to: '/governance/controls', icon: <Lock size={18} />, permission: 'governance:controls:read', section: 'governance', subGroup: 'sidebar.subGroupComplianceControls' },
  { labelKey: 'sidebar.governancePacks', to: '/governance/packs', icon: <Package size={18} />, permission: 'governance:packs:read', section: 'governance', subGroup: 'sidebar.subGroupComplianceControls' },
  { labelKey: 'sidebar.governanceGates', to: '/governance/gates', icon: <GitMerge size={18} />, permission: 'governance:gates:read', section: 'governance', subGroup: 'sidebar.subGroupComplianceControls' },
  { labelKey: 'sidebar.apiPolicyAsCode', to: '/governance/api-policy-as-code', icon: <FileCode size={18} />, permission: 'governance:policies:read', section: 'governance', subGroup: 'sidebar.subGroupComplianceControls' },
  { labelKey: 'sidebar.evidencePackages', to: '/governance/evidence', icon: <BookText size={18} />, permission: 'governance:evidence:read', section: 'governance', subGroup: 'sidebar.subGroupComplianceControls' },
  { labelKey: 'sidebar.waivers', to: '/governance/waivers', icon: <BookOpenCheck size={18} />, permission: 'governance:waivers:read', section: 'governance', subGroup: 'sidebar.subGroupComplianceControls' },
  { labelKey: 'sidebar.maturityScorecards', to: '/governance/maturity', icon: <Award size={18} />, permission: 'governance:reports:read', section: 'governance', subGroup: 'sidebar.subGroupComplianceControls' },
  { labelKey: 'sidebar.benchmarking', to: '/governance/benchmarking', icon: <BarChart3 size={18} />, permission: 'governance:reports:read', section: 'governance', subGroup: 'sidebar.subGroupComplianceControls' },
  // Tracking & Audit
  { labelKey: 'sidebar.technicalDebt', to: '/governance/technical-debt', icon: <TrendingDown size={18} />, permission: 'governance:reports:read', section: 'governance', subGroup: 'sidebar.subGroupTrackingAudit' },
  { labelKey: 'sidebar.auditTrail', to: '/audit', icon: <History size={18} />, permission: 'audit:trail:read', section: 'governance', subGroup: 'sidebar.subGroupTrackingAudit' },
  // Centros de Insights
  { labelKey: 'sidebar.changeConfidenceHub', to: '/governance/centers/change-confidence', icon: <Target size={18} />, permission: 'governance:reports:read', section: 'governance', subGroup: 'sidebar.subGroupInsightCenters' },
  { labelKey: 'sidebar.blastRadiusExplorer', to: '/governance/blast-radius', icon: <AlertTriangle size={18} />, permission: 'governance:reports:read', section: 'governance', subGroup: 'sidebar.subGroupInsightCenters' },
  { labelKey: 'sidebar.operationalReadiness', to: '/governance/centers/operational-readiness', icon: <Monitor size={18} />, permission: 'governance:reports:read', section: 'governance', subGroup: 'sidebar.subGroupInsightCenters' },
  { labelKey: 'sidebar.driftCenter', to: '/governance/centers/drift', icon: <ArrowRightLeft size={18} />, permission: 'governance:reports:read', section: 'governance', subGroup: 'sidebar.subGroupInsightCenters' },
  { labelKey: 'sidebar.sloServiceCenter', to: '/governance/centers/slo', icon: <HeartPulse size={18} />, permission: 'governance:reports:read', section: 'governance', subGroup: 'sidebar.subGroupInsightCenters' },
  { labelKey: 'sidebar.warRoom', to: '/governance/war-room', icon: <Zap size={18} />, permission: 'governance:reports:read', section: 'governance', subGroup: 'sidebar.subGroupInsightCenters' },
  // Visões por Persona
  { labelKey: 'sidebar.engineerCockpit', to: '/governance/persona/engineer', icon: <Cpu size={18} />, permission: 'governance:reports:read', section: 'governance', subGroup: 'sidebar.subGroupPersonaSuites' },
  { labelKey: 'sidebar.techLeadCommandCenter', to: '/governance/persona/tech-lead', icon: <Users2 size={18} />, permission: 'governance:reports:read', section: 'governance', subGroup: 'sidebar.subGroupPersonaSuites' },
  { labelKey: 'sidebar.architectLandscape', to: '/governance/persona/architect', icon: <Map size={18} />, permission: 'governance:reports:read', section: 'governance', subGroup: 'sidebar.subGroupPersonaSuites' },
  { labelKey: 'sidebar.productPortfolio', to: '/governance/persona/product', icon: <Briefcase size={18} />, permission: 'governance:reports:read', section: 'governance', subGroup: 'sidebar.subGroupPersonaSuites' },
  { labelKey: 'sidebar.executiveBrief', to: '/governance/persona/executive', icon: <Award size={18} />, permission: 'governance:reports:read', section: 'governance', subGroup: 'sidebar.subGroupPersonaSuites' },
  { labelKey: 'sidebar.auditorConsole', to: '/governance/persona/auditor', icon: <ScanEye size={18} />, permission: 'governance:compliance:read', section: 'governance', subGroup: 'sidebar.subGroupPersonaSuites' },

  // ── ORGANIZAÇÃO ───────────────────────────────────────────────────────────
  { labelKey: 'sidebar.teams', to: '/governance/teams', icon: <Users2 size={18} />, permission: 'governance:teams:read', section: 'organization' },
  { labelKey: 'sidebar.domains', to: '/governance/domains', icon: <Globe size={18} />, permission: 'governance:domains:read', section: 'organization' },

  // ── INTEGRAÇÕES ───────────────────────────────────────────────────────────
  { labelKey: 'sidebar.integrationHub', to: '/integrations', icon: <Cable size={18} />, permission: 'integrations:read', section: 'integrations' },

  // ── ADMIN ─────────────────────────────────────────────────────────────────
  { labelKey: 'sidebar.users', to: '/users', icon: <Users size={18} />, permission: 'identity:users:read', section: 'admin' },
  { labelKey: 'sidebar.tenants', to: '/tenants', icon: <Building2 size={18} />, permission: 'identity:tenants:admin', section: 'admin' },
  { labelKey: 'sidebar.breakGlassAndJit', to: '/break-glass', icon: <KeyRound size={18} />, permission: 'identity:sessions:read', section: 'admin' },
  { labelKey: 'sidebar.accessReview', to: '/access-reviews', icon: <UserCheck size={18} />, permission: 'identity:users:read', section: 'admin' },
  { labelKey: 'sidebar.environments', to: '/environments', icon: <Layers size={18} />, permission: 'env:environments:read', section: 'admin' },
  { labelKey: 'sidebar.delegatedAdmin', to: '/governance/delegated-admin', icon: <UserCheck size={18} />, permission: 'governance:admin:read', section: 'admin' },
  { labelKey: 'sidebar.platformConfiguration', to: '/platform/configuration', icon: <Settings size={18} />, permission: 'platform:admin:read', section: 'admin' },
  { labelKey: 'sidebar.platformHealthDashboard', to: '/platform/health', icon: <MonitorDot size={18} />, permission: 'platform:admin:read', section: 'admin' },
  // Saúde da Plataforma
  { labelKey: 'sidebar.platformOperations', to: '/platform/operations', icon: <Server size={18} />, permission: 'platform:admin:read', section: 'admin', subGroup: 'sidebar.subGroupPlatformHealth' },
  { labelKey: 'sidebar.startupReport', to: '/admin/startup-report', icon: <Activity size={18} />, permission: 'platform:admin:read', section: 'admin', subGroup: 'sidebar.subGroupPlatformHealth' },
  { labelKey: 'sidebar.supportBundle', to: '/admin/support-bundle', icon: <Archive size={18} />, permission: 'platform:admin:read', section: 'admin', subGroup: 'sidebar.subGroupPlatformHealth' },
  { labelKey: 'sidebar.databaseHealth', to: '/admin/database-health', icon: <Database size={18} />, permission: 'platform:admin:read', section: 'admin', subGroup: 'sidebar.subGroupPlatformHealth' },
  { labelKey: 'sidebar.backupCoordinator', to: '/admin/backup', icon: <HardDrive size={18} />, permission: 'platform:admin:read', section: 'admin', subGroup: 'sidebar.subGroupPlatformHealth' },
  { labelKey: 'sidebar.platformAlertRules', to: '/admin/platform-alerts', icon: <Bell size={18} />, permission: 'platform:admin:read', section: 'admin', subGroup: 'sidebar.subGroupPlatformHealth' },
  { labelKey: 'sidebar.capacityForecast', to: '/admin/capacity-forecast', icon: <TrendingUp size={18} />, permission: 'platform:admin:read', section: 'admin', subGroup: 'sidebar.subGroupPlatformHealth' },
  // Segurança Admin
  { labelKey: 'sidebar.sessionSecurity', to: '/admin/session-security', icon: <Fingerprint size={18} />, permission: 'platform:admin:read', section: 'admin', subGroup: 'sidebar.subGroupSecurityAdmin' },
  { labelKey: 'sidebar.samlSso', to: '/admin/saml-sso', icon: <Lock size={18} />, permission: 'platform:admin:read', section: 'admin', subGroup: 'sidebar.subGroupSecurityAdmin' },
  { labelKey: 'sidebar.mtlsManager', to: '/admin/mtls', icon: <Shield size={18} />, permission: 'platform:admin:read', section: 'admin', subGroup: 'sidebar.subGroupSecurityAdmin' },
  { labelKey: 'sidebar.networkPolicy', to: '/admin/network-policy', icon: <Network size={18} />, permission: 'platform:admin:read', section: 'admin', subGroup: 'sidebar.subGroupSecurityAdmin' },
  { labelKey: 'sidebar.proxyConfig', to: '/admin/proxy-config', icon: <ArrowRightLeft size={18} />, permission: 'platform:admin:read', section: 'admin', subGroup: 'sidebar.subGroupSecurityAdmin' },
  // Administração de IA
  { labelKey: 'sidebar.aiGovernance', to: '/admin/ai-governance', icon: <BrainCircuit size={18} />, permission: 'platform:admin:read', section: 'admin', subGroup: 'sidebar.subGroupAiAdmin' },
  { labelKey: 'sidebar.aiModelManager', to: '/admin/ai/models', icon: <Bot size={18} />, permission: 'platform:admin:read', section: 'admin', subGroup: 'sidebar.subGroupAiAdmin' },
  { labelKey: 'sidebar.aiResourceGovernor', to: '/admin/ai-governor', icon: <Gauge size={18} />, permission: 'platform:admin:read', section: 'admin', subGroup: 'sidebar.subGroupAiAdmin' },
  { labelKey: 'sidebar.ideExtensionsConsole', to: '/governance/ide-extensions', icon: <Code2 size={18} />, permission: 'platform:admin:read', section: 'admin', subGroup: 'sidebar.subGroupAiAdmin' },
  { labelKey: 'sidebar.licensingAdmin', to: '/governance/licensing', icon: <Coins size={18} />, permission: 'platform:admin:read', section: 'admin', subGroup: 'sidebar.subGroupAiAdmin' },
  { labelKey: 'sidebar.pluginMarketplace', to: '/governance/marketplace', icon: <Store size={18} />, permission: 'governance:reports:read', section: 'admin', subGroup: 'sidebar.subGroupAiAdmin' },
  // Product Analytics
  { labelKey: 'sidebar.productAnalytics', to: '/analytics', icon: <BarChart3 size={18} />, permission: 'platform:admin:read', section: 'admin', subGroup: 'sidebar.subGroupProductAnalytics' },
  { labelKey: 'sidebar.moduleAdoption', to: '/analytics/adoption', icon: <TrendingUp size={18} />, permission: 'platform:admin:read', section: 'admin', subGroup: 'sidebar.subGroupProductAnalytics' },
  { labelKey: 'sidebar.journeyFunnels', to: '/analytics/journeys', icon: <Waypoints size={18} />, permission: 'platform:admin:read', section: 'admin', subGroup: 'sidebar.subGroupProductAnalytics' },
  { labelKey: 'sidebar.personaUsage', to: '/analytics/personas', icon: <Users2 size={18} />, permission: 'platform:admin:read', section: 'admin', subGroup: 'sidebar.subGroupProductAnalytics' },
  { labelKey: 'sidebar.valueTracking', to: '/analytics/value', icon: <Target size={18} />, permission: 'platform:admin:read', section: 'admin', subGroup: 'sidebar.subGroupProductAnalytics' },
  // Geral
  { labelKey: 'sidebar.branding', to: '/platform/branding', icon: <Palette size={18} />, permission: 'configuration:admin', section: 'admin' },
  { labelKey: 'sidebar.userPreferences', to: '/user-preferences', icon: <SlidersHorizontal size={18} />, section: 'admin' },
];

const sectionLabels: Record<NavSection, string> = {
  home: '',
  services: 'sidebar.sectionServices',
  contracts: 'sidebar.sectionContracts',
  changes: 'sidebar.sectionChanges',
  operations: 'sidebar.sectionOperations',
  aiHub: 'sidebar.sectionAiHub',
  governance: 'sidebar.sectionGovernance',
  organization: 'sidebar.sectionOwnershipTeams',
  integrations: 'sidebar.sectionIntegrations',
  admin: 'sidebar.sectionAdmin',
};

/** Ícone representativo de cada secção — exibido no icon rail. */
const sectionIcons: Partial<Record<NavSection, React.ReactNode>> = {
  home: <LayoutDashboard size={22} />,
  services: <Server size={22} />,
  contracts: <FileText size={22} />,
  changes: <Zap size={22} />,
  operations: <Activity size={22} />,
  aiHub: <Bot size={22} />,
  governance: <Briefcase size={22} />,
  organization: <Users size={22} />,
  integrations: <Cable size={22} />,
  admin: <Settings size={22} />,
};

/** Agrupamento visual para separadores no icon rail. */
const sectionGroups: NavSection[][] = [
  ['home', 'services', 'contracts', 'changes', 'operations'],
  ['aiHub'],
  ['governance', 'organization', 'integrations'],
  ['admin'],
];

interface AppSidebarProps {
  collapsed?: boolean;
  onToggleCollapse?: () => void;
  mobile?: boolean;
  className?: string;
}

export function AppSidebar({ collapsed = false, onToggleCollapse, mobile = false, className }: AppSidebarProps) {
  const { t } = useTranslation();
  const location = useLocation();
  const { can } = usePermissions();
  const { config } = usePersona();
  const [activeSection, setActiveSection] = useState<NavSection>('home');

  const { openIncidents } = useNavCounters();

  // Permission-based filter
  const permittedItems = navItems.filter(item => !item.permission || can(item.permission));

  // Persona-based limit: respect sectionOrder priority, keep home always visible
  const visibleItems = (() => {
    const max = config.maxSidebarItems;
    if (!max) return permittedItems;

    const homeItems = permittedItems.filter(i => i.section === 'home');
    const remaining = permittedItems.filter(i => i.section !== 'home');

    const ordered: typeof permittedItems = [];
    for (const section of config.sectionOrder) {
      if (section === 'home') continue;
      ordered.push(...remaining.filter(i => i.section === section));
    }

    const limit = Math.max(0, max - homeItems.length);
    return [...homeItems, ...ordered.slice(0, limit)];
  })();

  // Sections that actually have visible items
  const visibleSections = useMemo(() => {
    const sections = new Set<NavSection>();
    for (const item of visibleItems) sections.add(item.section);
    return sections;
  }, [visibleItems]);

  // Auto-select active section based on current route
  useEffect(() => {
    const match = visibleItems.find(item => {
      if (item.to === '/') return location.pathname === '/';
      return location.pathname.startsWith(item.to);
    });
    if (match && match.section !== activeSection) {
      setActiveSection(match.section);
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [location.pathname]);

  const activeItems = visibleItems.filter(i => i.section === activeSection);

  const getCounter = (to: string): number => {
    if (to === '/operations/incidents') return openIncidents;
    return 0;
  };

  const getSectionCounter = (section: NavSection): number =>
    visibleItems
      .filter(i => i.section === section)
      .reduce((sum, item) => sum + getCounter(item.to), 0);

  return (
    <div
      className={cn(
        'flex h-full',
        !mobile && 'fixed inset-y-0 left-0 z-[var(--z-header)]',
        !mobile && 'transition-[width] duration-[var(--nto-motion-medium)] ease-[var(--ease-standard)]',
        mobile && 'w-[320px]',
        className,
      )}
      style={{
        ...(!mobile ? { width: collapsed ? SIDEBAR_WIDTH_COLLAPSED : SIDEBAR_WIDTH_EXPANDED } : {}),
      }}
      role="navigation"
      aria-label={t('shell.sidebarNav')}
      data-sidebar="dark"
    >
      {/* ─── Icon Rail ─────────────────────────────────────────────────────── */}
      <div
        className="flex flex-col h-full shrink-0 border-r border-edge"
        style={{ width: SIDEBAR_RAIL_WIDTH, background: 'var(--t-sidebar-gradient)' }}
      >
        {/* Logo */}
        <div className="flex items-center justify-center h-[70px] shrink-0 border-b border-edge">
          <img
            src="/brand/logo-icon.svg"
            alt={t('brand.name')}
            className="w-10 h-10 object-contain"
          />
        </div>

        {/* Section tab icons */}
        <div className="flex-1 flex flex-col py-3 px-3 overflow-y-auto">
          {sectionGroups.map((group, gi) => (
            <div key={gi} className={gi === sectionGroups.length - 1 ? 'mt-auto' : undefined}>
              {gi > 0 && (
                <div className="w-6 h-px bg-edge mx-auto my-3" />
              )}
              {group.map(section => {
                if (!visibleSections.has(section)) return null;
                const icon = sectionIcons[section];
                if (!icon) return null;
                const isActive = activeSection === section;
                const counter = getSectionCounter(section);
                const isHighlighted = config.highlightedSections.includes(section);

                return (
                  <button
                    key={section}
                    onClick={() => {
                      setActiveSection(section);
                      if (collapsed && onToggleCollapse) onToggleCollapse();
                    }}
                    title={sectionLabels[section] ? t(sectionLabels[section]) : section}
                    className={cn(
                      'relative flex items-center justify-center w-[48px] h-[44px] mx-auto rounded-md mb-1',
                      'transition-all duration-200',
                      isActive
                        ? 'bg-[rgba(27,127,232,.25)] text-[#3D96F2] shadow-[0_0_12px_rgba(27,127,232,.18),inset_0_0_0_1px_rgba(27,127,232,.4)]'
                        : isHighlighted
                          ? 'text-cyan hover:bg-hover hover:text-cyan'
                          : 'text-[rgba(129,170,214,.5)] hover:bg-[rgba(255,255,255,.04)] hover:text-[rgba(129,170,214,.8)]',
                    )}
                    aria-current={isActive ? 'true' : undefined}
                  >
                    {icon}
                    {counter > 0 && (
                      <span className="absolute -top-1 -right-1 min-w-[16px] h-4 px-0.5 rounded-full bg-critical text-[9px] font-bold text-white flex items-center justify-center leading-none">
                        {counter > 99 ? '99+' : counter}
                      </span>
                    )}
                  </button>
                );
              })}
            </div>
          ))}
        </div>

        {/* Expand toggle (only in collapsed rail) */}
        {collapsed && onToggleCollapse && !mobile && (
          <div className="px-3 py-2 border-t border-edge flex justify-center">
            <button
              onClick={onToggleCollapse}
              className="flex items-center justify-center w-[48px] h-[40px] rounded-md text-faded hover:text-body hover:bg-hover transition-all duration-200"
              title={t('common.expand')}
              aria-label={t('common.expand')}
            >
              <PanelLeftOpen size={18} />
            </button>
          </div>
        )}

      </div>

      {/* ─── Content Panel ─────────────────────────────────────────────────── */}
      <div
        className={cn(
          'flex flex-col h-full overflow-hidden border-r border-edge',
          'transition-[width,opacity] duration-[var(--nto-motion-medium)] ease-[var(--ease-standard)]',
          collapsed && !mobile ? 'w-0 opacity-0' : 'opacity-100',
        )}
        style={{
          ...(!collapsed || mobile ? { width: SIDEBAR_CONTENT_WIDTH } : {}),
          background: 'var(--t-sidebar-gradient)',
        }}
      >
        {/* Section header + collapse toggle */}
        <div className="flex items-center justify-between h-[70px] px-5 shrink-0 border-b border-edge">
          <span className="text-sm font-semibold text-[rgba(181,196,216,.9)] tracking-wide select-none">
            {t('brand.name')}
          </span>
          {onToggleCollapse && !mobile && (
            <button
              onClick={onToggleCollapse}
              className="p-1.5 rounded-lg text-faded hover:text-body hover:bg-hover transition-all duration-150 shrink-0"
              title={t('common.collapse')}
              aria-label={t('common.collapse')}
            >
              <PanelLeftClose size={16} />
            </button>
          )}
        </div>

        {/* Section heading + nav items */}
        <nav className="flex-1 px-5 py-4 overflow-y-auto" aria-label={t('shell.mainNavigation')}>
          {sectionLabels[activeSection] && (
            <p className="text-[11px] font-semibold uppercase tracking-[0.08em] text-accent mb-3 px-1">
              {t(sectionLabels[activeSection])}
            </p>
          )}

          <ul className="space-y-0.5" role="list">
            {activeItems.map((item, idx) => {
              const prev = idx > 0 ? activeItems[idx - 1] : undefined;
              const prevSubGroup = prev?.subGroup;
              const showSubGroupHeading = item.subGroup && item.subGroup !== prevSubGroup;

              return (
                <li key={item.to}>
                  {showSubGroupHeading && (
                    <div className="pt-4 pb-1 px-1">
                      <p className="text-[10px] font-semibold uppercase tracking-[0.06em] text-muted/60">
                        {t(item.subGroup!)}
                      </p>
                    </div>
                  )}
                  <NavLink
                  to={item.to}
                  end={item.to === '/'}
                  className={({ isActive }) =>
                    cn(
                      'flex items-center gap-2.5 px-3 py-2 rounded-lg text-sm',
                      'transition-all duration-150',
                      isActive
                        ? 'bg-[linear-gradient(90deg,rgba(27,127,232,.22),rgba(18,196,232,.08))] text-[#EAF2FF] font-medium shadow-[inset_2px_0_0_#1B7FE8]'
                        : item.preview
                          ? 'text-[rgba(129,170,214,.4)] hover:bg-[rgba(255,255,255,.04)] hover:text-[rgba(181,196,216,.8)]'
                          : 'text-[rgba(181,196,216,.6)] hover:bg-[rgba(255,255,255,.04)] hover:text-[#EAF2FF] font-normal',
                    )
                  }
                >
                  <span className="shrink-0" aria-hidden="true">{item.icon}</span>
                  <span className="truncate flex-1">{t(item.labelKey)}</span>
                  {item.preview && (
                    <span className="ml-auto shrink-0 rounded px-1.5 py-0.5 text-[9px] font-semibold uppercase leading-none bg-warning/15 text-warning border border-warning/25">
                      {t('preview.badge', 'Preview')}
                    </span>
                  )}
                  {!item.preview && getCounter(item.to) > 0 && (
                    <span className="ml-auto shrink-0 min-w-[18px] h-[18px] px-1 rounded-full bg-critical text-[9px] font-bold text-white flex items-center justify-center leading-none">
                      {getCounter(item.to) > 99 ? '99+' : getCounter(item.to)}
                    </span>
                  )}
                </NavLink>
              </li>
              );
            })}
          </ul>
        </nav>

      </div>
    </div>
  );
}
