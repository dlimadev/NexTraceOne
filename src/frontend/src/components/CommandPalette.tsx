import * as React from 'react';
import { useState, useEffect, useRef, useCallback, useMemo } from 'react';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Search,
  LayoutDashboard,
  Zap,
  GitBranch,
  FileText,
  CheckSquare,
  ArrowUpCircle,
  Users,
  ClipboardList,
  CornerDownLeft,
  BookOpen,
  AlertTriangle,
  FileCode,
  Bot,
  ShieldCheck,
  Share2,
  Layers,
  Globe,
  Activity,
  Loader2,
  ArrowRight,
  BarChart3,
  Clock,
  Train,
  MapPin,
} from 'lucide-react';
import { usePermissions } from '../hooks/usePermissions';
import type { Permission } from '../auth/permissions';
import { globalSearchApi } from '../features/catalog/api/globalSearch';
import type { SearchResultItem } from '../features/catalog/api/globalSearch';
import { knowledgeApi } from '../features/knowledge/api/knowledge';
import { usePersona } from '../contexts/PersonaContext';

interface PaletteItem {
  id: string;
  labelKey: string;
  to: string;
  icon: React.ReactNode;
  group: string;
  permission?: Permission;
  preview?: boolean;
}

/**
 * Itens navegáveis da command palette — espelha a sidebar para manter consistência.
 * Agrupados por seção oficial do produto (MODULES-AND-PAGES.md).
 */
const paletteItems: PaletteItem[] = [
  // ── Home ──
  { id: 'dashboard', labelKey: 'sidebar.dashboard', to: '/', icon: <LayoutDashboard size={16} />, group: 'commandPalette.navigation' },
  // ── Services ──
  { id: 'services', labelKey: 'sidebar.serviceCatalog', to: '/services', icon: <GitBranch size={16} />, group: 'commandPalette.services', permission: 'catalog:assets:read' },
  { id: 'dependency-graph', labelKey: 'sidebar.dependencyGraph', to: '/services/graph', icon: <Share2 size={16} />, group: 'commandPalette.services', permission: 'catalog:assets:read' },
  // ── Knowledge ──
  { id: 'source-of-truth', labelKey: 'sidebar.sourceOfTruth', to: '/source-of-truth', icon: <Globe size={16} />, group: 'commandPalette.knowledge', permission: 'catalog:assets:read' },
  { id: 'developer-portal', labelKey: 'sidebar.developerPortal', to: '/portal', icon: <BookOpen size={16} />, group: 'commandPalette.knowledge', permission: 'developer-portal:read' },
  // ── Contracts ──
  { id: 'contracts', labelKey: 'sidebar.apiContracts', to: '/contracts', icon: <FileText size={16} />, group: 'commandPalette.contracts', permission: 'contracts:read' },
  { id: 'contract-studio', labelKey: 'sidebar.contractStudio', to: '/contracts/studio', icon: <Layers size={16} />, group: 'commandPalette.contracts', permission: 'contracts:read' },
  // ── Changes ──
  { id: 'change-confidence', labelKey: 'sidebar.changes', to: '/changes', icon: <ShieldCheck size={16} />, group: 'commandPalette.changes', permission: 'change-intelligence:read' },
  { id: 'releases', labelKey: 'sidebar.releases', to: '/releases', icon: <Zap size={16} />, group: 'commandPalette.changes', permission: 'change-intelligence:read' },
  { id: 'workflow', labelKey: 'sidebar.workflow', to: '/workflow', icon: <CheckSquare size={16} />, group: 'commandPalette.changes', permission: 'workflow:instances:read' },
  { id: 'promotion', labelKey: 'sidebar.promotion', to: '/promotion', icon: <ArrowUpCircle size={16} />, group: 'commandPalette.changes', permission: 'promotion:requests:read' },
  { id: 'release-train', labelKey: 'sidebar.releaseTrain', to: '/release-train', icon: <Train size={16} />, group: 'commandPalette.changes', permission: 'change-intelligence:read' },
  { id: 'release-checklist', labelKey: 'sidebar.releaseChecklist', to: '/workflow/checklist', icon: <MapPin size={16} />, group: 'commandPalette.changes', permission: 'workflow:instances:write' },
  // ── Operations ──
  { id: 'incidents', labelKey: 'sidebar.incidents', to: '/operations/incidents', icon: <AlertTriangle size={16} />, group: 'commandPalette.operations', permission: 'operations:incidents:read' },
  { id: 'incident-timeline', labelKey: 'sidebar.incidentTimeline', to: '/operations/incidents/timeline', icon: <Clock size={16} />, group: 'commandPalette.operations', permission: 'operations:incidents:read' },
  { id: 'runbooks', labelKey: 'sidebar.runbooks', to: '/operations/runbooks', icon: <FileCode size={16} />, group: 'commandPalette.operations', permission: 'operations:runbooks:read' },
  { id: 'reliability', labelKey: 'sidebar.reliability', to: '/operations/reliability', icon: <Activity size={16} />, group: 'commandPalette.operations', permission: 'operations:reliability:read' },
  { id: 'slo-management', labelKey: 'sidebar.sloManagement', to: '/operations/reliability/slos', icon: <ShieldCheck size={16} />, group: 'commandPalette.operations', permission: 'operations:reliability:read' },
  { id: 'automation', labelKey: 'sidebar.automation', to: '/operations/automation', icon: <Zap size={16} />, group: 'commandPalette.operations', permission: 'operations:automation:read' },
  // ── AI Hub ──
  { id: 'ai-assistant', labelKey: 'sidebar.aiAssistant', to: '/ai/assistant', icon: <Bot size={16} />, group: 'commandPalette.aiHub', permission: 'ai:assistant:read' },
  { id: 'ai-agents', labelKey: 'sidebar.aiAgents', to: '/ai/agents', icon: <Bot size={16} />, group: 'commandPalette.aiHub', permission: 'ai:assistant:read' },
  { id: 'ai-models', labelKey: 'sidebar.modelRegistry', to: '/ai/models', icon: <Search size={16} />, group: 'commandPalette.aiHub', permission: 'ai:governance:read' },
  { id: 'ai-policies', labelKey: 'sidebar.aiPolicies', to: '/ai/policies', icon: <ShieldCheck size={16} />, group: 'commandPalette.aiHub', permission: 'ai:governance:read' },
  { id: 'ai-budgets', labelKey: 'sidebar.aiBudgets', to: '/ai/budgets', icon: <BarChart3 size={16} />, group: 'commandPalette.aiHub', permission: 'ai:governance:read' },
  // ── Governance ──
  { id: 'governance-executive', labelKey: 'sidebar.executiveOverview', to: '/governance/executive', icon: <BarChart3 size={16} />, group: 'commandPalette.governance', permission: 'governance:reports:read' },
  { id: 'governance-reports', labelKey: 'sidebar.reports', to: '/governance/reports', icon: <FileText size={16} />, group: 'commandPalette.governance', permission: 'governance:reports:read' },
  { id: 'governance-risk', labelKey: 'sidebar.riskCenter', to: '/governance/risk', icon: <AlertTriangle size={16} />, group: 'commandPalette.governance', permission: 'governance:risk:read' },
  { id: 'governance-compliance', labelKey: 'sidebar.compliance', to: '/governance/compliance', icon: <ClipboardList size={16} />, group: 'commandPalette.governance', permission: 'governance:compliance:read' },
  { id: 'governance-policies', labelKey: 'sidebar.policies', to: '/governance/policies', icon: <ShieldCheck size={16} />, group: 'commandPalette.governance', permission: 'governance:policies:read' },
  { id: 'governance-finops', labelKey: 'sidebar.finops', to: '/governance/finops', icon: <BarChart3 size={16} />, group: 'commandPalette.governance', permission: 'governance:finops:read' },
  { id: 'governance-teams', labelKey: 'sidebar.teams', to: '/governance/teams', icon: <Users size={16} />, group: 'commandPalette.governance', permission: 'governance:teams:read' },
  { id: 'governance-domains', labelKey: 'sidebar.domains', to: '/governance/domains', icon: <Globe size={16} />, group: 'commandPalette.governance', permission: 'governance:domains:read' },
  // ── Integrations ──
  { id: 'integrations', labelKey: 'sidebar.integrationHub', to: '/integrations', icon: <GitBranch size={16} />, group: 'commandPalette.integrations', permission: 'integrations:read' },
  // ── Analytics ──
  { id: 'analytics', labelKey: 'sidebar.productAnalytics', to: '/analytics', icon: <BarChart3 size={16} />, group: 'commandPalette.analytics', permission: 'analytics:read' },
  // ── Admin ──
  { id: 'users', labelKey: 'sidebar.users', to: '/users', icon: <Users size={16} />, group: 'commandPalette.admin', permission: 'identity:users:read' },
  { id: 'audit', labelKey: 'sidebar.audit', to: '/audit', icon: <ClipboardList size={16} />, group: 'commandPalette.admin', permission: 'audit:trail:read' },
  { id: 'platform-ops', labelKey: 'sidebar.platformOperations', to: '/platform/operations', icon: <Search size={16} />, group: 'commandPalette.admin', permission: 'platform:admin:read' },
];

/** Ícone por tipo de entidade retornada pela pesquisa global. */
const entityTypeIcons: Record<string, React.ReactNode> = {
  service: <GitBranch size={16} />,
  contract: <FileText size={16} />,
  runbook: <FileCode size={16} />,
  doc: <BookOpen size={16} />,
  knowledgedocument: <BookOpen size={16} />,
  operationalnote: <ClipboardList size={16} />,
  knowledge: <BookOpen size={16} />,
  note: <ClipboardList size={16} />,
};

/** Chave i18n de label por tipo de entidade. */
const entityTypeLabelKeys: Record<string, string> = {
  service: 'commandPalette.entityService',
  contract: 'commandPalette.entityContract',
  runbook: 'commandPalette.entityRunbook',
  doc: 'commandPalette.entityDoc',
  knowledgedocument: 'commandPalette.entityKnowledge',
  operationalnote: 'commandPalette.entityNote',
  knowledge: 'commandPalette.entityKnowledge',
  note: 'commandPalette.entityNote',
};

/** Cores de badge por status de entidade. */
const STATUS_COLOR_DEFAULT = 'bg-elevated text-muted';
const statusColors: Record<string, string> = {
  active: 'bg-success/20 text-success',
  healthy: 'bg-success/20 text-success',
  degraded: 'bg-warning/20 text-warning',
  draft: STATUS_COLOR_DEFAULT,
  deprecated: 'bg-critical/15 text-critical',
  published: 'bg-success/20 text-success',
  archived: STATUS_COLOR_DEFAULT,
  resolved: 'bg-success/20 text-success',
  info: 'bg-info/15 text-info',
  warning: 'bg-warning/20 text-warning',
  critical: 'bg-critical/15 text-critical',
};

 /** Hook utilitário para debounce de valor. */
function useDebouncedValue<T>(value: T, delayMs: number): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const timer = setTimeout(() => setDebounced(value), delayMs);
    return () => clearTimeout(timer);
  }, [value, delayMs]);
  return debounced;
}

interface CommandPaletteProps {
  open: boolean;
  onClose: () => void;
}

/**
 * Command Palette global — modal de busca/navegação rápida acessível via Cmd+K / Ctrl+K.
 *
 * Funcionalidades:
 * - Filtro textual sobre itens de navegação
 * - Pesquisa global de entidades (serviços, contratos, runbooks, docs) com ≥3 chars
 * - Navegação por teclado (↑ ↓ Enter Escape)
 * - Agrupamento por seção (Navigation / Search Results)
 * - Respeita permissões do usuário autenticado
 */
export function CommandPalette({ open, onClose }: CommandPaletteProps) {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { can } = usePermissions();
  const { persona } = usePersona();
  const inputRef = useRef<HTMLInputElement>(null);
  const [query, setQuery] = useState('');
  const [activeIndex, setActiveIndex] = useState(0);

  const debouncedQuery = useDebouncedValue(query, 300);
  const shouldSearchEntities = debouncedQuery.trim().length >= 3;

  const { data: searchData, isFetching: isSearching } = useQuery({
    queryKey: ['globalSearch', debouncedQuery, persona],
    queryFn: () =>
      globalSearchApi.search({ q: debouncedQuery.trim(), persona, maxResults: 8 }),
    enabled: open && shouldSearchEntities,
    staleTime: 30_000,
  });

  const { data: knowledgeSearchData, isFetching: isSearchingKnowledge } = useQuery({
    queryKey: ['knowledgeSearch', debouncedQuery],
    queryFn: () => knowledgeApi.search({ q: debouncedQuery.trim(), maxResults: 8 }).then((r) => r.data),
    enabled: open && shouldSearchEntities,
    staleTime: 30_000,
  });

  const visibleItems = useMemo(
    () => paletteItems.filter((item) => !item.permission || can(item.permission)),
    [can],
  );

  const entityResults = useMemo(() => {
    const globalItems = searchData?.items ?? [];
    const knowledgeItems = (knowledgeSearchData?.items ?? []).map((item): SearchResultItem => ({
      entityId: item.entityId,
      entityType: item.entityType,
      title: item.title,
      subtitle: item.subtitle,
      owner: null,
      status: item.status,
      route: item.route,
      relevanceScore: item.relevanceScore,
    }));

    const merged = [...globalItems, ...knowledgeItems];
    const deduplicated = new Map<string, SearchResultItem>();

    for (const item of merged) {
      const key = `${item.entityType}:${item.entityId}:${item.route}`;
      if (!deduplicated.has(key)) {
        deduplicated.set(key, item);
      }
    }

    return Array.from(deduplicated.values());
  }, [searchData?.items, knowledgeSearchData?.items]);

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

  /** Total de itens selecionáveis (navegação + entidades + "see all"). */
  const totalSelectableCount = useMemo(() => {
    const entityCount = shouldSearchEntities ? entityResults.length : 0;
    const seeAllCount = shouldSearchEntities && entityResults.length > 0 ? 1 : 0;
    return filtered.length + entityCount + seeAllCount;
  }, [filtered.length, entityResults.length, shouldSearchEntities]);

  useEffect(() => {
    if (open) {
      setQuery('');
      setActiveIndex(0);
      setTimeout(() => inputRef.current?.focus(), 50);
    }
  }, [open]);

  const handleSelectNavItem = useCallback(
    (item: PaletteItem) => {
      onClose();
      navigate(item.to);
    },
    [onClose, navigate],
  );

  const handleSelectEntity = useCallback(
    (item: SearchResultItem) => {
      onClose();
      navigate(item.route);
    },
    [onClose, navigate],
  );

  const handleSeeAll = useCallback(() => {
    onClose();
    navigate(`/search?q=${encodeURIComponent(query.trim())}`);
  }, [onClose, navigate, query]);

  const handleKeyDown = useCallback(
    (e: React.KeyboardEvent) => {
      if (e.key === 'ArrowDown') {
        e.preventDefault();
        setActiveIndex((i) => (totalSelectableCount > 0 ? (i + 1) % totalSelectableCount : 0));
      } else if (e.key === 'ArrowUp') {
        e.preventDefault();
        setActiveIndex((i) =>
          totalSelectableCount > 0 ? (i - 1 + totalSelectableCount) % totalSelectableCount : 0,
        );
      } else if (e.key === 'Enter') {
        e.preventDefault();
        const navCount = filtered.length;
        if (activeIndex < navCount) {
          const item = filtered[activeIndex];
          if (item) handleSelectNavItem(item);
        } else {
          const entityIndex = activeIndex - navCount;
          const entityItem = entityResults[entityIndex];
          if (entityItem) {
            handleSelectEntity(entityItem);
          } else {
            handleSeeAll();
          }
        }
      } else if (e.key === 'Escape') {
        onClose();
      }
    },
    [totalSelectableCount, filtered, activeIndex, entityResults, handleSelectNavItem, handleSelectEntity, handleSeeAll, onClose],
  );

  useEffect(() => {
    setActiveIndex(0);
  }, [query]);

  if (!open) return null;

  let itemCounter = 0;
  const navCount = filtered.length;

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
          {(isSearching || isSearchingKnowledge) && (
            <Loader2 size={16} className="text-muted animate-spin shrink-0" />
          )}
          <kbd className="hidden sm:inline-flex items-center gap-0.5 rounded border border-edge px-1.5 py-0.5 text-[10px] text-muted font-mono">
            ESC
          </kbd>
        </div>

        {/* Results */}
        <div className="max-h-72 overflow-y-auto p-2">
          {/* Navigation results */}
          {filtered.length === 0 && !shouldSearchEntities ? (
            <p className="py-8 text-center text-sm text-muted">{t('common.noResults')}</p>
          ) : (
            <>
              {Array.from(groups.entries()).map(([group, items]) => (
                <div key={group} className="mb-2 last:mb-0">
                  <p className="px-3 py-1.5 text-[11px] font-semibold uppercase tracking-wider text-faded">
                    {t(group)}
                  </p>
                  {items.map((item) => {
                    const index = itemCounter++;
                    return (
                      <button
                        key={item.id}
                        onClick={() => handleSelectNavItem(item)}
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
                        {item.preview && (
                          <span className="rounded border border-warning/25 bg-warning/10 px-1.5 py-0.5 text-[10px] uppercase tracking-wide text-warning">
                            {t('preview.bannerTitle', 'Preview')}
                          </span>
                        )}
                        {index === activeIndex && (
                          <CornerDownLeft size={14} className="shrink-0 opacity-50" />
                        )}
                      </button>
                    );
                  })}
                </div>
              ))}

              {/* Entity search results */}
              {shouldSearchEntities && (
                <div className="mb-2">
                  <p className="px-3 py-1.5 text-[11px] font-semibold uppercase tracking-wider text-faded">
                    {t('commandPalette.searchResults')}
                  </p>

                  {(isSearching || isSearchingKnowledge) && entityResults.length === 0 && (
                    <p className="flex items-center gap-2 px-3 py-3 text-sm text-muted">
                      <Loader2 size={14} className="animate-spin" />
                      {t('commandPalette.loading')}
                    </p>
                  )}

                  {!isSearching && entityResults.length === 0 && (
                    <p className="px-3 py-3 text-sm text-muted">
                      {t('commandPalette.noEntityResults')}
                    </p>
                  )}

                  {entityResults.map((entity, entityIdx) => {
                    const index = navCount + entityIdx;
                    const normalizedEntityType = entity.entityType.toLowerCase();
                    return (
                      <button
                        key={entity.entityId}
                        onClick={() => handleSelectEntity(entity)}
                        className={`flex items-center gap-3 w-full px-3 py-2 rounded-md text-sm transition-colors ${
                          index === activeIndex
                            ? 'bg-accent/15 text-accent'
                            : 'text-body hover:bg-hover'
                        }`}
                        role="option"
                        aria-selected={index === activeIndex}
                      >
                        <span className="shrink-0 opacity-70">
                          {entityTypeIcons[normalizedEntityType] ?? <Search size={16} />}
                        </span>
                        <span className="flex-1 text-left min-w-0">
                          <span className="block truncate">{entity.title}</span>
                          <span className="block truncate text-[11px] text-muted">
                            {entity.subtitle ??
                              t(entityTypeLabelKeys[normalizedEntityType] ?? '', entity.entityType)}
                          </span>
                        </span>
                        {entity.status && (
                          <span
                            className={`shrink-0 rounded-full px-2 py-0.5 text-[10px] font-medium ${
                              statusColors[entity.status] ?? STATUS_COLOR_DEFAULT
                            }`}
                          >
                            {entity.status}
                          </span>
                        )}
                        {index === activeIndex && (
                          <CornerDownLeft size={14} className="shrink-0 opacity-50" />
                        )}
                      </button>
                    );
                  })}

                  {/* See all results link */}
                  {entityResults.length > 0 && (
                    <button
                      onClick={handleSeeAll}
                      className={`flex items-center gap-3 w-full px-3 py-2 rounded-md text-sm transition-colors ${
                        navCount + entityResults.length === activeIndex
                          ? 'bg-accent/15 text-accent'
                          : 'text-accent/70 hover:bg-hover'
                      }`}
                      role="option"
                      aria-selected={navCount + entityResults.length === activeIndex}
                    >
                      <ArrowRight size={16} className="shrink-0 opacity-70" />
                      <span className="flex-1 text-left">{t('commandPalette.seeAllResults')}</span>
                      {navCount + entityResults.length === activeIndex && (
                        <CornerDownLeft size={14} className="shrink-0 opacity-50" />
                      )}
                    </button>
                  )}
                </div>
              )}
            </>
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
