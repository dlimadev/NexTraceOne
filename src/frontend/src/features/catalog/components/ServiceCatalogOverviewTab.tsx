import { useTranslation } from 'react-i18next';
import {
  Activity,
  TrendingUp,
  Timer,
  AlertTriangle,
  Shield,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import type { AssetGraph, NodeHealthResult } from '../../../types';

interface ServiceCatalogOverviewTabProps {
  graph: AssetGraph | undefined;
  healthData: NodeHealthResult | undefined;
  onSelectNode: (nodeId: string) => void;
}

/**
 * Conteúdo da aba "Visão Operacional" do Service Catalog.
 * Exibe KPIs, saúde dos serviços, top consumers e anomalias.
 */
export function ServiceCatalogOverviewTab({ graph, healthData, onSelectNode }: ServiceCatalogOverviewTabProps) {
  const { t } = useTranslation();

  return (
    <div className="space-y-6">
      {/* KPI cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
        {[
          { label: t('serviceCatalog.overview.requestsPerMin'), value: '—', icon: <Activity size={18} />, color: 'text-blue-500' },
          { label: t('serviceCatalog.overview.throughput'), value: '—', icon: <TrendingUp size={18} />, color: 'text-emerald-500' },
          { label: t('serviceCatalog.overview.avgLatency'), value: '—', icon: <Timer size={18} />, color: 'text-amber-500' },
          { label: t('serviceCatalog.overview.errorRate'), value: '—', icon: <AlertTriangle size={18} />, color: 'text-red-500' },
        ].map((kpi) => (
          <Card key={kpi.label}>
            <CardBody className="flex items-center gap-3">
              <div className={kpi.color}>{kpi.icon}</div>
              <div>
                <p className="text-2xl font-bold text-heading">{kpi.value}</p>
                <p className="text-xs text-muted">{kpi.label}</p>
              </div>
            </CardBody>
          </Card>
        ))}
      </div>

      {/* Resumo de saúde dos nós */}
      <Card>
        <CardHeader>
          <h2 className="font-semibold text-heading">{t('serviceCatalog.overview.serviceHealth')}</h2>
        </CardHeader>
        <CardBody>
          {healthData?.items && healthData.items.length > 0 ? (
            <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
              {(['healthy', 'degraded', 'unhealthy', 'unknown'] as const).map((status) => {
                const count = healthData.items.filter((h) => h.status.toLowerCase() === status).length;
                const colors = {
                  healthy: 'bg-emerald-900/40 text-emerald-300 border-emerald-700/50',
                  degraded: 'bg-amber-900/40 text-amber-300 border-amber-700/50',
                  unhealthy: 'bg-red-900/40 text-red-300 border-red-700/50',
                  unknown: 'bg-slate-800/40 text-slate-300 border-slate-700/50',
                };
                return (
                  <div key={status} className={`rounded-lg border p-4 text-center ${colors[status]}`}>
                    <p className="text-3xl font-bold">{count}</p>
                    <p className="text-xs font-medium mt-1">{t(`serviceCatalog.overview.${status}`)}</p>
                  </div>
                );
              })}
            </div>
          ) : (
            <div className="py-8 text-center">
              <Activity size={32} className="mx-auto text-muted mb-3" />
              <p className="text-sm text-muted">{t('serviceCatalog.overview.noMetrics')}</p>
              <p className="text-xs text-muted mt-1">{t('serviceCatalog.overview.noMetricsHint')}</p>
            </div>
          )}
        </CardBody>
      </Card>

      {/* Principais consumidores e anomalias */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <Card>
          <CardHeader>
            <h3 className="font-semibold text-heading text-sm">{t('serviceCatalog.overview.topConsumers')}</h3>
          </CardHeader>
          <CardBody className="p-0">
            {graph?.apis && graph.apis.some(a => (a.consumers?.length ?? 0) > 0) ? (
              <ul className="divide-y divide-edge">
                {graph.apis
                  .filter(a => (a.consumers?.length ?? 0) > 0)
                  .sort((a, b) => (b.consumers?.length ?? 0) - (a.consumers?.length ?? 0))
                  .slice(0, 5)
                  .map(api => (
                    <li
                      key={api.apiAssetId}
                      role="button"
                      tabIndex={0}
                      className="px-4 py-3 flex items-center justify-between hover:bg-hover transition-colors cursor-pointer"
                      onClick={() => onSelectNode(api.apiAssetId)}
                      onKeyDown={(e) => { if (e.key === 'Enter' || e.key === ' ') { e.preventDefault(); onSelectNode(api.apiAssetId); } }}
                    >
                      <div>
                        <p className="text-sm font-medium text-heading">{api.name}</p>
                        <p className="text-xs text-muted font-mono">{api.routePattern}</p>
                      </div>
                      <Badge variant="info">{api.consumers?.length ?? 0} {t('serviceCatalog.consumers')}</Badge>
                    </li>
                  ))}
              </ul>
            ) : (
              <p className="px-4 py-6 text-sm text-muted text-center">{t('serviceCatalog.noDependencies')}</p>
            )}
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <h3 className="font-semibold text-heading text-sm">{t('serviceCatalog.overview.anomalies')}</h3>
          </CardHeader>
          <CardBody className="py-8 text-center">
            <Shield size={32} className="mx-auto text-muted mb-3" />
            <p className="text-sm text-muted">{t('serviceCatalog.overview.noAnomalies')}</p>
          </CardBody>
        </Card>
      </div>
    </div>
  );
}
