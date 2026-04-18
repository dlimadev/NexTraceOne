/**
 * SloMarketplacePage — Marketplace de templates de SLO por tipo de serviço e preset de compliance.
 *
 * Biblioteca de templates SLO reutilizáveis por categoria (REST, Kafka, DB, Jobs)
 * com presets de compliance (LGPD, Financial SLA, GDPR) para aceleração da governança.
 *
 * @module operations/reliability
 * @pillar Contract Governance, Operational Reliability
 */
import * as React from 'react';
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { Store, RefreshCw, Download } from 'lucide-react';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { getSloTemplates, type SloTemplate, type SloTemplateCategory } from '../api/telemetry';

type TimeRange = '1h' | '6h' | '24h' | '7d';

const TIME_RANGE_OPTIONS: Array<{ value: TimeRange; labelKey: string }> = [
  { value: '1h', labelKey: 'sloMarketplace.timeRange.1h' },
  { value: '6h', labelKey: 'sloMarketplace.timeRange.6h' },
  { value: '24h', labelKey: 'sloMarketplace.timeRange.24h' },
  { value: '7d', labelKey: 'sloMarketplace.timeRange.7d' },
];

const CATEGORY_OPTIONS: Array<{ value: SloTemplateCategory | 'all'; labelKey: string }> = [
  { value: 'all', labelKey: 'sloMarketplace.categories.all' },
  { value: 'restApi', labelKey: 'sloMarketplace.categories.restApi' },
  { value: 'kafka', labelKey: 'sloMarketplace.categories.kafka' },
  { value: 'database', labelKey: 'sloMarketplace.categories.database' },
  { value: 'backgroundJob', labelKey: 'sloMarketplace.categories.backgroundJob' },
];

const FALLBACK: SloTemplate[] = [
  { id: '1', name: 'REST API Availability 99.9%', category: 'restApi', sliType: 'Availability', target: '99.9%', window: '30d', compliancePreset: 'internal', uses: 142, author: 'Platform Team' },
  { id: '2', name: 'REST API Latency p95 < 200ms', category: 'restApi', sliType: 'Latency', target: '200ms', window: '30d', compliancePreset: undefined, uses: 98, author: 'Platform Team' },
  { id: '3', name: 'Financial API SLA 99.99%', category: 'restApi', sliType: 'Availability', target: '99.99%', window: '30d', compliancePreset: 'financialSla', uses: 34, author: 'Compliance Team' },
  { id: '4', name: 'Kafka Consumer Lag < 1000', category: 'kafka', sliType: 'Throughput', target: '< 1000 messages', window: '1h', compliancePreset: undefined, uses: 56, author: 'Events Team' },
  { id: '5', name: 'Kafka Producer Availability 99.5%', category: 'kafka', sliType: 'Availability', target: '99.5%', window: '30d', compliancePreset: undefined, uses: 41, author: 'Events Team' },
  { id: '6', name: 'Database Query p95 < 100ms', category: 'database', sliType: 'Latency', target: '100ms', window: '24h', compliancePreset: undefined, uses: 87, author: 'Data Team' },
  { id: '7', name: 'LGPD Data Access Response Time', category: 'restApi', sliType: 'Latency', target: '72h', window: '30d', compliancePreset: 'lgpd', uses: 23, author: 'Legal & Compliance' },
  { id: '8', name: 'Background Job Success Rate 99%', category: 'backgroundJob', sliType: 'Success Rate', target: '99%', window: '24h', compliancePreset: undefined, uses: 65, author: 'Platform Team' },
];

export function SloMarketplacePage() {
  const { t } = useTranslation();
  const [timeRange, setTimeRange] = useState<TimeRange>('7d');
  const [category, setCategory] = useState<SloTemplateCategory | 'all'>('all');
  const [refreshKey, setRefreshKey] = useState(0);

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['slo-templates', category, refreshKey],
    queryFn: () => getSloTemplates(category !== 'all' ? { category } : {}),
    staleTime: 60_000,
    retry: false,
  });

  const templates = (data && data.length > 0) ? data : FALLBACK;
  const filtered = category === 'all' ? templates : templates.filter((t) => t.category === category);

  const handleRefresh = useCallback(() => {
    setRefreshKey((k) => k + 1);
    refetch();
  }, [refetch]);

  const compliancePresets = templates.filter((t) => t.compliancePreset).length;
  const categories = new Set(templates.map((t) => t.category)).size;

  return (
    <PageContainer>
      <div className="flex flex-col gap-1 mb-4 sm:flex-row sm:items-center sm:justify-between">
        <PageHeader
          title={t('sloMarketplace.title')}
          subtitle={t('sloMarketplace.subtitle')}
          icon={<Store className="w-5 h-5" />}
        />
        <div className="flex items-center gap-2 flex-wrap">
          <div className="flex rounded-md border border-border overflow-hidden text-xs">
            {TIME_RANGE_OPTIONS.map((opt) => (
              <button
                key={opt.value}
                type="button"
                onClick={() => setTimeRange(opt.value)}
                className={`px-3 py-1.5 transition-colors ${timeRange === opt.value ? 'bg-primary text-primary-foreground font-semibold' : 'hover:bg-muted text-muted-foreground'}`}
              >
                {t(opt.labelKey)}
              </button>
            ))}
          </div>
          <Button variant="outline" size="sm" onClick={handleRefresh}>
            <RefreshCw className="w-3.5 h-3.5 mr-1.5" />
            {t('common.refresh')}
          </Button>
        </div>
      </div>

      {isError && <PageErrorState message={t('sloMarketplace.loadError')} onRetry={handleRefresh} />}
      {isLoading && <PageLoadingState message={t('sloMarketplace.loading')} />}

      {!isLoading && (
        <>
          <PageSection>
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 mb-4">
              {[
                { label: t('sloMarketplace.stats.totalTemplates'), value: String(templates.length) },
                { label: t('sloMarketplace.stats.categories'), value: String(categories) },
                { label: t('sloMarketplace.stats.compliancePresets'), value: String(compliancePresets) },
              ].map((stat) => (
                <Card key={stat.label}>
                  <CardBody className="p-3">
                    <div className="text-xs text-muted-foreground mb-1">{stat.label}</div>
                    <div className="text-2xl font-bold tabular-nums">{stat.value}</div>
                  </CardBody>
                </Card>
              ))}
            </div>
          </PageSection>

          <PageSection>
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  {CATEGORY_OPTIONS.map((cat) => (
                    <button
                      key={cat.value}
                      type="button"
                      onClick={() => setCategory(cat.value)}
                      className={`px-3 py-1 rounded text-xs font-medium transition-colors ${category === cat.value ? 'bg-primary text-primary-foreground' : 'text-muted-foreground hover:bg-muted'}`}
                    >
                      {t(cat.labelKey)}
                    </button>
                  ))}
                </div>
              </CardHeader>
              <CardBody className="p-0">
                {filtered.length === 0 ? (
                  <div className="p-8 text-center text-muted-foreground text-sm">{t('sloMarketplace.noRecords')}</div>
                ) : (
                  <div className="overflow-x-auto">
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b border-border bg-muted/40 text-xs text-muted-foreground">
                          <th className="px-4 py-2.5 text-left font-medium">{t('sloMarketplace.table.name')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('sloMarketplace.table.type')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('sloMarketplace.table.sli')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('sloMarketplace.table.target')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('sloMarketplace.table.window')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('sloMarketplace.table.preset')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('sloMarketplace.table.uses')}</th>
                          <th className="px-4 py-2.5 text-left font-medium">{t('sloMarketplace.table.author')}</th>
                          <th className="px-4 py-2.5 text-left font-medium"></th>
                        </tr>
                      </thead>
                      <tbody>
                        {filtered.map((tmpl) => (
                          <tr key={tmpl.id} className="border-b border-border/50 hover:bg-muted/30 transition-colors">
                            <td className="px-4 py-2.5 font-medium">{tmpl.name}</td>
                            <td className="px-4 py-2.5"><Badge variant="secondary">{t(`sloMarketplace.categories.${tmpl.category}`)}</Badge></td>
                            <td className="px-4 py-2.5 text-muted-foreground">{tmpl.sliType}</td>
                            <td className="px-4 py-2.5 tabular-nums font-semibold">{tmpl.target}</td>
                            <td className="px-4 py-2.5 text-muted-foreground">{tmpl.window}</td>
                            <td className="px-4 py-2.5">
                              {tmpl.compliancePreset ? (
                                <Badge variant="info">{tmpl.compliancePreset}</Badge>
                              ) : (
                                <span className="text-muted-foreground text-xs">—</span>
                              )}
                            </td>
                            <td className="px-4 py-2.5 tabular-nums text-muted-foreground">{tmpl.uses}</td>
                            <td className="px-4 py-2.5 text-muted-foreground text-xs">{tmpl.author}</td>
                            <td className="px-4 py-2.5">
                              <Button variant="ghost" size="sm">
                                <Download className="w-3.5 h-3.5 mr-1" />
                                {t('sloMarketplace.actions.import')}
                              </Button>
                            </td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </div>
                )}
              </CardBody>
            </Card>
          </PageSection>
        </>
      )}
    </PageContainer>
  );
}
