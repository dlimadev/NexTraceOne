import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { useAuth } from '../../../contexts/AuthContext';
import { BarChart2, Plus, Trash2, Save } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { Modal } from '../../../components/Modal';
import { TextField } from '../../../components/TextField';
import { Select } from '../../../components/Select';
import { EmptyState } from '../../../components/EmptyState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import client from '../../../api/client';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

// ── Types ──────────────────────────────────────────────────────────────────

type ChartType = 'Line' | 'Bar' | 'Area' | 'Pie' | 'Gauge' | 'Table' | 'Sparkline';
type MetricSource = 'changes' | 'incidents' | 'contracts' | 'services' | 'slos' | 'finops';
type TimeRange = 'last_1h' | 'last_6h' | 'last_24h' | 'last_7d' | 'last_30d' | 'last_90d';

interface ChartSummary {
  chartId: string;
  name: string;
  chartType: ChartType;
  timeRange: TimeRange;
  isShared: boolean;
  createdAt: string;
}

interface ListChartsResponse {
  items: ChartSummary[];
  totalCount: number;
}

// ── Constants ──────────────────────────────────────────────────────────────

const CHART_TYPES: ChartType[] = ['Line', 'Bar', 'Area', 'Pie', 'Gauge', 'Table', 'Sparkline'];
const METRIC_SOURCES: MetricSource[] = ['changes', 'incidents', 'contracts', 'services', 'slos', 'finops'];
const TIME_RANGES: TimeRange[] = ['last_1h', 'last_6h', 'last_24h', 'last_7d', 'last_30d', 'last_90d'];

const CHART_TYPE_COLORS: Record<ChartType, 'primary' | 'secondary' | 'success' | 'warning'> = {
  Line: 'primary',
  Bar: 'secondary',
  Area: 'success',
  Pie: 'warning',
  Gauge: 'primary',
  Table: 'secondary',
  Sparkline: 'success',
};

// ── Hooks ──────────────────────────────────────────────────────────────────

const useListCharts = (userId: string, tenantId: string) => {
  const { activeEnvironmentId } = useEnvironment();
  return useQuery({
    queryKey: ['custom-charts', userId, tenantId, activeEnvironmentId],
    queryFn: () =>
      client
        .get<ListChartsResponse>('/custom-charts', { params: { userId, tenantId } })
        .then((r) => r.data),
  });
};

const useCreateChart = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: {
      tenantId: string;
      userId: string;
      name: string;
      chartType: number;
      metricQuery: string;
      timeRange: TimeRange;
      filtersJson: string | null;
    }) => client.post('/custom-charts', data).then((r) => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['custom-charts'] }),
  });
};

const useDeleteChart = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ chartId, tenantId }: { chartId: string; tenantId: string }) =>
      client.delete(`/custom-charts/${chartId}`, { params: { tenantId } }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['custom-charts'] }),
  });
};

// ── Builder Steps ──────────────────────────────────────────────────────────

interface BuilderState {
  step: 1 | 2 | 3 | 4 | 5;
  name: string;
  source: MetricSource | null;
  chartType: ChartType | null;
  timeRange: TimeRange;
  groupBy: string;
  filters: string;
}

const initialBuilder: BuilderState = {
  step: 1,
  name: '',
  source: null,
  chartType: null,
  timeRange: 'last_24h',
  groupBy: '',
  filters: '',
};

// ── Component ──────────────────────────────────────────────────────────────

/** Página de criação e gestão de gráficos customizados. */
export function CustomChartBuilderPage() {
  const { t } = useTranslation();
  const { user, tenantId: authTenantId } = useAuth();
  const tenantId = authTenantId ?? 'default';
  const userId = user?.id ?? 'current';

  const { data, isLoading, isError } = useListCharts(userId, tenantId);
  const createChart = useCreateChart();
  const deleteChart = useDeleteChart();

  const [showBuilder, setShowBuilder] = useState(false);
  const [builder, setBuilder] = useState<BuilderState>(initialBuilder);

  if (isLoading) return <PageContainer><PageLoadingState /></PageContainer>;
  if (isError) return <PageContainer><PageErrorState /></PageContainer>;

  const chartTypeIndex = (ct: ChartType): number => CHART_TYPES.indexOf(ct);

  const handleCreate = () => {
    if (!builder.source || !builder.chartType || !builder.name) return;
    const metricQuery = JSON.stringify({
      source: builder.source,
      groupBy: builder.groupBy || null,
    });
    createChart.mutate({
      tenantId,
      userId,
      name: builder.name,
      chartType: chartTypeIndex(builder.chartType),
      metricQuery,
      timeRange: builder.timeRange,
      filtersJson: builder.filters ? builder.filters : null,
    }, {
      onSuccess: () => {
        setShowBuilder(false);
        setBuilder(initialBuilder);
      },
    });
  };

  const canAdvance = () => {
    if (builder.step === 1) return !!builder.source;
    if (builder.step === 2) return !!builder.chartType;
    if (builder.step === 5) return !!builder.name;
    return true;
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('customCharts.title')}
        subtitle={t('customCharts.subtitle')}
        actions={
          <Button size="sm" onClick={() => setShowBuilder(true)}>
            <Plus className="w-4 h-4 mr-1" />
            {t('customCharts.newChart')}
          </Button>
        }
      />

      <PageSection>
        {!data?.items.length ? (
          <EmptyState
            icon={<BarChart2 className="w-8 h-8 text-faded" />}
            title={t('customCharts.empty.title')}
            description={t('customCharts.empty.description')}
            action={
              <Button size="sm" onClick={() => setShowBuilder(true)}>
                {t('customCharts.empty.cta')}
              </Button>
            }
          />
        ) : (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {data.items.map((chart) => (
              <Card key={chart.chartId}>
                <CardHeader>
                  <div className="flex items-center justify-between">
                    <span className="font-medium text-sm text-heading">{chart.name}</span>
                    <Badge variant={CHART_TYPE_COLORS[chart.chartType]}>{chart.chartType}</Badge>
                  </div>
                </CardHeader>
                <CardBody>
                  <p className="text-xs text-muted mb-3">
                    {t('customCharts.timeRange')}: {chart.timeRange}
                  </p>
                  <div className="flex justify-end">
                    <Button
                      variant="outline"
                      size="sm"
                      onClick={() => deleteChart.mutate({ chartId: chart.chartId, tenantId })}
                    >
                      <Trash2 className="w-3 h-3" />
                    </Button>
                  </div>
                </CardBody>
              </Card>
            ))}
          </div>
        )}
      </PageSection>

      {/* Builder Modal */}
      <Modal
        open={showBuilder}
        onClose={() => { setShowBuilder(false); setBuilder(initialBuilder); }}
        title={t('customCharts.builder.title')}
        description={t('customCharts.builder.step', { step: builder.step, total: 5 })}
        footer={
          <>
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                if (builder.step === 1) { setShowBuilder(false); setBuilder(initialBuilder); }
                else setBuilder((b) => ({ ...b, step: (b.step - 1) as BuilderState['step'] }));
              }}
            >
              {builder.step === 1 ? t('common.cancel') : t('common.back')}
            </Button>
            {builder.step < 5 ? (
              <Button
                size="sm"
                disabled={!canAdvance()}
                onClick={() => setBuilder((b) => ({ ...b, step: (b.step + 1) as BuilderState['step'] }))}
              >
                {t('common.next')}
              </Button>
            ) : (
              <Button
                size="sm"
                disabled={!builder.name || createChart.isPending}
                loading={createChart.isPending}
                onClick={handleCreate}
              >
                <Save className="w-4 h-4 mr-1" />
                {t('customCharts.builder.save')}
              </Button>
            )}
          </>
        }
      >
        {builder.step === 1 && (
          <div>
            <p className="text-sm font-medium text-body mb-3">
              {t('customCharts.builder.chooseMetric')}
            </p>
            <div className="grid grid-cols-2 gap-2">
              {METRIC_SOURCES.map((src) => (
                <button
                  key={src}
                  type="button"
                  onClick={() => setBuilder((b) => ({ ...b, source: src }))}
                  className={`text-sm border rounded-lg p-3 text-left transition-colors ${
                    builder.source === src
                      ? 'border-accent bg-accent/10 text-accent'
                      : 'border-edge hover:border-accent text-body'
                  }`}
                >
                  {src}
                </button>
              ))}
            </div>
          </div>
        )}

        {builder.step === 2 && (
          <div>
            <p className="text-sm font-medium text-body mb-3">
              {t('customCharts.builder.chooseChartType')}
            </p>
            <div className="grid grid-cols-2 gap-2">
              {CHART_TYPES.map((ct) => (
                <button
                  key={ct}
                  type="button"
                  onClick={() => setBuilder((b) => ({ ...b, chartType: ct }))}
                  className={`text-sm border rounded-lg p-3 text-left transition-colors ${
                    builder.chartType === ct
                      ? 'border-accent bg-accent/10 text-accent'
                      : 'border-edge hover:border-accent text-body'
                  }`}
                >
                  {ct}
                </button>
              ))}
            </div>
          </div>
        )}

        {builder.step === 3 && (
          <div className="space-y-4">
            <p className="text-sm font-medium text-body">
              {t('customCharts.builder.defineFilters')}
            </p>
            <TextField
              size="sm"
              label={t('customCharts.builder.groupBy')}
              value={builder.groupBy}
              onChange={(e) => setBuilder((b) => ({ ...b, groupBy: e.target.value }))}
              placeholder={t('customCharts.builder.groupByPlaceholder')}
            />
            <Select
              size="sm"
              label={t('customCharts.builder.timeRange')}
              value={builder.timeRange}
              onChange={(e) => setBuilder((b) => ({ ...b, timeRange: e.target.value as TimeRange }))}
              options={TIME_RANGES.map((tr) => ({ value: tr, label: tr }))}
            />
          </div>
        )}

        {builder.step === 4 && (
          <div>
            <p className="text-sm font-medium text-body mb-3">
              {t('customCharts.builder.preview')}
            </p>
            <div className="border border-dashed border-edge rounded-lg p-8 flex items-center justify-center">
              <div className="text-center text-faded">
                <BarChart2 className="w-10 h-10 mx-auto mb-2" />
                <p className="text-sm">{builder.source} / {builder.chartType} / {builder.timeRange}</p>
              </div>
            </div>
          </div>
        )}

        {builder.step === 5 && (
          <TextField
            size="sm"
            label={t('customCharts.builder.saveName')}
            value={builder.name}
            onChange={(e) => setBuilder((b) => ({ ...b, name: e.target.value }))}
            placeholder={t('customCharts.builder.namePlaceholder')}
            autoFocus
          />
        )}
      </Modal>
    </PageContainer>
  );
}
