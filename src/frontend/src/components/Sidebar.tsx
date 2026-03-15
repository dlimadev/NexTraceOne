import { NavLink, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
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
  { labelKey: 'sidebar.apiContracts', to: '/contracts', icon: <FileText size={18} />, permission: 'contracts:read', section: 'contracts' },
  { labelKey: 'sidebar.contractStudio', to: '/contracts/studio', icon: <Layers size={18} />, permission: 'contracts:read', section: 'contracts' },
  // ── Changes ──
  { labelKey: 'sidebar.changeConfidence', to: '/changes', icon: <ShieldCheck size={18} />, permission: 'change-intelligence:read', section: 'changes' },
  { labelKey: 'sidebar.changeIntelligence', to: '/releases', icon: <Zap size={18} />, permission: 'change-intelligence:releases:read', section: 'changes' },
  { labelKey: 'sidebar.workflow', to: '/workflow', icon: <CheckSquare size={18} />, permission: 'workflow:read', section: 'changes' },
  { labelKey: 'sidebar.promotion', to: '/promotion', icon: <ArrowUpCircle size={18} />, permission: 'promotion:read', section: 'changes' },
  // ── Operations ──
  { labelKey: 'sidebar.incidents', to: '/operations/incidents', icon: <AlertTriangle size={18} />, permission: 'operations:incidents:read', section: 'operations' },
  { labelKey: 'sidebar.runbooks', to: '/operations/runbooks', icon: <FileCode size={18} />, permission: 'operations:runbooks:read', section: 'operations' },
  { labelKey: 'sidebar.reliability', to: '/operations/reliability', icon: <Activity size={18} />, permission: 'operations:reliability:read', section: 'operations' },
  // ── AI Hub ──
  { labelKey: 'sidebar.aiAssistant', to: '/ai/assistant', icon: <Bot size={18} />, permission: 'ai:assistant:read', section: 'aiHub' },
  { labelKey: 'sidebar.modelRegistry', to: '/ai/models', icon: <Database size={18} />, permission: 'ai:models:read', section: 'aiHub' },
  { labelKey: 'sidebar.aiPolicies', to: '/ai/policies', icon: <ShieldCheck size={18} />, permission: 'ai:policies:read', section: 'aiHub' },
  { labelKey: 'sidebar.ideIntegrations', to: '/ai/ide', icon: <Plug size={18} />, permission: 'ai:ide:read', section: 'aiHub' },
  // ── Governance ──
  { labelKey: 'sidebar.reports', to: '/governance/reports', icon: <BarChart3 size={18} />, permission: 'governance:reports:read', section: 'governance' },
  { labelKey: 'sidebar.riskCenter', to: '/governance/risk', icon: <ShieldAlert size={18} />, permission: 'governance:risk:read', section: 'governance' },
  { labelKey: 'sidebar.compliance', to: '/governance/compliance', icon: <Scale size={18} />, permission: 'governance:compliance:read', section: 'governance' },
  { labelKey: 'sidebar.finops', to: '/governance/finops', icon: <DollarSign size={18} />, permission: 'governance:finops:read', section: 'governance' },
  // ── Admin ──
  { labelKey: 'sidebar.licensing', to: '/licensing', icon: <Shield size={18} />, permission: 'licensing:read', section: 'admin' },
  { labelKey: 'sidebar.vendorLicensing', to: '/vendor/licensing', icon: <Shield size={18} />, permission: 'licensing:vendor:license:read', section: 'admin' },
  { labelKey: 'sidebar.users', to: '/users', icon: <Users size={18} />, permission: 'identity:users:read', section: 'admin' },
  { labelKey: 'sidebar.breakGlass', to: '/break-glass', icon: <AlertTriangle size={18} />, permission: 'identity:sessions:read', section: 'admin' },
  { labelKey: 'sidebar.jitAccess', to: '/jit-access', icon: <Clock size={18} />, permission: 'identity:users:read', section: 'admin' },
  { labelKey: 'sidebar.delegations', to: '/delegations', icon: <UserCheck size={18} />, permission: 'identity:users:read', section: 'admin' },
  { labelKey: 'sidebar.accessReview', to: '/access-reviews', icon: <ClipboardCheck size={18} />, permission: 'identity:users:read', section: 'admin' },
  { labelKey: 'sidebar.mySessions', to: '/my-sessions', icon: <Monitor size={18} />, permission: 'identity:sessions:read', section: 'admin' },
  { labelKey: 'sidebar.audit', to: '/audit', icon: <ClipboardList size={18} />, permission: 'audit:read', section: 'admin' },
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
  admin: 'sidebar.sectionAdmin',
};

/**
 * Sidebar com navegação persona-aware.
 *
 * A ordem das secções adapta-se à persona do utilizador (definida em persona.ts).
 * As secções destacadas recebem indicador visual (borda accent).
 * A filtragem por permissão continua a funcionar normalmente.
 *
 * @see docs/PERSONA-UX-MAPPING.md
 */
export function Sidebar() {
  const { t } = useTranslation();
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const { can, roleName } = usePermissions();
  const { persona, config } = usePersona();

  const handleLogout = () => {
    logout();
    navigate('/login');
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
    return (
      <div key={sectionKey} className={`mb-4 ${highlighted ? 'pl-0.5 border-l-2 border-accent/30' : ''}`}>
        {labelKey && (
          <p className={`px-3 mb-1.5 text-[11px] font-semibold uppercase tracking-wider ${highlighted ? 'text-accent' : 'text-faded'}`}>
            {t(labelKey)}
          </p>
        )}
        <ul className="space-y-0.5">
          {items.map((item) => (
            <li key={item.to}>
              <NavLink
                to={item.to}
                end={item.to === '/'}
                className={({ isActive }) =>
                  `flex items-center gap-3 px-3 py-2 rounded-md text-sm transition-colors ${
                    isActive
                      ? 'bg-accent/10 text-accent border-l-2 border-accent -ml-px'
                      : 'text-muted hover:bg-hover hover:text-body'
                  }`
                }
              >
                {item.icon}
                <span>{t(item.labelKey)}</span>
              </NavLink>
            </li>
          ))}
        </ul>
      </div>
    );
  };

  return (
    <aside className="w-64 bg-panel flex flex-col h-screen fixed left-0 top-0 border-r border-edge">
      {/* Brand stripe — gradiente da marca no topo */}
      <div className="h-1 brand-gradient shrink-0" />

      {/* Logo */}
      <div className="px-5 py-4 border-b border-edge">
        <div className="flex items-center gap-2.5">
          <div className="w-8 h-8 rounded-lg bg-accent/15 flex items-center justify-center">
            <span className="text-accent font-bold text-sm">N</span>
          </div>
          <div>
            <span className="font-semibold text-base text-heading tracking-tight">NexTraceOne</span>
            <p className="text-[11px] text-muted leading-tight">{t('sidebar.tagline')}</p>
          </div>
        </div>
      </div>

      {/* Navigation — secções ordenadas por persona */}
      <nav className="flex-1 px-3 py-4 overflow-y-auto">
        {config.sectionOrder.map((sectionKey) => {
          const sectionItems = visibleItems.filter((i) => i.section === sectionKey);
          return renderSection(sectionKey, sectionItems);
        })}
      </nav>

      {/* User info — inclui badge da persona */}
      <div className="px-4 py-3 border-t border-edge">
        <div className="flex items-center gap-3">
          <div className="w-8 h-8 rounded-full bg-accent/20 flex items-center justify-center text-accent text-sm font-semibold shrink-0">
            {user?.email?.[0]?.toUpperCase() ?? 'U'}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium text-heading truncate">{user?.email ?? t('common.user')}</p>
            <p className="text-xs text-muted truncate">
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
      </div>
    </aside>
  );
}
