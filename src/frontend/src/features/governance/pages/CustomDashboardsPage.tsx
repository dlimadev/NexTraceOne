import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { LayoutDashboard, Plus, Eye, Settings, Copy, Trash2, Layout as LayoutIcon, Search, ArrowUpDown } from 'lucide-react';
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
}

const LAYOUTS = [
  'single-column',
  'two-column',
  'three-column',
  'grid',
  'custom',
] as const;

const PERSONAS = [
  'Engineer',
  'TechLead',
  'Architect',
  'Product',
  'Executive',
  'PlatformAdmin',
  'Auditor',
] as const;

const WIDGET_IDS = [
  'dora-metrics',
  'service-scorecard',
  'incident-summary',
  'change-confidence',
  'cost-trend',
  'reliability-slo',
  'knowledge-graph',
  'on-call-status',
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

// ── Hooks ──────────────────────────────────────────────────────────────────

const useListDashboards = (tenantId: string, envId?: string | null) =>
  useQuery({
    queryKey: ['governance-dashboards', tenantId, envId],
    queryFn: () =>
      client
        .get<ListDashboardsResponse>('/governance/dashboards', {
          params: { tenantId, page: 1, pageSize: 20 },
        })
        .then((r) => r.data),
  });

const useCreateDashboard = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: CreateDashboardRequest) =>
      client.post('/governance/dashboards', data).then((r) => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['governance-dashboards'] });
    },
  });
};

const useCloneDashboard = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ dashboardId, newName, userId }: { dashboardId: string; newName: string; userId: string }) =>
      client
        .post(`/governance/dashboards/${dashboardId}/clone`, {
          newName,
          tenantId: 'default',
          userId,
          sourceDashboardId: dashboardId,
        })
        .then((r) => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['governance-dashboards'] });
    },
  });
};

const useDeleteDashboard = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (dashboardId: string) =>
      client
        .delete(`/governance/dashboards/${dashboardId}`, { params: { tenantId: 'default' } })
        .then((r) => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['governance-dashboards'] });
    },
  });
};

// ── Widget label key map ───────────────────────────────────────────────────

const WIDGET_KEY_MAP: Record<string, string> = {
  'dora-metrics': 'doraMetrics',
  'service-scorecard': 'serviceScorecard',
  'incident-summary': 'incidentSummary',
  'change-confidence': 'changeConfidence',
  'cost-trend': 'costTrend',
  'reliability-slo': 'reliabilitySlo',
  'knowledge-graph': 'knowledgeGraph',
  'on-call-status': 'onCallStatus',
};

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

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [layout, setLayout] = useState<string>(LAYOUTS[0]);
  const [persona, setPersona] = useState<string>(PERSONAS[0]);
  const [selectedWidgets, setSelectedWidgets] = useState<string[]>([]);
  const [formError, setFormError] = useState<string | null>(null);
  const [formSuccess, setFormSuccess] = useState(false);
  const [searchQuery, setSearchQuery] = useState('');
  const [sortBy, setSortBy] = useState<'name' | 'persona' | 'widgetCount'>('name');
  const [sortDir, setSortDir] = useState<'asc' | 'desc'>('asc');

  // Template picker
  const [templatePickerOpen, setTemplatePickerOpen] = useState(false);

  // Clone dialog state
  const [cloneTargetId, setCloneTargetId] = useState<string | null>(null);
  const [cloneName, setCloneName] = useState('');
  const [cloneError, setCloneError] = useState<string | null>(null);

  // Delete confirm state
  const [deleteTargetId, setDeleteTargetId] = useState<string | null>(null);
  const [deleteError, setDeleteError] = useState<string | null>(null);

  const toggleWidget = (widgetId: string) => {
    setSelectedWidgets((prev) =>
      prev.includes(widgetId) ? prev.filter((w) => w !== widgetId) : [...prev, widgetId],
    );
  };

  /** Apply a template to the create form */
  const applyTemplate = (tpl: TemplatePreview) => {
    setName(t(tpl.titleKey, tpl.persona));
    setLayout(tpl.layout);
    setPersona(tpl.persona);
    setSelectedWidgets(tpl.widgets as string[]);
    setFormSuccess(false);
    setFormError(null);
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

  const handleDeleteOpen = (dashboardId: string) => {
    setDeleteTargetId(dashboardId);
    setDeleteError(null);
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

  /** Build widget inputs with automatic grid positions */
  const buildWidgetInputs = (widgetIds: string[]): WidgetInput[] =>
    widgetIds.map((id, index) => {
      const size = WIDGET_DEFAULT_SIZE[id] ?? { w: 2, h: 2 };
      return {
        type: id,
        posX: 0,
        posY: index * size.h,
        width: size.w,
        height: size.h,
      };
    });

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    setFormSuccess(false);

    try {
      await createMutation.mutateAsync({
        tenantId: TENANT_ID,
        userId: USER_ID,
        name,
        description,
        layout,
        widgets: buildWidgetInputs(selectedWidgets),
        persona,
      });
      setFormSuccess(true);
      setName('');
      setDescription('');
      setLayout(LAYOUTS[0]);
      setPersona(PERSONAS[0]);
      setSelectedWidgets([]);
    } catch {
      setFormError(t('governance.customDashboards.createError'));
    }
  };

  if (isLoading) return <PageLoadingState message={t('governance.customDashboards.loading')} />;
  if (isError)
    return <PageErrorState message={t('governance.customDashboards.error')} onRetry={() => refetch()} />;

  const filteredDashboards = (data?.items ?? [])
    .filter((d) => !searchQuery || d.name.toLowerCase().includes(searchQuery.toLowerCase()))
    .sort((a, b) => {
      let cmp = 0;
      if (sortBy === 'name') {
        cmp = a.name.localeCompare(b.name);
      } else if (sortBy === 'persona') {
        cmp = a.persona.localeCompare(b.persona);
      } else if (sortBy === 'widgetCount') {
        cmp = a.widgetCount - b.widgetCount;
      }
      return sortDir === 'asc' ? cmp : -cmp;
    });

  return (
    <PageContainer>
      <PageHeader
        title={t('governance.customDashboards.title')}
        subtitle={t('governance.customDashboards.subtitle')}
        icon={<LayoutDashboard size={24} />}
      />

      {/* Template Picker Modal */}
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
            <label className="block text-sm text-gray-700 dark:text-gray-300 mb-1">
              {t('governance.customDashboards.cloneName', 'New Dashboard Name')}
            </label>
            <input
              type="text"
              value={cloneName}
              onChange={(e) => setCloneName(e.target.value)}
              maxLength={100}
              className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-3 py-2 text-gray-900 dark:text-white mb-3"
              autoFocus
            />
            {cloneError && (
              <p className="text-sm text-red-600 dark:text-red-400 mb-2">{cloneError}</p>
            )}
            <div className="flex gap-2 justify-end">
              <Button variant="secondary" size="sm" onClick={() => setCloneTargetId(null)}>
                {t('common.cancel', 'Cancel')}
              </Button>
              <Button
                size="sm"
                onClick={handleCloneConfirm}
                disabled={cloneMutation.isPending || !cloneName.trim()}
              >
                {t('governance.customDashboards.cloneDashboard', 'Clone')}
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Delete Confirm Dialog */}
      {deleteTargetId && (
        <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50">
          <div className="bg-white dark:bg-gray-900 rounded-lg shadow-xl w-full max-w-sm p-6">
            <h2 className="text-base font-semibold text-gray-900 dark:text-white mb-2">
              {t('governance.customDashboards.confirmDeleteTitle', 'Delete Dashboard')}
            </h2>
            <p className="text-sm text-gray-600 dark:text-gray-400 mb-4">
              {t('governance.customDashboards.confirmDelete', 'This action cannot be undone. Are you sure you want to delete this dashboard?')}
            </p>
            {deleteError && (
              <p className="text-sm text-red-600 dark:text-red-400 mb-2">{deleteError}</p>
            )}
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
      <PageSection title={t('governance.customDashboards.createDashboard')}>
        <Card>
          <CardBody>
            <div className="flex justify-end mb-3">
              <Button
                size="sm"
                variant="secondary"
                onClick={() => setTemplatePickerOpen(true)}
                aria-label={t('governance.customDashboards.fromTemplate', 'Use Template')}
              >
                <LayoutIcon size={14} className="mr-1" />
                {t('governance.customDashboards.fromTemplate', 'Use Template')}
              </Button>
            </div>
            <form onSubmit={handleSubmit} className="space-y-4">
              {/* Name */}
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  {t('governance.customDashboards.dashboardName')}
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

              {/* Description */}
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  {t('governance.customDashboards.description')}
                </label>
                <textarea
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  rows={2}
                  className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-3 py-2 text-gray-900 dark:text-white"
                />
              </div>

              {/* Layout + Persona */}
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    {t('governance.customDashboards.layout')}
                  </label>
                  <select
                    value={layout}
                    onChange={(e) => setLayout(e.target.value)}
                    className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-3 py-2 text-gray-900 dark:text-white"
                  >
                    {LAYOUTS.map((l) => (
                      <option key={l} value={l}>
                        {t(`governance.customDashboards.${l.replace(/-([a-z])/g, (_, c) => c.toUpperCase())}`)}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    {t('governance.customDashboards.persona')}
                  </label>
                  <select
                    value={persona}
                    onChange={(e) => setPersona(e.target.value)}
                    className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-3 py-2 text-gray-900 dark:text-white"
                  >
                    {PERSONAS.map((p) => (
                      <option key={p} value={p}>
                        {p}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              {/* Widgets */}
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  {t('governance.customDashboards.selectWidgets')}
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
                        {t(`governance.customDashboards.widgets.${WIDGET_KEY_MAP[widgetId]}`)}
                      </span>
                    </label>
                  ))}
                </div>
              </div>

              {formError && (
                <p className="text-sm text-red-600 dark:text-red-400">{formError}</p>
              )}
              {formSuccess && (
                <p className="text-sm text-green-600 dark:text-green-400">
                  {t('governance.customDashboards.createSuccess')}
                </p>
              )}

              <Button type="submit" disabled={createMutation.isPending}>
                <Plus size={14} className="mr-1" />
                {t('governance.customDashboards.submit')}
              </Button>
            </form>
          </CardBody>
        </Card>
      </PageSection>

      {/* Dashboard List */}
      <PageSection
        title={`${t('governance.customDashboards.title')} (${data?.totalCount ?? 0})`}
      >
        {/* Search bar */}
        <div className="mb-4 relative">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" aria-hidden="true" />
          <input
            type="search"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder={t('governance.customDashboards.searchPlaceholder', 'Search dashboards by name…')}
            className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm pl-8 pr-3 py-2 text-gray-900 dark:text-white"
            aria-label={t('governance.customDashboards.searchLabel', 'Search dashboards')}
          />
        </div>

        {/* Sort controls */}
        <div className="mb-4 flex items-center gap-2 flex-wrap">
          <span className="flex items-center gap-1 text-xs text-gray-500 dark:text-gray-400">
            <ArrowUpDown size={12} />
            {t('governance.customDashboards.sortBy', 'Sort by')}:
          </span>
          {(['name', 'persona', 'widgetCount'] as const).map((field) => (
            <button
              key={field}
              onClick={() => {
                if (sortBy === field) {
                  setSortDir((d) => (d === 'asc' ? 'desc' : 'asc'));
                } else {
                  setSortBy(field);
                  setSortDir('asc');
                }
              }}
              className={`text-xs rounded px-2 py-1 border transition-colors ${
                sortBy === field
                  ? 'border-accent bg-accent/10 text-accent font-semibold'
                  : 'border-gray-200 dark:border-gray-700 text-gray-600 dark:text-gray-400 hover:border-accent/50'
              }`}
              aria-pressed={sortBy === field}
            >
              {t(`governance.customDashboards.sortField.${field}`, field)}
              {sortBy === field && (
                <span className="ml-0.5">{sortDir === 'asc' ? ' ↑' : ' ↓'}</span>
              )}
            </button>
          ))}
        </div>

        {filteredDashboards.length === 0 ? (
          <EmptyState
            title={searchQuery ? t('governance.customDashboards.noResults', 'No dashboards match your search') : t('governance.customDashboards.empty', 'No dashboards yet')}
            description={searchQuery ? t('governance.customDashboards.noResultsHint', 'Try a different search term or clear the search.') : t('governance.customDashboards.emptyDescription', 'Create a custom dashboard using the form above to get started.')}
            size="compact"
          />
        ) : (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {filteredDashboards.map((dashboard) => (
              <Card key={dashboard.dashboardId}>
                <CardHeader className="pb-0">
                  <div className="flex items-start justify-between gap-2">
                    <h3 className="text-sm font-semibold text-gray-900 dark:text-white">
                      {dashboard.name}
                    </h3>
                    <Badge variant={PERSONA_VARIANT[dashboard.persona] ?? 'secondary'}>
                      {dashboard.persona}
                    </Badge>
                  </div>
                </CardHeader>
                <CardBody className="pt-2 space-y-2">
                  <div className="flex items-center gap-4 text-xs text-gray-500 dark:text-gray-400">
                    <span>{dashboard.layout}</span>
                    <span>
                      {dashboard.widgetCount} {t('governance.customDashboards.widgets')}
                    </span>
                    {dashboard.isShared && (
                      <Badge variant="secondary">{t('governance.dashboardView.shared', 'Shared')}</Badge>
                    )}
                    {dashboard.isSystem && (
                      <Badge variant="warning">{t('governance.dashboardView.system', 'System')}</Badge>
                    )}
                  </div>
                  <div className="flex flex-wrap gap-2">
                    <Button
                      size="sm"
                      variant="secondary"
                      onClick={() => navigate(`/governance/dashboards/${dashboard.dashboardId}`)}
                    >
                      <Eye size={12} className="mr-1" />
                      {t('governance.customDashboards.viewDashboard')}
                    </Button>
                    {!dashboard.isSystem && (
                      <Button
                        size="sm"
                        variant="secondary"
                        onClick={() => navigate(`/governance/dashboards/${dashboard.dashboardId}/edit`)}
                        aria-label={t('governance.customDashboards.editDashboard', 'Edit')}
                      >
                        <Settings size={12} />
                      </Button>
                    )}
                    <Button
                      size="sm"
                      variant="secondary"
                      onClick={() => handleCloneOpen(dashboard.dashboardId, dashboard.name)}
                      aria-label={t('governance.customDashboards.cloneDashboard', 'Clone')}
                    >
                      <Copy size={12} />
                    </Button>
                    {!dashboard.isSystem && (
                      <Button
                        size="sm"
                        variant="secondary"
                        onClick={() => handleDeleteOpen(dashboard.dashboardId)}
                        aria-label={t('governance.customDashboards.deleteDashboard', 'Delete')}
                        className="text-red-500 hover:text-red-700"
                      >
                        <Trash2 size={12} />
                      </Button>
                    )}
                  </div>
                </CardBody>
              </Card>
            ))}
          </div>
        )}
      </PageSection>
    </PageContainer>
  );
}
