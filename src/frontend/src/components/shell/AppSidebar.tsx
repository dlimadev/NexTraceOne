import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { cn } from '../../lib/cn';
import { useAuth } from '../../contexts/AuthContext';
import { usePermissions } from '../../hooks/usePermissions';
import { usePersona } from '../../contexts/PersonaContext';
import { AppSidebarHeader } from './AppSidebarHeader';
import { AppSidebarGroup } from './AppSidebarGroup';
import { AppSidebarItem } from './AppSidebarItem';
import { AppSidebarFooter } from './AppSidebarFooter';
import type { Permission } from '../../auth/permissions';
import type { NavSection } from '../../auth/persona';
import { SIDEBAR_WIDTH_COLLAPSED, SIDEBAR_WIDTH_EXPANDED } from './constants';
import { isRouteAvailableInFinalProductionScope } from '../../releaseScope';
import {
  LayoutDashboard, FileText, Zap, Users, CheckSquare, ArrowUpCircle,
  Shield, ClipboardList, BookOpen, AlertTriangle, Clock, UserCheck,
  ClipboardCheck, Monitor, Bot, Database, ShieldCheck, ShieldAlert,
  BarChart3, Scale, DollarSign, FileCode, Share2, Server, Layers,
  Globe, Activity, Plug, Gauge, Grid3X3, Award, GitCompare, Route,
  Cable, Building2, TrendingUp, Target, Package, FileCheck, Plus,
  PanelLeftClose, PanelLeftOpen,
} from 'lucide-react';

interface NavItem {
  labelKey: string;
  to: string;
  icon: React.ReactNode;
  permission?: Permission;
  section: NavSection;
  /** Módulo em preview — não homologável. Mostra badge e estilo atenuado. */
  preview?: boolean;
}

const navItems: NavItem[] = [
  { labelKey: 'sidebar.dashboard', to: '/', icon: <LayoutDashboard size={18} />, section: 'home' },
  { labelKey: 'sidebar.serviceCatalog', to: '/services', icon: <Server size={18} />, permission: 'catalog:assets:read', section: 'services' },
  { labelKey: 'sidebar.dependencyGraph', to: '/services/graph', icon: <Share2 size={18} />, permission: 'catalog:assets:read', section: 'services' },
  { labelKey: 'sidebar.developerPortal', to: '/portal', icon: <BookOpen size={18} />, permission: 'developer-portal:read', section: 'services', preview: true },
  { labelKey: 'sidebar.sourceOfTruth', to: '/source-of-truth', icon: <Globe size={18} />, permission: 'catalog:assets:read', section: 'knowledge' },
  { labelKey: 'sidebar.contractCatalog', to: '/contracts', icon: <FileText size={18} />, permission: 'contracts:read', section: 'contracts' },
  { labelKey: 'sidebar.createContract', to: '/contracts/new', icon: <Plus size={18} />, permission: 'contracts:write', section: 'contracts' },
  { labelKey: 'sidebar.contractStudio', to: '/contracts/studio', icon: <Layers size={18} />, permission: 'contracts:read', section: 'contracts' },
  { labelKey: 'sidebar.contractGovernance', to: '/contracts/governance', icon: <Shield size={18} />, permission: 'contracts:read', section: 'contracts' },
  { labelKey: 'sidebar.spectralRulesets', to: '/contracts/spectral', icon: <ShieldCheck size={18} />, permission: 'contracts:write', section: 'contracts', preview: true },
  { labelKey: 'sidebar.canonicalEntities', to: '/contracts/canonical', icon: <Database size={18} />, permission: 'contracts:read', section: 'contracts', preview: true },
  { labelKey: 'sidebar.changeConfidence', to: '/changes', icon: <ShieldCheck size={18} />, permission: 'change-intelligence:read', section: 'changes' },
  { labelKey: 'sidebar.changeIntelligence', to: '/releases', icon: <Zap size={18} />, permission: 'change-intelligence:releases:read', section: 'changes' },
  { labelKey: 'sidebar.workflow', to: '/workflow', icon: <CheckSquare size={18} />, permission: 'workflow:read', section: 'changes' },
  { labelKey: 'sidebar.promotion', to: '/promotion', icon: <ArrowUpCircle size={18} />, permission: 'promotion:read', section: 'changes' },
  { labelKey: 'sidebar.incidents', to: '/operations/incidents', icon: <AlertTriangle size={18} />, permission: 'operations:incidents:read', section: 'operations' },
  { labelKey: 'sidebar.runbooks', to: '/operations/runbooks', icon: <FileCode size={18} />, permission: 'operations:runbooks:read', section: 'operations' },
  { labelKey: 'sidebar.reliability', to: '/operations/reliability', icon: <Activity size={18} />, permission: 'operations:reliability:read', section: 'operations', preview: true },
  { labelKey: 'sidebar.automation', to: '/operations/automation', icon: <Zap size={18} />, permission: 'operations:automation:read', section: 'operations', preview: true },
  { labelKey: 'sidebar.aiAssistant', to: '/ai/assistant', icon: <Bot size={18} />, permission: 'ai:assistant:read', section: 'aiHub' },
  { labelKey: 'sidebar.modelRegistry', to: '/ai/models', icon: <Database size={18} />, permission: 'ai:models:read', section: 'aiHub', preview: true },
  { labelKey: 'sidebar.aiPolicies', to: '/ai/policies', icon: <ShieldCheck size={18} />, permission: 'ai:policies:read', section: 'aiHub', preview: true },
  { labelKey: 'sidebar.aiRouting', to: '/ai/routing', icon: <Route size={18} />, permission: 'ai:governance:read', section: 'aiHub', preview: true },
  { labelKey: 'sidebar.ideIntegrations', to: '/ai/ide', icon: <Plug size={18} />, permission: 'ai:ide:read', section: 'aiHub', preview: true },
  { labelKey: 'sidebar.executiveOverview', to: '/governance/executive', icon: <Gauge size={18} />, permission: 'governance:reports:read', section: 'governance', preview: true },
  { labelKey: 'sidebar.riskHeatmap', to: '/governance/executive/heatmap', icon: <Grid3X3 size={18} />, permission: 'governance:risk:read', section: 'governance', preview: true },
  { labelKey: 'sidebar.maturityScorecards', to: '/governance/executive/maturity', icon: <Award size={18} />, permission: 'governance:reports:read', section: 'governance', preview: true },
  { labelKey: 'sidebar.benchmarking', to: '/governance/executive/benchmarking', icon: <GitCompare size={18} />, permission: 'governance:reports:read', section: 'governance', preview: true },
  { labelKey: 'sidebar.reports', to: '/governance/reports', icon: <BarChart3 size={18} />, permission: 'governance:reports:read', section: 'governance', preview: true },
  { labelKey: 'sidebar.riskCenter', to: '/governance/risk', icon: <ShieldAlert size={18} />, permission: 'governance:risk:read', section: 'governance', preview: true },
  { labelKey: 'sidebar.compliance', to: '/governance/compliance', icon: <Scale size={18} />, permission: 'governance:compliance:read', section: 'governance', preview: true },
  { labelKey: 'sidebar.policyCatalog', to: '/governance/policies', icon: <Shield size={18} />, permission: 'governance:policies:read', section: 'governance', preview: true },
  { labelKey: 'sidebar.evidencePackages', to: '/governance/evidence', icon: <ClipboardList size={18} />, permission: 'governance:evidence:read', section: 'governance', preview: true },
  { labelKey: 'sidebar.enterpriseControls', to: '/governance/controls', icon: <ShieldCheck size={18} />, permission: 'governance:controls:read', section: 'governance', preview: true },
  { labelKey: 'sidebar.finops', to: '/governance/finops', icon: <DollarSign size={18} />, permission: 'governance:finops:read', section: 'governance', preview: true },
  { labelKey: 'sidebar.executiveFinOps', to: '/governance/finops/executive', icon: <DollarSign size={18} />, permission: 'governance:finops:read', section: 'governance', preview: true },
  { labelKey: 'sidebar.governancePacks', to: '/governance/packs', icon: <Package size={18} />, permission: 'governance:packs:read', section: 'governance' },
  { labelKey: 'sidebar.waivers', to: '/governance/waivers', icon: <FileCheck size={18} />, permission: 'governance:waivers:read', section: 'governance' },
  { labelKey: 'sidebar.analyticsOverview', to: '/analytics', icon: <TrendingUp size={18} />, permission: 'governance:analytics:read', section: 'analytics', preview: true },
  { labelKey: 'sidebar.moduleAdoption', to: '/analytics/adoption', icon: <BarChart3 size={18} />, permission: 'governance:analytics:read', section: 'analytics', preview: true },
  { labelKey: 'sidebar.personaUsage', to: '/analytics/personas', icon: <Users size={18} />, permission: 'governance:analytics:read', section: 'analytics', preview: true },
  { labelKey: 'sidebar.journeyFunnels', to: '/analytics/journeys', icon: <Target size={18} />, permission: 'governance:analytics:read', section: 'analytics', preview: true },
  { labelKey: 'sidebar.valueTracking', to: '/analytics/value', icon: <Award size={18} />, permission: 'governance:analytics:read', section: 'analytics', preview: true },
  { labelKey: 'sidebar.integrationHub', to: '/integrations', icon: <Cable size={18} />, permission: 'integrations:read', section: 'integrations', preview: true },
  { labelKey: 'sidebar.ingestionExecutions', to: '/integrations/executions', icon: <Activity size={18} />, permission: 'integrations:read', section: 'integrations', preview: true },
  { labelKey: 'sidebar.ingestionFreshness', to: '/integrations/freshness', icon: <Gauge size={18} />, permission: 'integrations:read', section: 'integrations', preview: true },
  { labelKey: 'sidebar.teams', to: '/governance/teams', icon: <Users size={18} />, permission: 'governance:teams:read', section: 'organization' },
  { labelKey: 'sidebar.domains', to: '/governance/domains', icon: <Building2 size={18} />, permission: 'governance:domains:read', section: 'organization' },
  // TODO: Add 'governance:delegated:read' to Permission type when delegated admin permissions are formalized
  { labelKey: 'sidebar.delegatedAdmin', to: '/governance/delegated-admin', icon: <UserCheck size={18} />, permission: 'governance:delegated:read' as Permission, section: 'organization' },
  { labelKey: 'sidebar.users', to: '/users', icon: <Users size={18} />, permission: 'identity:users:read', section: 'admin' },
  { labelKey: 'sidebar.breakGlass', to: '/break-glass', icon: <AlertTriangle size={18} />, permission: 'identity:sessions:read', section: 'admin' },
  { labelKey: 'sidebar.jitAccess', to: '/jit-access', icon: <Clock size={18} />, permission: 'identity:users:read', section: 'admin' },
  { labelKey: 'sidebar.delegations', to: '/delegations', icon: <UserCheck size={18} />, permission: 'identity:users:read', section: 'admin' },
  { labelKey: 'sidebar.accessReview', to: '/access-reviews', icon: <ClipboardCheck size={18} />, permission: 'identity:users:read', section: 'admin' },
  { labelKey: 'sidebar.mySessions', to: '/my-sessions', icon: <Monitor size={18} />, permission: 'identity:sessions:read', section: 'admin' },
  { labelKey: 'sidebar.audit', to: '/audit', icon: <ClipboardList size={18} />, permission: 'audit:read', section: 'admin' },
  { labelKey: 'sidebar.platformOperations', to: '/platform/operations', icon: <Server size={18} />, permission: 'platform:admin:read', section: 'admin' },
];

const sectionLabels: Record<NavSection, string> = {
  home: '',
  services: 'sidebar.sectionServices',
  knowledge: 'sidebar.sectionKnowledge',
  contracts: 'sidebar.sectionContracts',
  changes: 'sidebar.sectionChanges',
  operations: 'sidebar.sectionOperations',
  aiHub: 'sidebar.sectionAiHub',
  governance: 'sidebar.sectionGovernance',
  organization: 'sidebar.sectionOrganization',
  analytics: 'sidebar.sectionAnalytics',
  integrations: 'sidebar.sectionIntegrations',
  admin: 'sidebar.sectionAdmin',
};

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
  const { can, roleName } = usePermissions();
  const { persona, config } = usePersona();
  const [expandedSections, setExpandedSections] = useState<Set<NavSection>>(new Set(['home']));

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const toggleSection = (section: NavSection) => {
    setExpandedSections(prev => {
      const next = new Set(prev);
      if (next.has(section)) next.delete(section);
      else next.add(section);
      return next;
    });
  };

  const visibleItems = navItems.filter(item => isRouteAvailableInFinalProductionScope(item.to) && (!item.permission || can(item.permission)));
  const isHighlighted = (section: NavSection): boolean => config.highlightedSections.includes(section);

  return (
    <div
      className={cn(
        'bg-deep flex flex-col h-full border-r border-edge',
        !mobile && 'fixed inset-y-0 left-0 z-[var(--z-header)]',
        !mobile && 'transition-[width] duration-[var(--nto-motion-medium)] ease-[var(--ease-standard)]',
        mobile && 'w-[272px]',
        className,
      )}
      style={!mobile ? { width: collapsed ? SIDEBAR_WIDTH_COLLAPSED : SIDEBAR_WIDTH_EXPANDED } : undefined}
      role="navigation"
      aria-label={t('shell.sidebarNav')}
    >
      <div className="h-0.5 brand-gradient shrink-0" />

      <AppSidebarHeader collapsed={collapsed} />

      <nav className={cn('flex-1 py-3 overflow-y-auto', collapsed ? 'px-1.5' : 'px-3')} aria-label={t('shell.mainNavigation')}>
        {config.sectionOrder.map(sectionKey => {
          const sectionItems = visibleItems.filter(i => i.section === sectionKey);
          if (sectionItems.length === 0) return null;

          const labelKey = sectionLabels[sectionKey];
          const highlighted = isHighlighted(sectionKey);
          const isHome = sectionKey === 'home';
          const isExpanded = expandedSections.has(sectionKey) || isHome;
          const hasMultipleItems = sectionItems.length > 1;

          return (
            <AppSidebarGroup
              key={sectionKey}
              sectionKey={sectionKey}
              labelKey={labelKey}
              highlighted={highlighted}
              collapsed={collapsed}
              expanded={isExpanded}
              hasMultipleItems={hasMultipleItems}
              onToggle={() => { if (hasMultipleItems) toggleSection(sectionKey); }}
            >
              {sectionItems.map(item => (
                <AppSidebarItem
                  key={item.to}
                  to={item.to}
                  icon={item.icon}
                  labelKey={item.labelKey}
                  collapsed={collapsed}
                  preview={item.preview}
                />
              ))}
            </AppSidebarGroup>
          );
        })}
      </nav>

      {onToggleCollapse && !mobile && (
        <div className={cn('px-3 py-2 border-t border-edge', collapsed && 'flex justify-center')}>
          <button
            onClick={onToggleCollapse}
            className="flex items-center gap-2 px-2 py-2 rounded-md text-faded hover:text-muted hover:bg-hover transition-all duration-[var(--nto-motion-base)] w-full text-sm"
            title={collapsed ? t('common.expand') : t('common.collapse')}
            aria-label={collapsed ? t('common.expand') : t('common.collapse')}
          >
            {collapsed ? <PanelLeftOpen size={16} /> : <PanelLeftClose size={16} />}
            {!collapsed && <span>{t('common.collapse')}</span>}
          </button>
        </div>
      )}

      <AppSidebarFooter
        collapsed={collapsed}
        email={user?.email}
        persona={persona}
        roleName={roleName}
        onLogout={handleLogout}
      />
    </div>
  );
}
