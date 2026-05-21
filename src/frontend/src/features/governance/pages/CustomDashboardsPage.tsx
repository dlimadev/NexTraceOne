import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import {
  LayoutDashboard, Plus, Eye, Settings, Copy, Trash2,
  Layout as LayoutIcon, Search, ArrowUpDown, Tag, Star,
  Clock, Filter, X, Globe, Lock, Users,
} from 'lucide-react';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import { useAuth } from '../../../contexts/AuthContext';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { DashboardTemplatePicker } from '../../../components/DashboardTemplatePicker';
import type { TemplatePreview } from '../../../components/DashboardTemplatePicker';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

interface DashboardSummary {
  dashboardId: string;
  name: string;
  persona: string;
  widgetCount: number;
  layout: string;
  isShared: boolean;
  teamId?: string | null;
  isSystem: boolean;
  createdAt: string;
  tags?: string[];
  lifecycleStatus?: string;
}

interface ListDashboardsResponse {
  items: DashboardSummary[];
  totalCount: number;
}

interface WidgetInput {
  existingWidgetId?: string | null;
  type: string;
  posX: number;
  posY: number;
  width: number;
  height: number;
  serviceId?: string | null;
  teamId?: string | null;
  timeRange?: string | null;
  customTitle?: string | null;
}

interface CreateDashboardRequest {
  tenantId: string;
  userId: string;
  name: string;
  description: string;
  layout: string;
  widgets: WidgetInput[];
  persona: string;
  tags?: string[];
}

const LAYOUTS = ['single-column', 'two-column', 'three-column', 'grid', 'custom'] as const;

const PERSONAS = [
  'Engineer', 'TechLead', 'Architect', 'Product', 'Executive', 'PlatformAdmin', 'Auditor',
] as const;

const WIDGET_IDS = [
  'dora-metrics', 'service-scorecard', 'incident-summary', 'change-confidence',
  'cost-trend', 'reliability-slo', 'knowledge-graph', 'on-call-status',
  'obs-metrics', 'obs-logs', 'obs-traces', 'obs-error-rate',
  'obs-pie-chart', 'obs-bar-gauge', 'obs-heatmap-calendar', 'obs-treemap', 'obs-histogram',
] as const;

const WIDGET_DEFAULT_SIZE: Record<string, { w: number; h: number }> = {
  'dora-metrics':      { w: 2, h: 2 },
  'service-scorecard': { w: 2, h: 2 },
  'incident-summary':  { w: 2, h: 2 },
  'change-confidence': { w: 2, h: 2 },
  'cost-trend':        { w: 2, h: 2 },
  'reliability-slo':   { w: 2, h: 2 },
  'knowledge-graph':   { w: 3, h: 3 },
  'on-call-status':    { w: 2, h: 1 },
  'obs-metrics':           { w: 3, h: 2 },
  'obs-logs':              { w: 3, h: 3 },
  'obs-traces':            { w: 3, h: 3 },
  'obs-error-rate':        { w: 2, h: 2 },
  'obs-pie-chart':         { w: 2, h: 2 },
  'obs-bar-gauge':         { w: 2, h: 2 },
  'obs-heatmap-calendar':  { w: 3, h: 2 },
  'obs-treemap':           { w: 3, h: 2 },
  'obs-histogram':         { w: 2, h: 2 },
};

const PERSONA_VARIANT: Record<string, 'primary' | 'secondary' | 'success' | 'warning'> = {
  Executive: 'primary',
  Engineer: 'secondary',
  TechLead: 'success',
  Architect: 'warning',
  Product: 'primary',
  PlatformAdmin: 'warning',
  Auditor: 'secondary',
};

const WIDGET_KEY_MAP: Record<string, string> = {
  'dora-metrics': 'doraMetrics',
  'service-scorecard': 'serviceScorecard',
  'incident-summary': 'incidentSummary',
  'change-confidence': 'changeConfidence',
  'cost-trend': 'costTrend',
  'reliability-slo': 'reliabilitySlo',
  'knowledge-graph': 'knowledgeGraph',
  'on-call-status': 'onCallStatus',
  'obs-metrics': 'obsMetrics',
  'obs-logs': 'obsLogs',
  'obs-traces': 'obsTraces',
  'obs-error-rate': 'obsErrorRate',
  'obs-pie-chart': 'obsPieChart',
  'obs-bar-gauge': 'obsBarGauge',
  'obs-heatmap-calendar': 'obsHeatmapCalendar',
  'obs-treemap': 'obsTreemap',
  'obs-histogram': 'obsHistogram',
};

const LIFECYCLE_COLORS: Record<string, string> = {
  Draft:      'text-gray-500 bg-gray-100 dark:bg-gray-800',
  Published:  'text-green-700 bg-green-100 dark:bg-green-900/30',
  Deprecated: 'text-yellow-700 bg-yellow-100 dark:bg-yellow-900/30',
  Archived:   'text-red-700 bg-red-100 dark:bg-red-900/30',
};

const FAVORITES_KEY = 'nextraceone:dashboard-favorites';

function getFavorites(): string[] {
  try { return JSON.parse(localStorage.getItem(FAVORITES_KEY) ?? '[]'); }
  catch { return []; }
}

function toggleFavorite(id: string): string[] {
  const favs = getFavorites();
  const next = favs.includes(id) ? favs.filter((f) => f !== id) : [...favs, id];
  localStorage.setItem(FAVORITES_KEY, JSON.stringify(next));
  return next;
}

// ── Hooks ──────────────────────────────────────────────────────────────────

const useListDashboards = (tenantId: string, envId?: string | null) =>
  useQuery({
    queryKey: ['governance-dashboards', tenantId, envId],
    queryFn: () =>
      client
        .get<ListDashboardsResponse>('/governance/dashboards', {
          params: { tenantId, page: 1, pageSize: 50 },
        })
        .then((r) => r.data),
  });

const useCreateDashboard = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateDashboardRequest) =>
      client.post('/governance/dashboards', data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['governance-dashboards'] }),
  });
};

const useCloneDashboard = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ dashboardId, newName, userId }: { dashboardId: string; newName: string; userId: string }) =>
      client
        .post(`/governance/dashboards/${dashboardId}/clone`, {
          newName, tenantId: 'default', userId, sourceDashboardId: dashboardId,
        })
        .then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['governance-dashboards'] }),
  });
};

const useDeleteDashboard = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (dashboardId: string) =>
      client
        .delete(`/governance/dashboards/${dashboardId}`, { params: { tenantId: 'default' } })
        .then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['governance-dashboards'] }),
  });
};

// ── Sub-components ─────────────────────────────────────────────────────────

function SharingIcon({ isShared, isSystem }: { isShared: boolean; isSystem: boolean }) {
  if (isSystem) return <Globe size={11} className="text-blue-500" title="System dashboard" />;
  if (isShared) return <Users size={11} className="text-green-500" title="Shared" />;
  return <Lock size={11} className="text-gray-400" title="Private" />;
}

function DashboardCard({
  dashboard,
  isFavorite,
  onFavoriteToggle,
  onView,
  onEdit,
  onClone,
  onDelete,
}: {
  dashboard: DashboardSummary;
  isFavorite: boolean;
  onFavoriteToggle: () => void;
  onView: () => void;
  onEdit: () => void;
  onClone: () => void;
  onDelete: () => void;
}) {
  const { t } = useTranslation();
  const lifecycle = dashboard.lifecycleStatus ?? 'Published';

  return (
    <Card className="flex flex-col group transition-shadow hover:shadow-md">
      <CardHeader className="pb-0">
        <div className="flex items-start justify-between gap-2">
          <div className="flex items-center gap-1.5 min-w-0">
            <SharingIcon isShared={dashboard.isShared} isSystem={dashboard.isSystem} />
            <h3 className="text-sm font-semibold text-gray-900 dark:text-white truncate">
              {dashboard.name}
            </h3>
          </div>
          <div className="flex items-center gap-1 shrink-0">
            <button
              onClick={onFavoriteToggle}
              className={`p-0.5 rounded transition-colors ${isFavorite ? 'text-yellow-500' : 'text-gray-300 dark:text-gray-600 opacity-0 group-hover:opacity-100'}`}
              aria-label={isFavorite ? t('governance.customDashboards.unfavorite', 'Remove from favorites') : t('governance.customDashboards.favorite', 'Add to favorites')}
            >
              <Star size={13} fill={isFavorite ? 'currentColor' : 'none'} />
            </button>
            <Badge variant={PERSONA_VARIANT[dashboard.persona] ?? 'secondary'}>
              {dashboard.persona}
            </Badge>
          </div>
        </div>
      </CardHeader>
      <CardBody className="pt-2 space-y-2 flex-1 flex flex-col">
        {/* Meta row */}
        <div className="flex items-center gap-3 text-xs text-gray-500 dark:text-gray-400">
          <span>{dashboard.layout}</span>
          <span>
            {dashboard.widgetCount} {t('governance.customDashboards.widgets', 'widgets')}
          </span>
          <span
            className={`rounded-full px-1.5 py-0.5 text-[10px] font-medium ${LIFECYCLE_COLORS[lifecycle] ?? LIFECYCLE_COLORS.Published}`}
          >
            {lifecycle}
          </span>
        </div>

        {/* Tags */}
        {(dashboard.tags?.length ?? 0) > 0 && (
          <div className="flex flex-wrap gap-1">
            {dashboard.tags!.map((tag) => (
              <span
                key={tag}
                className="inline-flex items-center gap-0.5 rounded-full bg-gray-100 dark:bg-gray-800 px-1.5 py-0.5 text-[10px] text-gray-600 dark:text-gray-400"
              >
                <Tag size={8} />
                {tag}
              </span>
            ))}
          </div>
        )}

        {/* Created at */}
        <p className="text-[10px] text-gray-400 flex items-center gap-1">
          <Clock size={9} />
          {new Date(dashboard.createdAt).toLocaleDateString()}
        </p>

        {/* Actions */}
        <div className="flex flex-wrap gap-1.5 mt-auto pt-1">
          <Button size="sm" variant="secondary" onClick={onView}>
            <Eye size={12} className="mr-1" />
            {t('governance.customDashboards.viewDashboard', 'View')}
          </Button>
          {!dashboard.isSystem && (
            <Button size="sm" variant="secondary" onClick={onEdit} aria-label={t('governance.customDashboards.editDashboard', 'Edit')}>
              <Settings size={12} />
            </Button>
          )}
          <Button size="sm" variant="secondary" onClick={onClone} aria-label={t('governance.customDashboards.cloneDashboard', 'Clone')}>
            <Copy size={12} />
          </Button>
          {!dashboard.isSystem && (
            <Button
              size="sm"
              variant="secondary"
              onClick={onDelete}
              aria-label={t('governance.customDashboards.deleteDashboard', 'Delete')}
              className="text-red-500 hover:text-red-700"
            >
              <Trash2 size={12} />
            </Button>
          )}
        </div>
      </CardBody>
    </Card>
  );
}

// ── Component ──────────────────────────────────────────────────────────────

export function CustomDashboardsPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { activeEnvironmentId } = useEnvironment();
  const { user } = useAuth();
  const TENANT_ID = 'default';
  const USER_ID = user?.id ?? 'current-user';

  const { data, isLoading, isError, refetch } = useListDashboards(TENANT_ID, activeEnvironmentId);
  const createMutation = useCreateDashboard();
  const cloneMutation = useCloneDashboard();
  const deleteMutation = useDeleteDashboard();

  // Form state
  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [layout, setLayout] = useState<string>(LAYOUTS[0]);
  const [persona, setPersona] = useState<string>(PERSONAS[0]);
  const [selectedWidgets, setSelectedWidgets] = useState<string[]>([]);
  const [tagsInput, setTagsInput] = useState('');
  const [formError, setFormError] = useState<string | null>(null);
  const [formSuccess, setFormSuccess] = useState(false);

  // Filter/sort state
  const [searchQuery, setSearchQuery] = useState('');
  const [sortBy, setSortBy] = useState<'name' | 'persona' | 'widgetCount' | 'createdAt'>('name');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('asc');
  const [personaFilter, setPersonaFilter] = useState('');
  const [tagFilter, setTagFilter] = useState('');
  const [showOnlyFavorites, setShowOnlyFavorites] = useState(false);

  // Favorites (localStorage)
  const [favorites, setFavorites] = useState<string[]>(getFavorites);

  // Template picker
  const [templatePickerOpen, setTemplatePickerOpen] = useState(false);

  // Clone dialog
  const [cloneTargetId, setCloneTargetId] = useState<string | null>(null);
  const [cloneName, setCloneName] = useState('');
  const [cloneError, setCloneError] = useState<string | null>(null);

  // Delete confirm
  const [deleteTargetId, setDeleteTargetId] = useState<string | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const toggleWidget = (widgetId: string) => {
    setSelectedWidgets((prev) =>
      prev.includes(widgetId) ? prev.filter((w) => w !== widgetId) : [...prev, widgetId],
    );
  };

  const applyTemplate = (tpl: TemplatePreview) => {
    setName(t(tpl.titleKey, tpl.persona));
    setLayout(tpl.layout);
    setPersona(tpl.persona);
    setSelectedWidgets(tpl.widgets as string[]);
    setFormSuccess(false);
    setFormError(null);
  };

  const handleFavoriteToggle = (id: string) => {
    setFavorites(toggleFavorite(id));
  };

  const handleCloneOpen = (dashboardId: string, dashboardName: string) => {
    setCloneTargetId(dashboardId);
    setCloneName(`${dashboardName} (copy)`);
    setCloneError(null);
  };

  const handleCloneConfirm = async () => {
    if (!cloneTargetId) return;
    setCloneError(null);
    try {
      await cloneMutation.mutateAsync({ dashboardId: cloneTargetId, newName: cloneName, userId: USER_ID });
      setCloneTargetId(null);
      setCloneName('');
    } catch {
      setCloneError(t('governance.customDashboards.cloneError', 'Failed to clone dashboard.'));
    }
  };

  const handleDeleteConfirm = async () => {
    if (!deleteTargetId) return;
    setDeleteError(null);
    try {
      await deleteMutation.mutateAsync(deleteTargetId);
      setDeleteTargetId(null);
    } catch {
      setDeleteError(t('governance.customDashboards.deleteError', 'Failed to delete dashboard.'));
    }
  };

  const buildWidgetInputs = (widgetIds: string[]): WidgetInput[] =>
    widgetIds.map((id, index) => {
      const size = WIDGET_DEFAULT_SIZE[id] ?? { w: 2, h: 2 };
      return { type: id, posX: 0, posY: index * size.h, width: size.w, height: size.h };
    });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    setFormSuccess(false);
    const tags = tagsInput.split(',').map((t) => t.trim().toLowerCase()).filter(Boolean);
    try {
      await createMutation.mutateAsync({
        tenantId: TENANT_ID, userId: USER_ID, name, description, layout,
        widgets: buildWidgetInputs(selectedWidgets), persona,
        tags: tags.length > 0 ? tags : undefined,
      });
      setFormSuccess(true);
      setName(''); setDescription(''); setLayout(LAYOUTS[0]);
      setPersona(PERSONAS[0]); setSelectedWidgets([]); setTagsInput('');
    } catch {
      setFormError(t('governance.customDashboards.createError', 'Failed to create dashboard.'));
    }
  };

  if (isLoading) return <PageLoadingState message={t('governance.customDashboards.loading', 'Loading dashboards...')} />;
  if (isError) return <PageErrorState message={t('governance.customDashboards.error', 'Failed to load dashboards')} onRetry={() => refetch()} />;

  // Collect all unique tags from all dashboards
  const allTags = Array.from(
    new Set((data?.items ?? []).flatMap((d) => d.tags ?? []))
  ).sort();

  // Filter & sort
  const filteredDashboards = (data?.items ?? [])
    .filter((d) => {
      if (searchQuery && !d.name.toLowerCase().includes(searchQuery.toLowerCase())) return false;
      if (personaFilter && d.persona !== personaFilter) return false;
      if (tagFilter && !(d.tags ?? []).includes(tagFilter)) return false;
      if (showOnlyFavorites && !favorites.includes(d.dashboardId)) return false;
      return true;
    })
    .sort((a, b) => {
      let cmp = 0;
      if (sortBy === 'name') cmp = a.name.localeCompare(b.name);
      else if (sortBy === 'persona') cmp = a.persona.localeCompare(b.persona);
      else if (sortBy === 'widgetCount') cmp = a.widgetCount - b.widgetCount;
      else if (sortBy === 'createdAt') cmp = new Date(a.createdAt).getTime() - new Date(b.createdAt).getTime();
      return sortDir === 'asc' ? cmp : -cmp;
    });

  const favoriteDashboards = filteredDashboards.filter((d) => favorites.includes(d.dashboardId));
  const otherDashboards = filteredDashboards.filter((d) => !favorites.includes(d.dashboardId));

  const hasActiveFilters = searchQuery || personaFilter || tagFilter || showOnlyFavorites;

  return (
    <PageContainer>
      <PageHeader
        title={t('governance.customDashboards.title', 'Custom Dashboards')}
        subtitle={t('governance.customDashboards.subtitle', 'Build and manage personalized dashboard views')}
        icon={<LayoutDashboard size={24} />}
      />

      {/* Template Picker */}
      <DashboardTemplatePicker
        open={templatePickerOpen}
        onClose={() => setTemplatePickerOpen(false)}
        onSelect={(tpl) => { applyTemplate(tpl); setTemplatePickerOpen(false); }}
      />

      {/* Clone Dialog */}
      {cloneTargetId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-white dark:bg-gray-900 rounded-lg shadow-xl w-full max-w-sm p-6">
            <h2 className="text-base font-semibold text-gray-900 dark:text-white mb-3">
              {t('governance.customDashboards.cloneTitle', 'Clone Dashboard')}
            </h2>
            <input
              type="text"
              value={cloneName}
              onChange={(e) => setCloneName(e.target.value)}
              maxLength={100}
              className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-3 py-2 text-gray-900 dark:text-white mb-3"
              autoFocus
            />
            {cloneError && <p className="text-sm text-red-600 dark:text-red-400 mb-2">{cloneError}</p>}
            <div className="flex gap-2 justify-end">
              <Button variant="secondary" size="sm" onClick={() => setCloneTargetId(null)}>
                {t('common.cancel', 'Cancel')}
              </Button>
              <Button size="sm" onClick={handleCloneConfirm} disabled={cloneMutation.isPending || !cloneName.trim()}>
                {t('governance.customDashboards.cloneDashboard', 'Clone')}
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Delete Confirm */}
      {deleteTargetId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-white dark:bg-gray-900 rounded-lg shadow-xl w-full max-w-sm p-6">
            <h2 className="text-base font-semibold text-gray-900 dark:text-white mb-2">
              {t('governance.customDashboards.confirmDeleteTitle', 'Delete Dashboard')}
            </h2>
            <p className="text-sm text-gray-600 dark:text-gray-400 mb-4">
              {t('governance.customDashboards.confirmDelete', 'This action cannot be undone. Are you sure?')}
            </p>
            {deleteError && <p className="text-sm text-red-600 dark:text-red-400 mb-2">{deleteError}</p>}
            <div className="flex gap-2 justify-end">
              <Button variant="secondary" size="sm" onClick={() => setDeleteTargetId(null)}>
                {t('common.cancel', 'Cancel')}
              </Button>
              <Button
                size="sm"
                onClick={handleDeleteConfirm}
                disabled={deleteMutation.isPending}
                className="bg-red-600 hover:bg-red-700 text-white"
              >
                {t('governance.customDashboards.deleteDashboard', 'Delete')}
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Create Dashboard Form */}
      <PageSection title={t('governance.customDashboards.createDashboard', 'Create Dashboard')}>
        <Card>
          <CardBody>
            <div className="flex justify-end mb-3">
              <Button size="sm" variant="secondary" onClick={() => setTemplatePickerOpen(true)}>
                <LayoutIcon size={14} className="mr-1" />
                {t('governance.customDashboards.fromTemplate', 'Use Template')}
              </Button>
            </div>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    {t('governance.customDashboards.dashboardName', 'Name')}
                  </label>
                  <input
                    type="text"
                    value={name}
                    onChange={(e) => setName(e.target.value)}
                    required
                    maxLength={100}
                    className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-3 py-2 text-gray-900 dark:text-white"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    {t('governance.customDashboards.tags', 'Tags')} <span className="text-gray-400 font-normal">(comma-separated)</span>
                  </label>
                  <div className="relative">
                    <Tag size={13} className="absolute left-2.5 top-1/2 -translate-y-1/2 text-gray-400" />
                    <input
                      type="text"
                      value={tagsInput}
                      onChange={(e) => setTagsInput(e.target.value)}
                      placeholder="ops, sre, team-alpha"
                      className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm pl-8 pr-3 py-2 text-gray-900 dark:text-white"
                    />
                  </div>
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  {t('governance.customDashboards.description', 'Description')}
                </label>
                <textarea
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  rows={2}
                  className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-3 py-2 text-gray-900 dark:text-white"
                />
              </div>

              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    {t('governance.customDashboards.layout', 'Layout')}
                  </label>
                  <select
                    value={layout}
                    onChange={(e) => setLayout(e.target.value)}
                    className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-3 py-2 text-gray-900 dark:text-white"
                  >
                    {LAYOUTS.map((l) => (
                      <option key={l} value={l}>{l}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    {t('governance.customDashboards.persona', 'Persona')}
                  </label>
                  <select
                    value={persona}
                    onChange={(e) => setPersona(e.target.value)}
                    className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-3 py-2 text-gray-900 dark:text-white"
                  >
                    {PERSONAS.map((p) => <option key={p} value={p}>{p}</option>)}
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  {t('governance.customDashboards.selectWidgets', 'Starter Widgets')}
                </label>
                <div className="grid grid-cols-2 gap-2 sm:grid-cols-4">
                  {WIDGET_IDS.map((widgetId) => (
                    <label
                      key={widgetId}
                      className="flex items-center gap-2 rounded border border-gray-200 dark:border-gray-700 px-3 py-2 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-900/20"
                    >
                      <input
                        type="checkbox"
                        checked={selectedWidgets.includes(widgetId)}
                        onChange={() => toggleWidget(widgetId)}
                        className="rounded"
                      />
                      <span className="text-xs text-gray-700 dark:text-gray-300">
                        {t(`governance.customDashboards.widgets.${WIDGET_KEY_MAP[widgetId] ?? widgetId}`, widgetId)}
                      </span>
                    </label>
                  ))}
                </div>
              </div>

              {formError && <p className="text-sm text-red-600 dark:text-red-400">{formError}</p>}
              {formSuccess && (
                <p className="text-sm text-green-600 dark:text-green-400">
                  {t('governance.customDashboards.createSuccess', 'Dashboard created successfully!')}
                </p>
              )}

              <Button type="submit" disabled={createMutation.isPending}>
                <Plus size={14} className="mr-1" />
                {t('governance.customDashboards.submit', 'Create Dashboard')}
              </Button>
            </form>
          </CardBody>
        </Card>
      </PageSection>

      {/* Dashboard List */}
      <PageSection title={`${t('governance.customDashboards.title', 'Dashboards')} (${data?.totalCount ?? 0})`}>

        {/* Filter toolbar */}
        <div className="mb-4 flex flex-col gap-3">
          {/* Search */}
          <div className="flex gap-2 flex-wrap items-center">
            <div className="relative flex-1 min-w-48">
              <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
              <input
                type="search"
                value={searchQuery}
                onChange={(e) => setSearchQuery(e.target.value)}
                placeholder={t('governance.customDashboards.searchPlaceholder', 'Search by name…')}
                className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm pl-8 pr-3 py-2 text-gray-900 dark:text-white"
              />
            </div>

            {/* Favorites toggle */}
            <button
              onClick={() => setShowOnlyFavorites((v) => !v)}
              className={`flex items-center gap-1.5 rounded border px-3 py-2 text-xs transition-colors ${
                showOnlyFavorites
                  ? 'border-yellow-500 bg-yellow-50 dark:bg-yellow-900/20 text-yellow-700 dark:text-yellow-400'
                  : 'border-gray-300 dark:border-gray-600 text-gray-600 dark:text-gray-400 hover:border-accent/50'
              }`}
            >
              <Star size={13} fill={showOnlyFavorites ? 'currentColor' : 'none'} />
              {t('governance.customDashboards.favorites', 'Favorites')}
            </button>

            {/* Clear filters */}
            {hasActiveFilters && (
              <button
                onClick={() => { setSearchQuery(''); setPersonaFilter(''); setTagFilter(''); setShowOnlyFavorites(false); }}
                className="flex items-center gap-1 text-xs text-gray-500 dark:text-gray-400 hover:text-red-500 transition-colors"
              >
                <X size={12} />
                {t('governance.customDashboards.clearFilters', 'Clear')}
              </button>
            )}
          </div>

          {/* Persona chips */}
          <div className="flex flex-wrap items-center gap-2">
            <span className="flex items-center gap-1 text-xs text-gray-500 dark:text-gray-400 shrink-0">
              <Filter size={11} />
              {t('governance.customDashboards.filterByPersona', 'Persona')}:
            </span>
            <button
              onClick={() => setPersonaFilter('')}
              className={`rounded-full px-2.5 py-0.5 text-xs transition-colors ${!personaFilter ? 'bg-accent text-white' : 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 hover:bg-accent/20'}`}
            >
              {t('governance.customDashboards.allPersonas', 'All')}
            </button>
            {PERSONAS.map((p) => (
              <button
                key={p}
                onClick={() => setPersonaFilter(personaFilter === p ? '' : p)}
                className={`rounded-full px-2.5 py-0.5 text-xs transition-colors ${personaFilter === p ? 'bg-accent text-white' : 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 hover:bg-accent/20'}`}
              >
                {p}
              </button>
            ))}
          </div>

          {/* Tag chips */}
          {allTags.length > 0 && (
            <div className="flex flex-wrap items-center gap-2">
              <span className="flex items-center gap-1 text-xs text-gray-500 dark:text-gray-400 shrink-0">
                <Tag size={11} />
                {t('governance.customDashboards.filterByTag', 'Tag')}:
              </span>
              {allTags.map((tag) => (
                <button
                  key={tag}
                  onClick={() => setTagFilter(tagFilter === tag ? '' : tag)}
                  className={`rounded-full px-2.5 py-0.5 text-xs transition-colors ${tagFilter === tag ? 'bg-accent text-white' : 'bg-gray-100 dark:bg-gray-800 text-gray-600 dark:text-gray-400 hover:bg-accent/20'}`}
                >
                  #{tag}
                </button>
              ))}
            </div>
          )}

          {/* Sort controls */}
          <div className="flex items-center gap-2 flex-wrap">
            <span className="flex items-center gap-1 text-xs text-gray-500 dark:text-gray-400">
              <ArrowUpDown size={11} />
              {t('governance.customDashboards.sortBy', 'Sort')}:
            </span>
            {(['name', 'persona', 'widgetCount', 'createdAt'] as const).map((field) => (
              <button
                key={field}
                onClick={() => {
                  if (sortBy === field) setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'));
                  else { setSortBy(field); setSortDir('asc'); }
                }}
                className={`text-xs rounded px-2 py-1 border transition-colors ${sortBy === field ? 'border-accent bg-accent/10 text-accent font-semibold' : 'border-gray-200 dark:border-gray-700 text-gray-600 dark:text-gray-400 hover:border-accent/50'}`}
              >
                {t(`governance.customDashboards.sortField.${field}`, field)}
                {sortBy === field && <span className="ml-0.5">{sortDir === 'asc' ? ' ↑' : ' ↓'}</span>}
              </button>
            ))}
          </div>
        </div>

        {filteredDashboards.length === 0 ? (
          <EmptyState
            title={hasActiveFilters ? t('governance.customDashboards.noResults', 'No dashboards match your filters') : t('governance.customDashboards.empty', 'No dashboards yet')}
            description={hasActiveFilters ? t('governance.customDashboards.noResultsHint', 'Try adjusting or clearing your filters.') : t('governance.customDashboards.emptyDescription', 'Create a dashboard above to get started.')}
            size="compact"
          />
        ) : (
          <>
            {/* Favorites section */}
            {favoriteDashboards.length > 0 && !showOnlyFavorites && (
              <div className="mb-6">
                <h3 className="flex items-center gap-1.5 text-xs font-semibold text-yellow-600 dark:text-yellow-400 mb-3 uppercase tracking-wide">
                  <Star size={12} fill="currentColor" />
                  {t('governance.customDashboards.favoritesSection', 'Favorites')}
                </h3>
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
                  {favoriteDashboards.map((d) => (
                    <DashboardCard
                      key={d.dashboardId}
                      dashboard={d}
                      isFavorite
                      onFavoriteToggle={() => handleFavoriteToggle(d.dashboardId)}
                      onView={() => navigate(`/governance/dashboards/${d.dashboardId}`)}
                      onEdit={() => navigate(`/governance/dashboards/${d.dashboardId}/edit`)}
                      onClone={() => handleCloneOpen(d.dashboardId, d.name)}
                      onDelete={() => { setDeleteTargetId(d.dashboardId); setDeleteError(null); }}
                    />
                  ))}
                </div>
              </div>
            )}

            {/* All / non-favorite dashboards */}
            {(showOnlyFavorites ? filteredDashboards : otherDashboards).length > 0 && (
              <>
                {favoriteDashboards.length > 0 && !showOnlyFavorites && (
                  <h3 className="text-xs font-semibold text-gray-500 dark:text-gray-400 mb-3 uppercase tracking-wide">
                    {t('governance.customDashboards.allDashboards', 'All Dashboards')}
                  </h3>
                )}
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
                  {(showOnlyFavorites ? filteredDashboards : otherDashboards).map((d) => (
                    <DashboardCard
                      key={d.dashboardId}
                      dashboard={d}
                      isFavorite={favorites.includes(d.dashboardId)}
                      onFavoriteToggle={() => handleFavoriteToggle(d.dashboardId)}
                      onView={() => navigate(`/governance/dashboards/${d.dashboardId}`)}
                      onEdit={() => navigate(`/governance/dashboards/${d.dashboardId}/edit`)}
                      onClone={() => handleCloneOpen(d.dashboardId, d.name)}
                      onDelete={() => { setDeleteTargetId(d.dashboardId); setDeleteError(null); }}
                    />
                  ))}
                </div>
              </>
            )}
          </>
        )}
      </PageSection>
    </PageContainer>
  );
}
