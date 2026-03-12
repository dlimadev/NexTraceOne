import { NavLink, useNavigate } from 'react-router-dom';
import {
  LayoutDashboard,
  GitBranch,
  FileText,
  Zap,
  Users,
  CheckSquare,
  ArrowUpCircle,
  ClipboardList,
  LogOut,
  ChevronRight,
} from 'lucide-react';
import { useAuth } from '../contexts/AuthContext';
import { usePermissions } from '../hooks/usePermissions';
import type { Permission } from '../auth/permissions';

interface NavItem {
  label: string;
  to: string;
  icon: React.ReactNode;
  permission?: Permission;
}

const navItems: NavItem[] = [
  { label: 'Dashboard', to: '/', icon: <LayoutDashboard size={18} /> },
  { label: 'Releases', to: '/releases', icon: <Zap size={18} />, permission: 'releases:read' },
  { label: 'Engineering Graph', to: '/graph', icon: <GitBranch size={18} />, permission: 'graph:read' },
  { label: 'Contracts', to: '/contracts', icon: <FileText size={18} />, permission: 'contracts:read' },
  { label: 'Workflow', to: '/workflow', icon: <CheckSquare size={18} />, permission: 'workflow:read' },
  { label: 'Promotion', to: '/promotion', icon: <ArrowUpCircle size={18} />, permission: 'promotion:read' },
  { label: 'Users', to: '/users', icon: <Users size={18} />, permission: 'users:read' },
  { label: 'Audit', to: '/audit', icon: <ClipboardList size={18} />, permission: 'audit:read' },
];

export function Sidebar() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const { can } = usePermissions();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const visibleItems = navItems.filter(
    (item) => !item.permission || can(item.permission)
  );

  return (
    <aside className="w-64 bg-gray-900 text-gray-100 flex flex-col h-screen fixed left-0 top-0">
      {/* Logo */}
      <div className="px-6 py-5 border-b border-gray-700">
        <div className="flex items-center gap-2">
          <div className="w-7 h-7 bg-indigo-500 rounded flex items-center justify-center text-white font-bold text-sm">
            N
          </div>
          <span className="font-semibold text-lg tracking-tight">NexTraceOne</span>
        </div>
        <p className="text-xs text-gray-400 mt-1">Change Intelligence</p>
      </div>

      {/* Navigation */}
      <nav className="flex-1 px-3 py-4 overflow-y-auto">
        <ul className="space-y-1">
          {visibleItems.map((item) => (
            <li key={item.to}>
              <NavLink
                to={item.to}
                end={item.to === '/'}
                className={({ isActive }) =>
                  `flex items-center gap-3 px-3 py-2 rounded-md text-sm transition-colors ${
                    isActive
                      ? 'bg-indigo-600 text-white'
                      : 'text-gray-300 hover:bg-gray-800 hover:text-white'
                  }`
                }
              >
                {item.icon}
                <span>{item.label}</span>
                <ChevronRight size={14} className="ml-auto opacity-40" />
              </NavLink>
            </li>
          ))}
        </ul>
      </nav>

      {/* User info */}
      <div className="px-4 py-4 border-t border-gray-700">
        <div className="flex items-center gap-3">
          <div className="w-8 h-8 rounded-full bg-indigo-500 flex items-center justify-center text-white text-sm font-medium">
            {user?.email?.[0]?.toUpperCase() ?? 'U'}
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-sm font-medium truncate">{user?.email ?? 'User'}</p>
            <p className="text-xs text-gray-400 truncate">{user?.roles?.[0] ?? 'Developer'}</p>
          </div>
          <button
            onClick={handleLogout}
            className="text-gray-400 hover:text-white transition-colors"
            title="Logout"
          >
            <LogOut size={16} />
          </button>
        </div>
      </div>
    </aside>
  );
}
