import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { LayoutDashboard, Plus } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

interface DashboardSummary {
  dashboardId: string;
  name: string;
  persona: string;
  widgetCount: number;
  layout: string;
  isShared: boolean;
  createdAt: string;
}

interface ListDashboardsResponse {
  items: DashboardSummary[];
  totalCount: number;
}

interface CreateDashboardRequest {
  tenantId: string;
  userId: string;
  name: string;
  description: string;
  layout: string;
  widgetIds: string[];
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

const useListDashboards = (tenantId: string) =>
  useQuery({
    queryKey: ['governance-dashboards', tenantId],
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
  const TENANT_ID = 'default';

  const { data, isLoading, isError, refetch } = useListDashboards(TENANT_ID);
  const createMutation = useCreateDashboard();

  const [name, setName] = useState('');
  const [description, setDescription] = useState('');
  const [layout, setLayout] = useState<string>(LAYOUTS[0]);
  const [persona, setPersona] = useState<string>(PERSONAS[0]);
  const [selectedWidgets, setSelectedWidgets] = useState<string[]>([]);
  const [formError, setFormError] = useState<string | null>(null);
  const [formSuccess, setFormSuccess] = useState(false);

  const toggleWidget = (widgetId: string) => {
    setSelectedWidgets((prev) =>
      prev.includes(widgetId) ? prev.filter((w) => w !== widgetId) : [...prev, widgetId],
    );
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    setFormSuccess(false);

    try {
      await createMutation.mutateAsync({
        tenantId: TENANT_ID,
        userId: 'current-user',
        name,
        description,
        layout,
        widgetIds: selectedWidgets,
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

  return (
    <PageContainer>
      <PageHeader
        title={t('governance.customDashboards.title')}
        subtitle={t('governance.customDashboards.subtitle')}
        icon={<LayoutDashboard size={24} />}
      />

      {/* Create Dashboard Form */}
      <PageSection title={t('governance.customDashboards.createDashboard')}>
        <Card>
          <CardBody>
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
        {data?.items.length === 0 ? (
          <EmptyState
            title={t('governance.customDashboards.empty', 'No dashboards yet')}
            description={t('governance.customDashboards.emptyDescription', 'Create a custom dashboard using the form above to get started.')}
            size="compact"
          />
        ) : (
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 lg:grid-cols-3">
            {data?.items.map((dashboard) => (
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
                  </div>
                  <Button size="sm" variant="secondary" onClick={() => undefined}>
                    {t('governance.customDashboards.viewDashboard')}
                  </Button>
                </CardBody>
              </Card>
            ))}
          </div>
        )}
      </PageSection>
    </PageContainer>
  );
}
