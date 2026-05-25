/**
 * ServiceObservabilityTab — Observabilidade contextualizada por serviço.
 *
 * Exibe métricas-chave de runtime do serviço (latência, taxa de erros, throughput)
 * e links diretos para os exploradores cross-service com o serviço pré-filtrado.
 *
 * @pillar Service 360° — APM contextualizado ao serviço, nunca isolado
 */
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Activity, Bug, GitFork, ExternalLink, AlertTriangle, Cpu, HardDrive, Gauge } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Link } from 'react-router-dom';
import client from '../../../api/client';

interface Props {
  serviceId: string;
  serviceName: string;
}

interface ServiceHealthSnapshot {
  requestsPerMinute: number;
  p95LatencyMs: number;
  errorRatePercent: number;
  throughputTrend: 'up' | 'down' | 'stable';
  availabilityPercent: number;
  lastUpdatedAt: string;
}

async function fetchServiceHealthSnapshot(serviceId: string): Promise<ServiceHealthSnapshot> {
  const r = await client.get<ServiceHealthSnapshot>(`/runtime/services/${serviceId}/health-snapshot`);
  return r.data;
}

function MetricCard({
  label,
  value,
  unit,
  status,
}: {
  label: string;
  value: string | number;
  unit?: string;
  status?: 'healthy' | 'warning' | 'critical';
}) {
  const statusColor =
    status === 'critical'
      ? 'text-critical'
      : status === 'warning'
        ? 'text-warning'
        : 'text-success';

  return (
    <div className="flex flex-col gap-1 p-4 bg-elevated rounded-lg border border-edge">
      <span className="text-xs text-muted uppercase tracking-wider">{label}</span>
      <span className={`text-2xl font-bold ${statusColor}`}>
        {value}
        {unit && <span className="text-sm font-normal text-muted ml-1">{unit}</span>}
      </span>
    </div>
  );
}

function ExplorerLink({
  icon,
  label,
  to,
}: {
  icon: React.ReactNode;
  label: string;
  to: string;
}) {
  return (
    <Link
      to={to}
      className="flex items-center justify-between p-3 rounded-lg border border-edge hover:bg-elevated transition-colors group"
    >
      <div className="flex items-center gap-2.5 text-sm text-body">
        <span className="text-accent">{icon}</span>
        {label}
      </div>
      <ExternalLink size={13} className="text-muted group-hover:text-accent transition-colors" />
    </Link>
  );
}

export function ServiceObservabilityTab({ serviceId, serviceName }: Props) {
  const { t } = useTranslation();

  const { data: snapshot, isLoading } = useQuery({
    queryKey: ['service-health-snapshot', serviceId],
    queryFn: () => fetchServiceHealthSnapshot(serviceId),
    staleTime: 30_000,
  });

  const errorStatus =
    !snapshot
      ? undefined
      : snapshot.errorRatePercent > 5
        ? 'critical'
        : snapshot.errorRatePercent > 1
          ? 'warning'
          : 'healthy';

  const latencyStatus =
    !snapshot
      ? undefined
      : snapshot.p95LatencyMs > 2000
        ? 'critical'
        : snapshot.p95LatencyMs > 500
          ? 'warning'
          : 'healthy';

  const encodedService = encodeURIComponent(serviceName);

  return (
    <div className="flex flex-col gap-6">
      {/* ── Métricas em tempo real ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <Activity size={16} className="text-accent" />
            <h3 className="text-sm font-semibold text-heading">
              {t('serviceDetail.observability.liveMetrics', 'Live Metrics')}
            </h3>
            {snapshot && (
              <Badge variant="success" size="sm">
                {t('serviceDetail.observability.live', 'Live')}
              </Badge>
            )}
          </div>
        </CardHeader>
        <CardBody>
          {isLoading ? (
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3 animate-pulse">
              {[...Array(4)].map((_, i) => (
                <div key={i} className="h-20 bg-elevated rounded-lg border border-edge" />
              ))}
            </div>
          ) : snapshot ? (
            <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
              <MetricCard
                label={t('serviceDetail.observability.rps', 'Requests/min')}
                value={snapshot.requestsPerMinute.toLocaleString()}
                status="healthy"
              />
              <MetricCard
                label={t('serviceDetail.observability.p95Latency', 'P95 Latency')}
                value={snapshot.p95LatencyMs}
                unit="ms"
                status={latencyStatus}
              />
              <MetricCard
                label={t('serviceDetail.observability.errorRate', 'Error Rate')}
                value={`${snapshot.errorRatePercent.toFixed(2)}%`}
                status={errorStatus}
              />
              <MetricCard
                label={t('serviceDetail.observability.availability', 'Availability')}
                value={`${snapshot.availabilityPercent.toFixed(2)}%`}
                status={snapshot.availabilityPercent >= 99.9 ? 'healthy' : snapshot.availabilityPercent >= 99 ? 'warning' : 'critical'}
              />
            </div>
          ) : (
            <div className="flex items-center gap-2 text-sm text-muted py-4">
              <AlertTriangle size={14} />
              {t('serviceDetail.observability.noData', 'No runtime data available. Connect an observability integration to see live metrics.')}
            </div>
          )}
        </CardBody>
      </Card>

      {/* ── Exploradores contextualizados ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <GitFork size={16} className="text-accent" />
            <h3 className="text-sm font-semibold text-heading">
              {t('serviceDetail.observability.explorers', 'Explore Runtime Data')}
            </h3>
          </div>
        </CardHeader>
        <CardBody>
          <p className="text-xs text-muted mb-4">
            {t('serviceDetail.observability.explorersDescription', 'All views below are pre-filtered for this service.')}
          </p>
          <div className="grid grid-cols-1 sm:grid-cols-2 gap-2">
            <ExplorerLink
              icon={<Activity size={15} />}
              label={t('sidebar.requestExplorer', 'Request Explorer')}
              to={`/operations/request-explorer?service=${encodedService}`}
            />
            <ExplorerLink
              icon={<GitFork size={15} />}
              label={t('sidebar.traceExplorer', 'Trace Explorer')}
              to={`/operations/telemetry/traces?service=${encodedService}`}
            />
            <ExplorerLink
              icon={<Bug size={15} />}
              label={t('sidebar.errorTracking', 'Error Tracking')}
              to={`/operations/error-tracking?service=${encodedService}`}
            />
            <ExplorerLink
              icon={<Cpu size={15} />}
              label={t('sidebar.profilingExplorer', 'Profiling')}
              to={`/operations/profiling-explorer?service=${encodedService}`}
            />
            <ExplorerLink
              icon={<HardDrive size={15} />}
              label={t('sidebar.dbExplorer', 'DB Impact')}
              to={`/operations/db-explorer?service=${encodedService}`}
            />
            <ExplorerLink
              icon={<Gauge size={15} />}
              label={t('sidebar.loadTesting', 'Load Testing')}
              to={`/operations/load-testing?service=${encodedService}`}
            />
          </div>
        </CardBody>
      </Card>
    </div>
  );
}
