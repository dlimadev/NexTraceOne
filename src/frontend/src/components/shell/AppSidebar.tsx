import { useState, useEffect, useMemo } from 'react';
import { useNavigate, useLocation, NavLink } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { cn } from '../../lib/cn';
import { useAuth } from '../../contexts/AuthContext';
import { usePermissions } from '../../hooks/usePermissions';
import { usePersona } from '../../contexts/PersonaContext';
// import { AppSidebarFooter } from './AppSidebarFooter';
import type { Permission } from '../../auth/permissions';
import type { NavSection } from '../../auth/persona';
import { SIDEBAR_RAIL_WIDTH, SIDEBAR_CONTENT_WIDTH, SIDEBAR_WIDTH_COLLAPSED, SIDEBAR_WIDTH_EXPANDED } from './constants';
import { useNavCounters } from '../../hooks/useNavCounters';
import {
  LayoutDashboard, FileText, Zap, Users, CheckSquare, ArrowUpCircle,
  Shield, ClipboardList, AlertTriangle, Clock, UserCheck,
  ClipboardCheck, Monitor, Bot, Database, ShieldCheck,
  FileCode, Share2, Server, Layers,
  Globe, Activity, Settings,
  PanelLeftClose, PanelLeftOpen,
  BarChart3, Cable, TrendingUp, BookOpen, Briefcase,
  Network, Workflow, StickyNote, BookMarked, Radar,
  CalendarDays, Award, BrainCircuit, Palette, Cpu,
  Archive, HardDrive,
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
  { labelKey: 'sidebar.dashboard', to: '/', icon: <LayoutDashboard size={18} />, section: 'home' },
  { labelKey: 'sidebar.serviceCatalog', to: '/services', icon: <Server size={18} />, permission: 'catalog:assets:read', section: 'services' },
  { labelKey: 'sidebar.dependencyGraph', to: '/services/graph', icon: <Share2 size={18} />, permission: 'catalog:assets:read', section: 'services' },
  { labelKey: 'legacyCatalog.sidebar.legacyAssets', to: '/services/legacy', icon: <Database size={18} />, permission: 'catalog:assets:read', section: 'services' },
  { labelKey: 'sidebar.serviceDiscovery', to: '/services/discovery', icon: <Radar size={18} />, permission: 'catalog:assets:read', section: 'services' },
  { labelKey: 'sidebar.serviceMaturity', to: '/services/maturity', icon: <Award size={18} />, permission: 'catalog:assets:read', section: 'services' },
  { labelKey: 'sidebar.serviceScorecards', to: '/services/scorecards', icon: <TrendingUp size={18} />, permission: 'catalog:assets:read', section: 'services' },
  { labelKey: 'sidebar.sourceOfTruth', to: '/source-of-truth', icon: <Globe size={18} />, permission: 'catalog:assets:read', section: 'knowledge' },
  { labelKey: 'sidebar.knowledgeHub', to: '/knowledge', icon: <BookOpen size={18} />, permission: 'catalog:assets:read', section: 'knowledge' },
  { labelKey: 'sidebar.operationalNotes', to: '/knowledge/notes', icon: <StickyNote size={18} />, permission: 'catalog:assets:read', section: 'knowledge' },
  { labelKey: 'sidebar.developerPortal', to: '/portal', icon: <BookMarked size={18} />, permission: 'developer-portal:read', section: 'knowledge' },
  { labelKey: 'sidebar.changes', to: '/changes', icon: <ShieldCheck size={18} />, permission: 'change-intelligence:read', section: 'changes' },
  { labelKey: 'sidebar.releases', to: '/releases', icon: <Zap size={18} />, permission: 'change-intelligence:read', section: 'changes' },
  { labelKey: 'sidebar.releaseCalendar', to: '/release-calendar', icon: <CalendarDays size={18} />, permission: 'change-intelligence:read', section: 'changes' },
  { labelKey: 'sidebar.doraMetrics', to: '/dora-metrics', icon: <BarChart3 size={18} />, permission: 'change-intelligence:read', section: 'changes' },
  { labelKey: 'sidebar.workflow', to: '/workflow', icon: <CheckSquare size={18} />, permission: 'workflow:instances:read', section: 'changes' },
  { labelKey: 'sidebar.promotion', to: '/promotion', icon: <ArrowUpCircle size={18} />, permission: 'promotion:requests:read', section: 'changes' },
  { labelKey: 'sidebar.incidents', to: '/operations/incidents', icon: <AlertTriangle size={18} />, permission: 'operations:incidents:read', section: 'operations' },
  { labelKey: 'sidebar.incidentTimeline', to: '/operations/incidents/timeline', icon: <Clock size={18} />, permission: 'operations:incidents:read', section: 'operations' },
  { labelKey: 'sidebar.runbooks', to: '/operations/runbooks', icon: <FileCode size={18} />, permission: 'operations:runbooks:read', section: 'operations' },
  { labelKey: 'sidebar.reliability', to: '/operations/reliability', icon: <Activity size={18} />, permission: 'operations:reliability:read', section: 'operations' },
  { labelKey: 'sidebar.sloManagement', to: '/operations/reliability/slos', icon: <ShieldCheck size={18} />, permission: 'operations:reliability:read', section: 'operations' },
  { labelKey: 'sidebar.automation', to: '/operations/automation', icon: <Workflow size={18} />, permission: 'operations:automation:read', section: 'operations' },
  { labelKey: 'sidebar.runtimeIntelligence', to: '/operations/runtime-comparison', icon: <BarChart3 size={18} />, permission: 'operations:runtime:read', section: 'operations' },
  { labelKey: 'sidebar.predictiveIntelligence', to: '/operations/predictive-intelligence', icon: <BrainCircuit size={18} />, permission: 'operations:runtime:read', section: 'operations' },
  { labelKey: 'sidebar.aiAssistant', to: '/ai/assistant', icon: <Bot size={18} />, permission: 'ai:assistant:read', section: 'aiHub' },
  { labelKey: 'sidebar.aiAgents', to: '/ai/agents', icon: <Network size={18} />, permission: 'ai:assistant:read', section: 'aiHub' },
  { labelKey: 'sidebar.modelRegistry', to: '/ai/models', icon: <Database size={18} />, permission: 'ai:governance:read', section: 'aiHub' },
  { labelKey: 'sidebar.aiPolicies', to: '/ai/policies', icon: <Shield size={18} />, permission: 'ai:governance:read', section: 'aiHub' },
  { labelKey: 'sidebar.aiRouting', to: '/ai/routing', icon: <Share2 size={18} />, permission: 'ai:governance:read', section: 'aiHub' },
  { labelKey: 'sidebar.aiIde', to: '/ai/ide', icon: <Monitor size={18} />, permission: 'ai:governance:read', section: 'aiHub' },
  { labelKey: 'sidebar.aiBudgets', to: '/ai/budgets', icon: <BarChart3 size={18} />, permission: 'ai:governance:read', section: 'aiHub' },
  { labelKey: 'sidebar.aiAudit', to: '/ai/audit', icon: <ClipboardList size={18} />, permission: 'ai:governance:read', section: 'aiHub' },
  { labelKey: 'sidebar.aiAnalysis', to: '/ai/analysis', icon: <BarChart3 size={18} />, permission: 'ai:runtime:read', section: 'aiHub' },
  { labelKey: 'sidebar.executiveOverview', to: '/governance/executive', icon: <Briefcase size={18} />, permission: 'governance:reports:read', section: 'governance' },
  { labelKey: 'sidebar.reports', to: '/governance/reports', icon: <BarChart3 size={18} />, permission: 'governance:reports:read', section: 'governance' },
  { labelKey: 'sidebar.compliance', to: '/governance/compliance', icon: <ClipboardCheck size={18} />, permission: 'governance:compliance:read', section: 'governance' },
  { labelKey: 'sidebar.riskCenter', to: '/governance/risk', icon: <AlertTriangle size={18} />, permission: 'governance:risk:read', section: 'governance' },
  { labelKey: 'sidebar.finops', to: '/governance/finops', icon: <TrendingUp size={18} />, permission: 'governance:finops:read', section: 'governance' },
  { labelKey: 'sidebar.wasteDetection', to: '/governance/waste-detection', icon: <Zap size={18} />, permission: 'governance:finops:read', section: 'governance' },
  { labelKey: 'sidebar.policies', to: '/governance/policies', icon: <Shield size={18} />, permission: 'governance:policies:read', section: 'governance' },
  { labelKey: 'sidebar.packs', to: '/governance/packs', icon: <Layers size={18} />, permission: 'governance:packs:read', section: 'governance' },
  { labelKey: 'sidebar.waivers', to: '/governance/waivers', icon: <ClipboardList size={18} />, permission: 'governance:waivers:read', section: 'governance' },
  { labelKey: 'sidebar.controls', to: '/governance/controls', icon: <ShieldCheck size={18} />, permission: 'governance:controls:read', section: 'governance' },
  { labelKey: 'sidebar.evidence', to: '/governance/evidence', icon: <FileText size={18} />, permission: 'governance:evidence:read', section: 'governance' },
  { labelKey: 'sidebar.maturityScorecards', to: '/governance/maturity', icon: <BarChart3 size={18} />, permission: 'governance:reports:read', section: 'governance' },
  { labelKey: 'sidebar.benchmarking', to: '/governance/benchmarking', icon: <TrendingUp size={18} />, permission: 'governance:reports:read', section: 'governance' },
  { labelKey: 'sidebar.securityGate', to: '/catalog/security-gate', icon: <ShieldCheck size={18} />, permission: 'governance:security:scan', section: 'governance' },
  { labelKey: 'sidebar.contractGovernance', to: '/contracts/governance', icon: <Shield size={18} />, permission: 'contracts:read', section: 'governance', subGroup: 'sidebar.subGroupContractGovernance' },
  { labelKey: 'sidebar.contractHealthDashboard', to: '/contracts/health', icon: <BarChart3 size={18} />, permission: 'contracts:read', section: 'governance', subGroup: 'sidebar.subGroupContractGovernance' },
  { labelKey: 'sidebar.publicationCenter', to: '/contracts/publication', icon: <ArrowUpCircle size={18} />, permission: 'contracts:write', section: 'governance', subGroup: 'sidebar.subGroupContractGovernance' },
  { labelKey: 'sidebar.cdct', to: '/contracts/cdct', icon: <Activity size={18} />, permission: 'contracts:read', section: 'governance', subGroup: 'sidebar.subGroupContractGovernance' },
  { labelKey: 'sidebar.spectralRulesets', to: '/contracts/spectral', icon: <ShieldCheck size={18} />, permission: 'contracts:write', section: 'governance', subGroup: 'sidebar.subGroupContractGovernance' },
  { labelKey: 'sidebar.canonicalEntities', to: '/contracts/canonical', icon: <Database size={18} />, permission: 'contracts:read', section: 'governance', subGroup: 'sidebar.subGroupContractGovernance' },
  { labelKey: 'sidebar.contractPipeline', to: '/catalog/contracts/pipeline', icon: <Zap size={18} />, permission: 'catalog:contracts:pipeline:read', section: 'governance', subGroup: 'sidebar.subGroupContractGovernance' },
  { labelKey: 'sidebar.developerExperienceScore', to: '/catalog/developer-experience-score', icon: <Activity size={18} />, permission: 'catalog:assets:read', section: 'services' },
  { labelKey: 'sidebar.dependencyDashboard', to: '/catalog/dependency-dashboard', icon: <Shield size={18} />, permission: 'catalog:assets:read', section: 'services' },
  { labelKey: 'sidebar.licenseCompliance', to: '/catalog/license-compliance', icon: <CheckSquare size={18} />, permission: 'catalog:assets:read', section: 'services' },
  { labelKey: 'sidebar.apiPolicyAsCode', to: '/governance/api-policy-as-code', icon: <FileCode size={18} />, permission: 'governance:policies:read', section: 'governance' },
  { labelKey: 'sidebar.governanceGates', to: '/governance/gates', icon: <ShieldCheck size={18} />, permission: 'governance:gates:read', section: 'governance' },
  { labelKey: 'sidebar.teams', to: '/governance/teams', icon: <Users size={18} />, permission: 'governance:teams:read', section: 'organization' },
  { labelKey: 'sidebar.domains', to: '/governance/domains', icon: <Globe size={18} />, permission: 'governance:domains:read', section: 'organization' },
  { labelKey: 'sidebar.delegatedAdmin', to: '/governance/delegated-admin', icon: <UserCheck size={18} />, permission: 'governance:admin:read', section: 'organization' },
  { labelKey: 'sidebar.integrationHub', to: '/integrations', icon: <Cable size={18} />, permission: 'integrations:read', section: 'integrations' },
  { labelKey: 'sidebar.productAnalytics', to: '/analytics', icon: <BarChart3 size={18} />, permission: 'analytics:read', section: 'admin' },
  { labelKey: 'sidebar.users', to: '/users', icon: <Users size={18} />, permission: 'identity:users:read', section: 'admin' },
  { labelKey: 'sidebar.breakGlass', to: '/break-glass', icon: <AlertTriangle size={18} />, permission: 'identity:sessions:read', section: 'admin' },
  { labelKey: 'sidebar.jitAccess', to: '/jit-access', icon: <Clock size={18} />, permission: 'identity:users:read', section: 'admin' },
  { labelKey: 'sidebar.delegations', to: '/delegations', icon: <UserCheck size={18} />, permission: 'identity:users:read', section: 'admin' },
  { labelKey: 'sidebar.accessReview', to: '/access-reviews', icon: <ClipboardCheck size={18} />, permission: 'identity:users:read', section: 'admin' },
  { labelKey: 'sidebar.environments', to: '/environments', icon: <Globe size={18} />, permission: 'env:environments:read', section: 'admin' },
  { labelKey: 'sidebar.mySessions', to: '/my-sessions', icon: <Monitor size={18} />, permission: 'identity:sessions:read', section: 'admin' },
  { labelKey: 'sidebar.audit', to: '/audit', icon: <ClipboardList size={18} />, permission: 'audit:trail:read', section: 'admin' },
  { labelKey: 'sidebar.platformOperations', to: '/platform/operations', icon: <Server size={18} />, permission: 'platform:admin:read', section: 'admin' },
  { labelKey: 'sidebar.platformHealthDashboard', to: '/platform/health', icon: <Activity size={18} />, permission: 'platform:admin:read', section: 'admin' },
  { labelKey: 'sidebar.databaseHealth', to: '/admin/database-health', icon: <Database size={18} />, permission: 'platform:admin:read', section: 'admin' },
  { labelKey: 'sidebar.aiModelManager', to: '/admin/ai/models', icon: <Cpu size={18} />, permission: 'platform:admin:read', section: 'admin' },
  { labelKey: 'sidebar.networkPolicy', to: '/admin/network-policy', icon: <Network size={18} />, permission: 'platform:admin:read', section: 'admin' },
  { labelKey: 'sidebar.supportBundle', to: '/admin/support-bundle', icon: <Archive size={18} />, permission: 'platform:admin:read', section: 'admin' },
  { labelKey: 'sidebar.backupCoordinator', to: '/admin/backup', icon: <HardDrive size={18} />, permission: 'platform:admin:read', section: 'admin' },
  { labelKey: 'sidebar.platformConfiguration', to: '/platform/configuration', icon: <Settings size={18} />, permission: 'platform:admin:read', section: 'admin' },
  { labelKey: 'sidebar.parameterUsageReport', to: '/platform/configuration/analytics/usage', icon: <BarChart3 size={18} />, permission: 'configuration:analytics:read', section: 'admin' },
  { labelKey: 'sidebar.parameterCompliance', to: '/platform/configuration/analytics/compliance', icon: <ShieldCheck size={18} />, permission: 'configuration:analytics:read', section: 'admin' },
  { labelKey: 'sidebar.branding', to: '/platform/branding', icon: <Palette size={18} />, permission: 'configuration:admin', section: 'admin' },
  { labelKey: 'sidebar.userPreferences', to: '/user-preferences', icon: <Settings size={18} />, section: 'admin' },
];

const sectionLabels: Record<NavSection, string> = {
  home: '',
  services: 'sidebar.sectionServices',
  knowledge: 'sidebar.sectionKnowledge',
  changes: 'sidebar.sectionChanges',
  operations: 'sidebar.sectionOperations',
  aiHub: 'sidebar.sectionAiHub',
  governance: 'sidebar.sectionGovernance',
  organization: 'sidebar.sectionOwnershipTeams',
  analytics: 'sidebar.sectionAnalytics',
  integrations: 'sidebar.sectionIntegrations',
  admin: 'sidebar.sectionAdmin',
};

/** Ícone representativo de cada secção — exibido no icon rail. */
const sectionIcons: Partial<Record<NavSection, React.ReactNode>> = {
  home: <LayoutDashboard size={22} />,
  services: <Server size={22} />,
  knowledge: <BookOpen size={22} />,
  changes: <Zap size={22} />,
  operations: <AlertTriangle size={22} />,
  aiHub: <Bot size={22} />,
  governance: <Briefcase size={22} />,
  organization: <Users size={22} />,
  integrations: <Cable size={22} />,
  admin: <Settings size={22} />,
};

/** Agrupamento visual para separadores no icon rail. */
const sectionGroups: NavSection[][] = [
  ['home', 'services', 'knowledge', 'changes', 'operations'],
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
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const { can, roleName } = usePermissions();
  const { persona, config } = usePersona();
  const [activeSection, setActiveSection] = useState<NavSection>('home');

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

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
    >
      {/* ─── Icon Rail ─────────────────────────────────────────────────────── */}
      <div
        className="flex flex-col h-full shrink-0 border-r border-edge"
        style={{ width: SIDEBAR_RAIL_WIDTH, background: 'var(--t-sidebar-gradient)' }}
      >
        {/* Logo */}
        <div className="flex items-center justify-center h-[70px] shrink-0 border-b border-edge">
          <img
            src="/logo.svg"
            alt={t('brand.name')}
            className="w-10 h-10 object-contain"
          />
        </div>

        {/* Section tab icons */}
        <div className="flex-1 flex flex-col py-3 px-3 overflow-y-auto">
          {sectionGroups.map((group, gi) => (
            <div key={gi}>
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
                      'relative flex items-center justify-center w-[48px] h-[44px] mx-auto rounded-xl mb-1',
                      'transition-all duration-200',
                      isActive
                        ? 'bg-blue/15 text-blue shadow-glow-blue'
                        : isHighlighted
                          ? 'text-cyan hover:bg-hover hover:text-cyan'
                          : 'text-muted hover:bg-hover hover:text-body',
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
              className="flex items-center justify-center w-[48px] h-[40px] rounded-xl text-faded hover:text-body hover:bg-hover transition-all duration-200"
              title={t('common.expand')}
              aria-label={t('common.expand')}
            >
              <PanelLeftOpen size={18} />
            </button>
          </div>
        )}

        {/* User avatar — rail mode */}
        {/* <AppSidebarFooter
          collapsed
          email={user?.email}
          persona={persona}
          roleName={roleName}
          onLogout={handleLogout}
        /> */}
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
        {/* Brand header + collapse toggle */}
        <div className="flex items-center justify-between h-[70px] px-5 shrink-0 border-b border-edge">
          <span className="text-base font-semibold text-heading truncate">{t('brand.name')}</span>
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
                        ? 'bg-blue text-white font-medium shadow-sm'
                        : item.preview
                          ? 'text-muted/50 hover:bg-hover hover:text-muted'
                          : 'text-body hover:bg-hover hover:text-heading font-normal',
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

        {/* User card — expanded mode */}
        {/* <AppSidebarFooter
          collapsed={false}
          email={user?.email}
          persona={persona}
          roleName={roleName}
          onLogout={handleLogout}
        /> */}
      </div>
    </div>
  );
}
