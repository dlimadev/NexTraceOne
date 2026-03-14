import { useState, useEffect, useRef, useCallback, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  Search,
  LayoutDashboard,
  Zap,
  GitBranch,
  FileText,
  CheckSquare,
  ArrowUpCircle,
  Shield,
  Users,
  ClipboardList,
  CornerDownLeft,
  BookOpen,
  AlertTriangle,
  FileCode,
  Bot,
  Database,
  ShieldCheck,
  ShieldAlert,
  BarChart3,
  Scale,
  DollarSign,
} from 'lucide-react';
import { usePermissions } from '../hooks/usePermissions';
import type { Permission } from '../auth/permissions';

interface PaletteItem {
  id: string;
  labelKey: string;
  to: string;
  icon: React.ReactNode;
  group: string;
  permission?: Permission;
}

/**
 * Itens navegáveis da command palette — espelha a sidebar para manter consistência.
 * Agrupados por seção oficial do produto (MODULES-AND-PAGES.md).
 */
const paletteItems: PaletteItem[] = [
  // ── Home ──
  { id: 'dashboard', labelKey: 'sidebar.dashboard', to: '/', icon: <LayoutDashboard size={16} />, group: 'commandPalette.navigation' },
  // ── Services ──
  { id: 'services', labelKey: 'sidebar.serviceCatalog', to: '/services', icon: <GitBranch size={16} />, group: 'commandPalette.services', permission: 'engineering-graph:assets:read' },
  { id: 'portal', labelKey: 'sidebar.developerPortal', to: '/portal', icon: <BookOpen size={16} />, group: 'commandPalette.services', permission: 'developer-portal:read' },
  // ── Contracts ──
  { id: 'contracts', labelKey: 'sidebar.apiContracts', to: '/contracts', icon: <FileText size={16} />, group: 'commandPalette.contracts', permission: 'contracts:read' },
  // ── Changes ──
  { id: 'releases', labelKey: 'sidebar.changeIntelligence', to: '/releases', icon: <Zap size={16} />, group: 'commandPalette.changes', permission: 'change-intelligence:releases:read' },
  { id: 'workflow', labelKey: 'sidebar.workflow', to: '/workflow', icon: <CheckSquare size={16} />, group: 'commandPalette.changes', permission: 'workflow:read' },
  { id: 'promotion', labelKey: 'sidebar.promotion', to: '/promotion', icon: <ArrowUpCircle size={16} />, group: 'commandPalette.changes', permission: 'promotion:read' },
  // ── Operations ──
  { id: 'incidents', labelKey: 'sidebar.incidents', to: '/operations/incidents', icon: <AlertTriangle size={16} />, group: 'commandPalette.operations', permission: 'operations:incidents:read' },
  { id: 'runbooks', labelKey: 'sidebar.runbooks', to: '/operations/runbooks', icon: <FileCode size={16} />, group: 'commandPalette.operations', permission: 'operations:runbooks:read' },
  // ── AI Hub ──
  { id: 'ai-assistant', labelKey: 'sidebar.aiAssistant', to: '/ai/assistant', icon: <Bot size={16} />, group: 'commandPalette.aiHub', permission: 'ai:assistant:read' },
  { id: 'ai-models', labelKey: 'sidebar.modelRegistry', to: '/ai/models', icon: <Database size={16} />, group: 'commandPalette.aiHub', permission: 'ai:models:read' },
  { id: 'ai-policies', labelKey: 'sidebar.aiPolicies', to: '/ai/policies', icon: <ShieldCheck size={16} />, group: 'commandPalette.aiHub', permission: 'ai:policies:read' },
  // ── Governance ──
  { id: 'reports', labelKey: 'sidebar.reports', to: '/governance/reports', icon: <BarChart3 size={16} />, group: 'commandPalette.governance', permission: 'governance:reports:read' },
  { id: 'risk', labelKey: 'sidebar.riskCenter', to: '/governance/risk', icon: <ShieldAlert size={16} />, group: 'commandPalette.governance', permission: 'governance:risk:read' },
  { id: 'compliance', labelKey: 'sidebar.compliance', to: '/governance/compliance', icon: <Scale size={16} />, group: 'commandPalette.governance', permission: 'governance:compliance:read' },
  { id: 'finops', labelKey: 'sidebar.finops', to: '/governance/finops', icon: <DollarSign size={16} />, group: 'commandPalette.governance', permission: 'governance:finops:read' },
  // ── Admin ──
  { id: 'licensing', labelKey: 'sidebar.licensing', to: '/licensing', icon: <Shield size={16} />, group: 'commandPalette.admin', permission: 'licensing:read' },
  { id: 'users', labelKey: 'sidebar.users', to: '/users', icon: <Users size={16} />, group: 'commandPalette.admin', permission: 'identity:users:read' },
  { id: 'audit', labelKey: 'sidebar.audit', to: '/audit', icon: <ClipboardList size={16} />, group: 'commandPalette.admin', permission: 'audit:read' },
];

interface CommandPaletteProps {
  open: boolean;
  onClose: () => void;
}

/**
 * Command Palette global — modal de busca/navegação rápida acessível via Cmd+K / Ctrl+K.
 *
 * Funcionalidades:
 * - Filtro textual sobre itens de navegação
 * - Navegação por teclado (↑ ↓ Enter Escape)
 * - Agrupamento por seção (Navigation / Admin)
 * - Respeita permissões do usuário autenticado
 */
export function CommandPalette({ open, onClose }: CommandPaletteProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { can } = usePermissions();
  const inputRef = useRef<HTMLInputElement>(null);
  const [query, setQuery] = useState('');
  const [activeIndex, setActiveIndex] = useState(0);

  const visibleItems = useMemo(
    () => paletteItems.filter((item) => !item.permission || can(item.permission)),
    [can],
  );

  const filtered = useMemo(() => {
    if (!query.trim()) return visibleItems;
    const q = query.toLowerCase();
    return visibleItems.filter((item) =>
      t(item.labelKey).toLowerCase().includes(q) || item.to.includes(q),
    );
  }, [query, visibleItems, t]);

  /** Agrupa itens filtrados por seção para exibição. */
  const groups = useMemo(() => {
    const map = new Map<string, PaletteItem[]>();
    for (const item of filtered) {
      const list = map.get(item.group) ?? [];
      list.push(item);
      map.set(item.group, list);
    }
    return map;
  }, [filtered]);

  const flatItems = useMemo(() => filtered, [filtered]);

  useEffect(() => {
    if (open) {
      setQuery('');
      setActiveIndex(0);
      setTimeout(() => inputRef.current?.focus(), 50);
    }
  }, [open]);

  const handleSelect = useCallback(
    (item: PaletteItem) => {
      onClose();
      navigate(item.to);
    },
    [onClose, navigate],
  );

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === 'ArrowDown') {
        e.preventDefault();
        setActiveIndex((i) => (i + 1) % flatItems.length);
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        setActiveIndex((i) => (i - 1 + flatItems.length) % flatItems.length);
      } else if (e.key === 'Enter' && flatItems[activeIndex]) {
        e.preventDefault();
        handleSelect(flatItems[activeIndex]);
      } else if (e.key === 'Escape') {
        onClose();
      }
    },
    [flatItems, activeIndex, handleSelect, onClose],
  );

  useEffect(() => {
    setActiveIndex(0);
  }, [query]);

  if (!open) return null;

  let itemCounter = 0;

  return (
    <div
      className="fixed inset-0 z-50 flex items-start justify-center pt-[15vh]"
      onClick={onClose}
      role="dialog"
      aria-modal="true"
      aria-label={t('commandPalette.title')}
    >
      {/* Backdrop */}
      <div className="absolute inset-0 bg-black/60 backdrop-blur-sm" />

      {/* Palette */}
      <div
        className="relative w-full max-w-lg bg-panel border border-edge rounded-xl shadow-xl overflow-hidden animate-slide-up"
        onClick={(e) => e.stopPropagation()}
      >
        {/* Search input */}
        <div className="flex items-center gap-3 px-4 py-3 border-b border-edge">
          <Search size={18} className="text-muted shrink-0" />
          <input
            ref={inputRef}
            type="text"
            value={query}
            onChange={(e) => setQuery(e.target.value)}
            onKeyDown={handleKeyDown}
            placeholder={t('commandPalette.placeholder')}
            className="flex-1 bg-transparent text-heading text-sm placeholder:text-muted outline-none"
            aria-label={t('commandPalette.placeholder')}
          />
          <kbd className="hidden sm:inline-flex items-center gap-0.5 rounded border border-edge px-1.5 py-0.5 text-[10px] text-muted font-mono">
            ESC
          </kbd>
        </div>

        {/* Results */}
        <div className="max-h-72 overflow-y-auto p-2">
          {flatItems.length === 0 ? (
            <p className="py-8 text-center text-sm text-muted">{t('common.noResults')}</p>
          ) : (
            Array.from(groups.entries()).map(([group, items]) => (
              <div key={group} className="mb-2 last:mb-0">
                <p className="px-3 py-1.5 text-[11px] font-semibold uppercase tracking-wider text-faded">
                  {t(group)}
                </p>
                {items.map((item) => {
                  const index = itemCounter++;
                  return (
                    <button
                      key={item.id}
                      onClick={() => handleSelect(item)}
                      className={`flex items-center gap-3 w-full px-3 py-2 rounded-md text-sm transition-colors ${
                        index === activeIndex
                          ? 'bg-accent/15 text-accent'
                          : 'text-body hover:bg-hover'
                      }`}
                      role="option"
                      aria-selected={index === activeIndex}
                    >
                      <span className="shrink-0 opacity-70">{item.icon}</span>
                      <span className="flex-1 text-left truncate">{t(item.labelKey)}</span>
                      {index === activeIndex && (
                        <CornerDownLeft size={14} className="shrink-0 opacity-50" />
                      )}
                    </button>
                  );
                })}
              </div>
            ))
          )}
        </div>

        {/* Footer hints */}
        <div className="flex items-center gap-4 px-4 py-2 border-t border-edge text-[11px] text-faded">
          <span className="inline-flex items-center gap-1">
            <kbd className="rounded border border-edge px-1 py-0.5 font-mono">↑↓</kbd>
            {t('commandPalette.navigate')}
          </span>
          <span className="inline-flex items-center gap-1">
            <kbd className="rounded border border-edge px-1 py-0.5 font-mono">↵</kbd>
            {t('commandPalette.select')}
          </span>
          <span className="inline-flex items-center gap-1">
            <kbd className="rounded border border-edge px-1 py-0.5 font-mono">esc</kbd>
            {t('commandPalette.close')}
          </span>
        </div>
      </div>
    </div>
  );
}
