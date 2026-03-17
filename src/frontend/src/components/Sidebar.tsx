import { useState } from 'react';
import { NavLink, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { cn } from '../lib/cn';
import {
  LayoutDashboard,
  FileText,
  Zap,
  Users,
  CheckSquare,
  ArrowUpCircle,
  Shield,
  ClipboardList,
  BookOpen,
  LogOut,
  AlertTriangle,
  Clock,
  UserCheck,
  ClipboardCheck,
  Monitor,
  Bot,
  Database,
  ShieldCheck,
  ShieldAlert,
  BarChart3,
  Scale,
  DollarSign,
  FileCode,
  Share2,
  Server,
  Layers,
  Globe,
  Activity,
  Plug,
  Gauge,
  Grid3X3,
  Award,
  GitCompare,
  Route,
  Cable,
  Building2,
  TrendingUp,
  Target,
  Package,
  FileCheck,
  PanelLeftClose,
  PanelLeftOpen,
  ChevronDown,
  ChevronRight,
  Plus,
} from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';
import { usePermissions } from '../hooks/usePermissions';
import { usePersona } from '../contexts/PersonaContext';
import type { Permission } from '../auth/permissions';
import type { NavSection } from '../auth/persona';

interface NavItem {
  /** Chave i18n para o label exibido na sidebar. */
  labelKey: string;
  to: string;
  icon: React.ReactNode;
  permission?: Permission;
  /** Seção à qual o item pertence, alinhada aos módulos oficiais do produto. */
  section: NavSection;
}

/**
 * Itens de navegação da sidebar alinhados com MODULES-AND-PAGES.md.
 *
 * A ordem dos itens DENTRO de cada secção é fixa.
 * A ordem das SECÇÕES é determinada pela persona do utilizador.
 *
 * @see docs/PERSONA-UX-MAPPING.md — prioridade de módulos por persona
 */
const navItems: NavItem[] = [
  // ── Home ──
  { labelKey: 'sidebar.dashboard', to: '/', icon: <LayoutDashboard size={18} />, section: 'home' },
  // ── Services ──
  { labelKey: 'sidebar.serviceCatalog', to: '/services', icon: <Server size={18} />, permission: 'catalog:assets:read', section: 'services' },
  { labelKey: 'sidebar.dependencyGraph', to: '/services/graph', icon: <Share2 size={18} />, permission: 'catalog:assets:read', section: 'services' },
  { labelKey: 'sidebar.developerPortal', to: '/portal', icon: <BookOpen size={18} />, permission: 'developer-portal:read', section: 'services' },
  // ── Knowledge ──
  { labelKey: 'sidebar.sourceOfTruth', to: '/source-of-truth', icon: <Globe size={18} />, permission: 'catalog:assets:read', section: 'knowledge' },
  // ── Contracts ──
  { labelKey: 'sidebar.contractCatalog', to: '/contracts', icon: <FileText size={18} />, permission: 'contracts:read', section: 'contracts' },
  { labelKey: 'sidebar.createContract', to: '/contracts/new', icon: <Plus size={18} />, permission: 'contracts:write', section: 'contracts' },
  { labelKey: 'sidebar.contractStudio', to: '/contracts/studio', icon: <Layers size={18} />, permission: 'contracts:read', section: 'contracts' },
  { labelKey: 'sidebar.contractGovernance', to: '/contracts/governance', icon: <Shield size={18} />, permission: 'contracts:read', section: 'contracts' },
  { labelKey: 'sidebar.spectralRulesets', to: '/contracts/spectral', icon: <ShieldCheck size={18} />, permission: 'contracts:write', section: 'contracts' },
  { labelKey: 'sidebar.canonicalEntities', to: '/contracts/canonical', icon: <Database size={18} />, permission: 'contracts:read', section: 'contracts' },
  // ── Changes ──
  { labelKey: 'sidebar.changeConfidence', to: '/changes', icon: <ShieldCheck size={18} />, permission: 'change-intelligence:read', section: 'changes' },
  { labelKey: 'sidebar.changeIntelligence', to: '/releases', icon: <Zap size={18} />, permission: 'change-intelligence:releases:read', section: 'changes' },
  { labelKey: 'sidebar.workflow', to: '/workflow', icon: <CheckSquare size={18} />, permission: 'workflow:read', section: 'changes' },
  { labelKey: 'sidebar.promotion', to: '/promotion', icon: <ArrowUpCircle size={18} />, permission: 'promotion:read', section: 'changes' },
  // ── Operations ──
  { labelKey: 'sidebar.incidents', to: '/operations/incidents', icon: <AlertTriangle size={18} />, permission: 'operations:incidents:read', section: 'operations' },
  { labelKey: 'sidebar.runbooks', to: '/operations/runbooks', icon: <FileCode size={18} />, permission: 'operations:runbooks:read', section: 'operations' },
  { labelKey: 'sidebar.reliability', to: '/operations/reliability', icon: <Activity size={18} />, permission: 'operations:reliability:read', section: 'operations' },
  { labelKey: 'sidebar.automation', to: '/operations/automation', icon: <Zap size={18} />, permission: 'operations:automation:read', section: 'operations' },
  // ── AI Hub ──
  { labelKey: 'sidebar.aiAssistant', to: '/ai/assistant', icon: <Bot size={18} />, permission: 'ai:assistant:read', section: 'aiHub' },
  { labelKey: 'sidebar.modelRegistry', to: '/ai/models', icon: <Database size={18} />, permission: 'ai:models:read', section: 'aiHub' },
  { labelKey: 'sidebar.aiPolicies', to: '/ai/policies', icon: <ShieldCheck size={18} />, permission: 'ai:policies:read', section: 'aiHub' },
  { labelKey: 'sidebar.aiRouting', to: '/ai/routing', icon: <Route size={18} />, permission: 'ai:governance:read', section: 'aiHub' },
  { labelKey: 'sidebar.ideIntegrations', to: '/ai/ide', icon: <Plug size={18} />, permission: 'ai:ide:read', section: 'aiHub' },
  // ── Governance ──
  { labelKey: 'sidebar.executiveOverview', to: '/governance/executive', icon: <Gauge size={18} />, permission: 'governance:reports:read', section: 'governance' },
  { labelKey: 'sidebar.riskHeatmap', to: '/governance/executive/heatmap', icon: <Grid3X3 size={18} />, permission: 'governance:risk:read', section: 'governance' },
  { labelKey: 'sidebar.maturityScorecards', to: '/governance/executive/maturity', icon: <Award size={18} />, permission: 'governance:reports:read', section: 'governance' },
  { labelKey: 'sidebar.benchmarking', to: '/governance/executive/benchmarking', icon: <GitCompare size={18} />, permission: 'governance:reports:read', section: 'governance' },
  { labelKey: 'sidebar.reports', to: '/governance/reports', icon: <BarChart3 size={18} />, permission: 'governance:reports:read', section: 'governance' },
  { labelKey: 'sidebar.riskCenter', to: '/governance/risk', icon: <ShieldAlert size={18} />, permission: 'governance:risk:read', section: 'governance' },
  { labelKey: 'sidebar.compliance', to: '/governance/compliance', icon: <Scale size={18} />, permission: 'governance:compliance:read', section: 'governance' },
  { labelKey: 'sidebar.policyCatalog', to: '/governance/policies', icon: <Shield size={18} />, permission: 'governance:policies:read', section: 'governance' },
  { labelKey: 'sidebar.evidencePackages', to: '/governance/evidence', icon: <ClipboardList size={18} />, permission: 'governance:evidence:read', section: 'governance' },
  { labelKey: 'sidebar.enterpriseControls', to: '/governance/controls', icon: <ShieldCheck size={18} />, permission: 'governance:controls:read', section: 'governance' },
  { labelKey: 'sidebar.finops', to: '/governance/finops', icon: <DollarSign size={18} />, permission: 'governance:finops:read', section: 'governance' },
  { labelKey: 'sidebar.executiveFinOps', to: '/governance/finops/executive', icon: <DollarSign size={18} />, permission: 'governance:finops:read', section: 'governance' },
  { labelKey: 'sidebar.governancePacks', to: '/governance/packs', icon: <Package size={18} />, permission: 'governance:packs:read' as Permission, section: 'governance' },
  { labelKey: 'sidebar.waivers', to: '/governance/waivers', icon: <FileCheck size={18} />, permission: 'governance:waivers:read' as Permission, section: 'governance' },
  // ── Analytics ──
  { labelKey: 'sidebar.analyticsOverview', to: '/analytics', icon: <TrendingUp size={18} />, permission: 'governance:analytics:read', section: 'analytics' },
  { labelKey: 'sidebar.moduleAdoption', to: '/analytics/adoption', icon: <BarChart3 size={18} />, permission: 'governance:analytics:read', section: 'analytics' },
  { labelKey: 'sidebar.personaUsage', to: '/analytics/personas', icon: <Users size={18} />, permission: 'governance:analytics:read', section: 'analytics' },
  { labelKey: 'sidebar.journeyFunnels', to: '/analytics/journeys', icon: <Target size={18} />, permission: 'governance:analytics:read', section: 'analytics' },
  { labelKey: 'sidebar.valueTracking', to: '/analytics/value', icon: <Award size={18} />, permission: 'governance:analytics:read', section: 'analytics' },
  // ── Integrations ──
  { labelKey: 'sidebar.integrationHub', to: '/integrations', icon: <Cable size={18} />, permission: 'integrations:read', section: 'integrations' },
  { labelKey: 'sidebar.ingestionExecutions', to: '/integrations/executions', icon: <Activity size={18} />, permission: 'integrations:read', section: 'integrations' },
  { labelKey: 'sidebar.ingestionFreshness', to: '/integrations/freshness', icon: <Gauge size={18} />, permission: 'integrations:read', section: 'integrations' },
  // ── Organization ──
  { labelKey: 'sidebar.teams', to: '/governance/teams', icon: <Users size={18} />, permission: 'governance:teams:read' as Permission, section: 'organization' },
  { labelKey: 'sidebar.domains', to: '/governance/domains', icon: <Building2 size={18} />, permission: 'governance:domains:read' as Permission, section: 'organization' },
  { labelKey: 'sidebar.delegatedAdmin', to: '/governance/delegated-admin', icon: <UserCheck size={18} />, permission: 'governance:delegated:read' as Permission, section: 'organization' },
  // ── Admin ──
  { labelKey: 'sidebar.users', to: '/users', icon: <Users size={18} />, permission: 'identity:users:read', section: 'admin' },
  { labelKey: 'sidebar.breakGlass', to: '/break-glass', icon: <AlertTriangle size={18} />, permission: 'identity:sessions:read', section: 'admin' },
  { labelKey: 'sidebar.jitAccess', to: '/jit-access', icon: <Clock size={18} />, permission: 'identity:users:read', section: 'admin' },
  { labelKey: 'sidebar.delegations', to: '/delegations', icon: <UserCheck size={18} />, permission: 'identity:users:read', section: 'admin' },
  { labelKey: 'sidebar.accessReview', to: '/access-reviews', icon: <ClipboardCheck size={18} />, permission: 'identity:users:read', section: 'admin' },
  { labelKey: 'sidebar.mySessions', to: '/my-sessions', icon: <Monitor size={18} />, permission: 'identity:sessions:read', section: 'admin' },
  { labelKey: 'sidebar.audit', to: '/audit', icon: <ClipboardList size={18} />, permission: 'audit:read', section: 'admin' },
  { labelKey: 'sidebar.platformOperations', to: '/platform/operations', icon: <Server size={18} />, permission: 'platform:admin:read', section: 'admin' },
];

/** Mapeamento de chave de secção para label i18n. */
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

interface SidebarProps {
  /** Whether the sidebar is collapsed (icon-only mode). */
  collapsed?: boolean;
  /** Callback to toggle collapsed state. */
  onToggleCollapse?: () => void;
}

/**
 * Sidebar com navegação persona-aware e modo colapsado.
 *
 * A ordem das secções adapta-se à persona do utilizador (definida em persona.ts).
 * As secções destacadas recebem indicador visual (borda accent).
 * A filtragem por permissão continua a funcionar normalmente.
 * Secções com múltiplos itens são expansíveis/recolhíveis.
 *
 * @see docs/PERSONA-UX-MAPPING.md
 */
export function Sidebar({ collapsed = false, onToggleCollapse }: SidebarProps) {
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
    setExpandedSections((prev) => {
      const next = new Set(prev);
      if (next.has(section)) {
        next.delete(section);
      } else {
        next.add(section);
      }
      return next;
    });
  };

  const visibleItems = navItems.filter(
    (item) => !item.permission || can(item.permission)
  );

  /** Verifica se uma secção é destacada para a persona actual. */
  const isHighlighted = (section: NavSection): boolean =>
    config.highlightedSections.includes(section);

  /** Renderiza um grupo de items de navegação. */
  const renderSection = (sectionKey: NavSection, items: NavItem[]) => {
    if (items.length === 0) return null;
    const labelKey = sectionLabels[sectionKey];
    const highlighted = isHighlighted(sectionKey);
    const isHome = sectionKey === 'home';
    const isExpanded = expandedSections.has(sectionKey) || isHome;
    const hasMultipleItems = items.length > 1;

    if (collapsed) {
      return (
        <div key={sectionKey} className="mb-1">
          {items.map((item) => (
            <NavLink
              key={item.to}
              to={item.to}
              end={item.to === '/'}
              title={t(item.labelKey)}
              className={({ isActive }) =>
                cn(
                  'flex items-center justify-center w-10 h-10 mx-auto rounded-md mb-0.5',
                  'transition-all duration-[var(--nto-motion-base)]',
                  isActive
                    ? 'bg-accent/10 text-cyan shadow-glow-sm'
                    : 'text-muted hover:bg-hover hover:text-body',
                )
              }
            >
              {item.icon}
            </NavLink>
          ))}
        </div>
      );
    }

    return (
      <div key={sectionKey} className={cn('mb-1.5', highlighted && 'pl-0.5 border-l-2 border-cyan/30')}>
        {labelKey && (
          <button
            onClick={() => { if (hasMultipleItems) toggleSection(sectionKey); }}
            className={cn(
              'w-full flex items-center justify-between px-3 py-1.5 text-[11px] font-semibold uppercase tracking-wider',
              highlighted ? 'text-cyan' : 'text-faded',
              hasMultipleItems ? 'hover:text-muted cursor-pointer' : 'cursor-default',
            )}
          >
            <span>{t(labelKey)}</span>
            {hasMultipleItems && (
              <span className="text-faded">
                {isExpanded ? <ChevronDown size={12} /> : <ChevronRight size={12} />}
              </span>
            )}
          </button>
        )}
        {(isExpanded || !hasMultipleItems) && (
          <ul className="space-y-0.5">
            {items.map((item) => (
              <li key={item.to}>
                <NavLink
                  to={item.to}
                  end={item.to === '/'}
                  className={({ isActive }) =>
                    cn(
                      'flex items-center gap-3 px-3 py-2 rounded-md text-sm',
                      'transition-all duration-[var(--nto-motion-base)]',
                      isActive
                        ? 'bg-accent/10 text-cyan font-medium border-l-2 border-cyan -ml-0.5 pl-[10px]'
                        : 'text-muted hover:bg-hover hover:text-body',
                    )
                  }
                >
                  {item.icon}
                  <span className="truncate">{t(item.labelKey)}</span>
                </NavLink>
              </li>
            ))}
          </ul>
        )}
      </div>
    );
  };

  return (
    <aside
      className={cn(
        'bg-deep flex flex-col h-screen fixed left-0 top-0 border-r border-edge z-[var(--z-header)]',
        'transition-all duration-[var(--nto-motion-medium)]',
      )}
      style={{ width: collapsed ? 64 : 272 }}
    >
      {/* Brand stripe */}
      <div className="h-0.5 brand-gradient shrink-0" />

      {/* Logo */}
      <div className={cn(
        'py-4 border-b border-edge flex items-center',
        collapsed ? 'justify-center px-3' : 'gap-3 px-5',
      )}>
        <div className="w-9 h-9 rounded-lg bg-accent/12 flex items-center justify-center shrink-0 shadow-glow-sm">
          <span className="text-cyan font-bold text-base">N</span>
        </div>
        {!collapsed && (
          <div className="flex-1 min-w-0">
            <span className="font-semibold text-sm text-heading tracking-tight">NexTraceOne</span>
            <p className="text-[10px] text-muted leading-tight truncate">{t('sidebar.tagline')}</p>
          </div>
        )}
      </div>

      {/* Navigation — secções ordenadas por persona */}
      <nav className={cn('flex-1 py-3 overflow-y-auto', collapsed ? 'px-1.5' : 'px-3')}>
        {config.sectionOrder.map((sectionKey) => {
          const sectionItems = visibleItems.filter((i) => i.section === sectionKey);
          return renderSection(sectionKey, sectionItems);
        })}
      </nav>

      {/* Collapse toggle */}
      {onToggleCollapse && (
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

      {/* User info */}
      <div className={cn('py-3 border-t border-edge', collapsed ? 'px-2 flex justify-center' : 'px-4')}>
        {collapsed ? (
          <button
            onClick={handleLogout}
            className="w-8 h-8 rounded-lg bg-accent/15 flex items-center justify-center text-cyan text-sm font-semibold hover:bg-critical/20 hover:text-critical transition-all duration-[var(--nto-motion-base)]"
            title={t('auth.signOut')}
            aria-label={t('auth.signOut')}
          >
            {user?.email?.[0]?.toUpperCase() ?? 'U'}
          </button>
        ) : (
          <div className="flex items-center gap-2.5">
            <div className="w-8 h-8 rounded-lg bg-accent/15 flex items-center justify-center text-cyan text-sm font-semibold shrink-0">
              {user?.email?.[0]?.toUpperCase() ?? 'U'}
            </div>
            <div className="flex-1 min-w-0">
              <p className="text-sm font-medium text-heading truncate">{user?.email ?? t('common.user')}</p>
              <p className="text-[11px] text-muted truncate">
                {t(`persona.${persona}.label`)} · {roleName || t('common.defaultRole')}
              </p>
            </div>
            <button
              onClick={handleLogout}
              className="text-muted hover:text-critical transition-colors p-1 rounded hover:bg-critical/10"
              title={t('auth.signOut')}
              aria-label={t('auth.signOut')}
            >
              <LogOut size={16} />
            </button>
          </div>
        )}
      </div>
    </aside>
  );
}
