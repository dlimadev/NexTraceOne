import { NavLink, useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  LayoutDashboard,
  GitBranch,
  FileText,
  Zap,
  Users,
  CheckSquare,
  ArrowUpCircle,
  Shield,
  ClipboardList,
  LogOut,
} from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';
import { usePermissions } from '../hooks/usePermissions';
import type { Permission } from '../auth/permissions';

interface NavItem {
  /** Chave i18n para o label exibido na sidebar. */
  labelKey: string;
  to: string;
  icon: React.ReactNode;
  permission?: Permission;
  /** Seção à qual o item pertence (PLATFORM ou ADMIN). */
  section: 'platform' | 'admin';
}

/**
 * Itens de navegação da sidebar com códigos de permissão alinhados ao catálogo real do backend.
 * As permissões usam o formato modular: "modulo:recurso:ação".
 * Organizados em seções para hierarquia visual clara.
 */
const navItems: NavItem[] = [
  { labelKey: 'sidebar.dashboard', to: '/', icon: <LayoutDashboard size={18} />, section: 'platform' },
  { labelKey: 'sidebar.releases', to: '/releases', icon: <Zap size={18} />, permission: 'change-intelligence:releases:read', section: 'platform' },
  { labelKey: 'sidebar.engineeringGraph', to: '/graph', icon: <GitBranch size={18} />, permission: 'engineering-graph:assets:read', section: 'platform' },
  { labelKey: 'sidebar.contracts', to: '/contracts', icon: <FileText size={18} />, permission: 'contracts:read', section: 'platform' },
  { labelKey: 'sidebar.workflow', to: '/workflow', icon: <CheckSquare size={18} />, permission: 'workflow:read', section: 'platform' },
  { labelKey: 'sidebar.promotion', to: '/promotion', icon: <ArrowUpCircle size={18} />, permission: 'promotion:read', section: 'platform' },
  { labelKey: 'sidebar.licensing', to: '/licensing', icon: <Shield size={18} />, permission: 'licensing:read', section: 'admin' },
  { labelKey: 'sidebar.users', to: '/users', icon: <Users size={18} />, permission: 'identity:users:read', section: 'admin' },
  { labelKey: 'sidebar.audit', to: '/audit', icon: <ClipboardList size={18} />, permission: 'audit:read', section: 'admin' },
];

export function Sidebar() {
  const { t } = useTranslation();
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const { can, roleName } = usePermissions();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const visibleItems = navItems.filter(
    (item) => !item.permission || can(item.permission)
  );

  const platformItems = visibleItems.filter((i) => i.section === 'platform');
  const adminItems = visibleItems.filter((i) => i.section === 'admin');

  /** Renderiza um grupo de items de navegação. */
  const renderSection = (label: string, items: NavItem[]) => {
    if (items.length === 0) return null;
    return (
      <div className="mb-4">
        <p className="px-3 mb-1.5 text-[11px] font-semibold uppercase tracking-wider text-faded">
          {label}
        </p>
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
            <p className="text-[11px] text-muted leading-tight">{t('sidebar.changeIntelligence')}</p>
          </div>
        </div>
      </div>

      {/* Navigation */}
      <nav className="flex-1 px-3 py-4 overflow-y-auto">
        {renderSection(t('sidebar.sectionPlatform'), platformItems)}
        {renderSection(t('sidebar.sectionAdmin'), adminItems)}
      </nav>

      {/* User info */}
      <div className="px-4 py-3 border-t border-edge">
        <div className="flex items-center gap-3">
          <div className="w-8 h-8 rounded-full bg-accent/20 flex items-center justify-center text-accent text-sm font-semibold shrink-0">
            {user?.email?.[0]?.toUpperCase() ?? 'U'}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium text-heading truncate">{user?.email ?? 'User'}</p>
            <p className="text-xs text-muted truncate">{roleName || 'Developer'}</p>
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
