import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  Activity, CheckCircle, AlertTriangle, XCircle,
  TrendingUp, TrendingDown, Minus, Shield, ArrowLeft,
  FileText, Link2, BookOpen, GitBranch,
} from 'lucide-react';
import { NavLink } from 'react-router-dom';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';

const mockDetails: Record<string, {
  identity: { serviceId: string; displayName: string; serviceType: string; domain: string; teamName: string; criticality: string };
  status: string;
  summary: string;
  trend: { direction: string; timeframe: string; summary: string };
  metrics: { availability: number; latency: number; errorRate: number; rps: number };
  flags: number;
  changes: { id: string; description: string; type: string; confidence: string; date: string }[];
  incidents: { id: string; ref: string; title: string; status: string; date: string }[];
  dependencies: { serviceId: string; displayName: string; status: string }[];
  contracts: { id: string; name: string; version: string; protocol: string; state: string }[];
  runbooks: { title: string; url: string }[];
  anomaly: string;
  coverage: { signals: boolean; runbook: boolean; owner: boolean; deps: boolean; changes: boolean; incidents: boolean };
}> = {
  'svc-order-api': {
    identity: { serviceId: 'svc-order-api', displayName: 'Order API', serviceType: 'RestApi', domain: 'Orders', teamName: 'order-squad', criticality: 'Critical' },
    status: 'Healthy',
    summary: 'reliability.mock.orderApiSummary',
    trend: { direction: 'Stable', timeframe: '7d', summary: 'reliability.mock.orderApiTrend' },
    metrics: { availability: 99.95, latency: 45.2, errorRate: 0.3, rps: 1250 },
    flags: 0,
    changes: [{ id: '1', description: 'v2.4.1 — Performance tuning', type: 'Deployment', confidence: 'Validated', date: '3 days ago' }],
    incidents: [],
    dependencies: [
      { serviceId: 'svc-payment-gateway', displayName: 'Payment Gateway', status: 'Degraded' },
      { serviceId: 'svc-inventory-consumer', displayName: 'Inventory Consumer', status: 'NeedsAttention' },
    ],
    contracts: [{ id: '1', name: 'Order API v2', version: '2.4.0', protocol: 'REST', state: 'Published' }],
    runbooks: [{ title: 'Order API — Incident Response', url: '#' }],
    anomaly: 'reliability.mock.noAnomalies',
    coverage: { signals: true, runbook: true, owner: true, deps: true, changes: true, incidents: true },
  },
  'svc-payment-gateway': {
    identity: { serviceId: 'svc-payment-gateway', displayName: 'Payment Gateway', serviceType: 'RestApi', domain: 'Payments', teamName: 'payment-squad', criticality: 'Critical' },
    status: 'Degraded',
    summary: 'reliability.mock.paymentGwSummary',
    trend: { direction: 'Declining', timeframe: '24h', summary: 'reliability.mock.paymentGwTrend' },
    metrics: { availability: 94.8, latency: 320.5, errorRate: 5.2, rps: 890 },
    flags: 5,
    changes: [{ id: '2', description: 'v3.1.0 — New retry logic', type: 'Deployment', confidence: 'NeedsAttention', date: '6 hours ago' }],
    incidents: [],
    dependencies: [{ serviceId: 'svc-auth-gateway', displayName: 'Auth Gateway', status: 'Healthy' }],
    contracts: [{ id: '2', name: 'Payment API v3', version: '3.1.0', protocol: 'REST', state: 'Published' }],
    runbooks: [{ title: 'Payment Gateway — Degradation Playbook', url: '#' }],
    anomaly: 'reliability.mock.paymentGwAnomaly',
    coverage: { signals: true, runbook: true, owner: true, deps: true, changes: true, incidents: true },
  },
  'svc-catalog-sync': {
    identity: { serviceId: 'svc-catalog-sync', displayName: 'Catalog Sync', serviceType: 'IntegrationComponent', domain: 'Catalog', teamName: 'platform-squad', criticality: 'Medium' },
    status: 'Unavailable',
    summary: 'reliability.mock.catalogSyncSummary',
    trend: { direction: 'Declining', timeframe: '2h', summary: 'reliability.mock.catalogSyncTrend' },
    metrics: { availability: 0, latency: 0, errorRate: 100, rps: 0 },
    flags: 10,
    changes: [],
    incidents: [{ id: '1', ref: 'INC-2024-0042', title: 'Integration partner outage', status: 'Open', date: '2 hours ago' }],
    dependencies: [],
    contracts: [],
    runbooks: [],
    anomaly: 'reliability.mock.catalogSyncAnomaly',
    coverage: { signals: true, runbook: false, owner: true, deps: false, changes: false, incidents: true },
  },
};

const statusBadge = (status: string): { variant: 'success' | 'warning' | 'danger' | 'default'; icon: React.ReactNode } => {
  switch (status) {
    case 'Healthy': return { variant: 'success', icon: <CheckCircle size={14} /> };
    case 'Degraded': return { variant: 'warning', icon: <AlertTriangle size={14} /> };
    case 'Unavailable': return { variant: 'danger', icon: <XCircle size={14} /> };
    case 'NeedsAttention': return { variant: 'default', icon: <Shield size={14} /> };
    default: return { variant: 'default', icon: <Minus size={14} /> };
  }
};

const trendIcon = (dir: string) => {
  switch (dir) {
    case 'Improving': return <TrendingUp size={14} className="text-emerald-500" />;
    case 'Declining': return <TrendingDown size={14} className="text-red-500" />;
    default: return <Minus size={14} className="text-muted" />;
  }
};

export function ServiceReliabilityDetailPage() {
  const { serviceId } = useParams<{ serviceId: string }>();
  const { t } = useTranslation();

  const data = serviceId ? mockDetails[serviceId] : undefined;

  if (!data) {
    return (
      <div className="p-6 lg:p-8 animate-fade-in">
        <NavLink to="/operations/reliability" className="flex items-center gap-1 text-sm text-accent hover:underline mb-4">
          <ArrowLeft size={14} /> {t('common.back')}
        </NavLink>
        <div className="text-center text-muted py-12">{t('reliability.detail.notFound')}</div>
      </div>
    );
  }

  const badge = statusBadge(data.status);

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      <NavLink to="/operations/reliability" className="flex items-center gap-1 text-sm text-accent hover:underline mb-4">
        <ArrowLeft size={14} /> {t('reliability.detail.backToOverview')}
      </NavLink>

      {/* Header */}
      <div className="flex items-start justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-heading">{data.identity.displayName}</h1>
          <p className="text-sm text-muted mt-0.5">{data.identity.serviceId} · {data.identity.serviceType} · {data.identity.domain}</p>
        </div>
        <Badge variant={badge.variant} className="flex items-center gap-1 text-sm px-3 py-1">
          {badge.icon}
          {t(`reliability.status.${data.status}`)}
        </Badge>
      </div>

      {/* Overview Cards */}
      <div className="grid md:grid-cols-4 gap-4 mb-6">
        <Card><CardBody><p className="text-xs text-muted">{t('reliability.detail.availability')}</p><p className="text-lg font-semibold text-heading">{data.metrics.availability}%</p></CardBody></Card>
        <Card><CardBody><p className="text-xs text-muted">{t('reliability.detail.latencyP99')}</p><p className="text-lg font-semibold text-heading">{data.metrics.latency} ms</p></CardBody></Card>
        <Card><CardBody><p className="text-xs text-muted">{t('reliability.detail.errorRate')}</p><p className="text-lg font-semibold text-heading">{data.metrics.errorRate}%</p></CardBody></Card>
        <Card><CardBody><p className="text-xs text-muted">{t('reliability.detail.throughput')}</p><p className="text-lg font-semibold text-heading">{data.metrics.rps} rps</p></CardBody></Card>
      </div>

      <div className="grid md:grid-cols-2 gap-6">
        {/* Operational Summary */}
        <Card>
          <CardHeader><h2 className="text-sm font-semibold text-heading flex items-center gap-2"><Activity size={16} className="text-accent" />{t('reliability.detail.operationalSummary')}</h2></CardHeader>
          <CardBody>
            <p className="text-sm text-body">{t(data.summary)}</p>
            <div className="mt-3 flex items-center gap-2 text-xs text-muted">
              {trendIcon(data.trend.direction)}
              <span>{t(`reliability.trend.${data.trend.direction}`)} · {data.trend.timeframe}</span>
            </div>
            <p className="text-xs text-muted mt-1">{t(data.trend.summary)}</p>
          </CardBody>
        </Card>

        {/* Anomaly Summary */}
        <Card>
          <CardHeader><h2 className="text-sm font-semibold text-heading flex items-center gap-2"><AlertTriangle size={16} className="text-amber-500" />{t('reliability.detail.anomalySummary')}</h2></CardHeader>
          <CardBody>
            <p className="text-sm text-body">{t(data.anomaly)}</p>
          </CardBody>
        </Card>

        {/* Ownership */}
        <Card>
          <CardHeader><h2 className="text-sm font-semibold text-heading">{t('reliability.detail.ownership')}</h2></CardHeader>
          <CardBody>
            <dl className="grid grid-cols-2 gap-2 text-sm">
              <div><dt className="text-xs text-muted">{t('reliability.detail.team')}</dt><dd className="text-body">{data.identity.teamName}</dd></div>
              <div><dt className="text-xs text-muted">{t('reliability.detail.criticality')}</dt><dd className="text-body">{data.identity.criticality}</dd></div>
            </dl>
          </CardBody>
        </Card>

        {/* Recent Changes */}
        <Card>
          <CardHeader><h2 className="text-sm font-semibold text-heading flex items-center gap-2"><GitBranch size={16} className="text-accent" />{t('reliability.detail.recentChanges')}</h2></CardHeader>
          <CardBody>
            {data.changes.length === 0 ? (
              <p className="text-sm text-muted">{t('reliability.detail.noRecentChanges')}</p>
            ) : (
              <ul className="space-y-2">
                {data.changes.map(c => (
                  <li key={c.id} className="text-sm">
                    <span className="text-body">{c.description}</span>
                    <span className="text-xs text-muted ml-2">{c.date}</span>
                    <Badge variant={c.confidence === 'Validated' ? 'success' : 'warning'} className="ml-2 text-[10px]">{c.confidence}</Badge>
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>

        {/* Linked Incidents */}
        <Card>
          <CardHeader><h2 className="text-sm font-semibold text-heading flex items-center gap-2"><AlertTriangle size={16} className="text-red-500" />{t('reliability.detail.linkedIncidents')}</h2></CardHeader>
          <CardBody>
            {data.incidents.length === 0 ? (
              <p className="text-sm text-muted">{t('reliability.detail.noIncidents')}</p>
            ) : (
              <ul className="space-y-2">
                {data.incidents.map(inc => (
                  <li key={inc.id} className="text-sm">
                    <span className="font-medium text-heading">{inc.ref}</span>
                    <span className="text-body ml-2">{inc.title}</span>
                    <Badge variant="danger" className="ml-2 text-[10px]">{inc.status}</Badge>
                    <span className="text-xs text-muted ml-2">{inc.date}</span>
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>

        {/* Dependencies */}
        <Card>
          <CardHeader><h2 className="text-sm font-semibold text-heading flex items-center gap-2"><Link2 size={16} className="text-accent" />{t('reliability.detail.dependencies')}</h2></CardHeader>
          <CardBody>
            {data.dependencies.length === 0 ? (
              <p className="text-sm text-muted">{t('reliability.detail.noDependencies')}</p>
            ) : (
              <ul className="space-y-2">
                {data.dependencies.map(dep => {
                  const depBadge = statusBadge(dep.status);
                  return (
                    <li key={dep.serviceId} className="flex items-center gap-2 text-sm">
                      <Badge variant={depBadge.variant} className="flex items-center gap-1 text-[10px]">
                        {depBadge.icon} {t(`reliability.status.${dep.status}`)}
                      </Badge>
                      <NavLink to={`/operations/reliability/${dep.serviceId}`} className="text-accent hover:underline">{dep.displayName}</NavLink>
                    </li>
                  );
                })}
              </ul>
            )}
          </CardBody>
        </Card>

        {/* Contracts */}
        <Card>
          <CardHeader><h2 className="text-sm font-semibold text-heading flex items-center gap-2"><FileText size={16} className="text-accent" />{t('reliability.detail.linkedContracts')}</h2></CardHeader>
          <CardBody>
            {data.contracts.length === 0 ? (
              <p className="text-sm text-muted">{t('reliability.detail.noContracts')}</p>
            ) : (
              <ul className="space-y-2">
                {data.contracts.map(c => (
                  <li key={c.id} className="text-sm">
                    <span className="text-body">{c.name}</span>
                    <span className="text-xs text-muted ml-2">v{c.version} · {c.protocol} · {c.state}</span>
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>

        {/* Runbooks */}
        <Card>
          <CardHeader><h2 className="text-sm font-semibold text-heading flex items-center gap-2"><BookOpen size={16} className="text-accent" />{t('reliability.detail.runbooks')}</h2></CardHeader>
          <CardBody>
            {data.runbooks.length === 0 ? (
              <p className="text-sm text-muted">{t('reliability.detail.noRunbooks')}</p>
            ) : (
              <ul className="space-y-2">
                {data.runbooks.map((r, i) => (
                  <li key={i} className="text-sm">
                    <a href={r.url} className="text-accent hover:underline">{r.title}</a>
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>
      </div>

      {/* Coverage */}
      <Card className="mt-6">
        <CardHeader><h2 className="text-sm font-semibold text-heading">{t('reliability.detail.coverage')}</h2></CardHeader>
        <CardBody>
          <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3">
            {Object.entries(data.coverage).map(([key, value]) => (
              <div key={key} className={`flex items-center gap-2 text-sm px-3 py-2 rounded-md border ${value ? 'border-emerald-500/20 bg-emerald-500/5 text-emerald-600' : 'border-red-500/20 bg-red-500/5 text-red-500'}`}>
                {value ? <CheckCircle size={14} /> : <XCircle size={14} />}
                {t(`reliability.coverage.${key}`)}
              </div>
            ))}
          </div>
        </CardBody>
      </Card>
    </div>
  );
}
