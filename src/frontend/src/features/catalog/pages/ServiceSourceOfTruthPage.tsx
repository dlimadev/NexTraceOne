import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  ArrowLeft,
  Server,
  Globe,
  Users,
  FileText,
  BookOpen,
  ExternalLink,
  CheckCircle,
  XCircle,
  Layers,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';
import { sourceOfTruthApi } from '../api/sourceOfTruth';
import type { CoverageIndicators } from '../../../types';

/** Variantes visuais para badges de criticidade. */
const criticalityColors: Record<string, string> = {
  Critical: 'bg-red-900/40 text-red-300 border border-red-700/50',
  High: 'bg-orange-900/40 text-orange-300 border border-orange-700/50',
  Medium: 'bg-yellow-900/40 text-yellow-300 border border-yellow-700/50',
  Low: 'bg-slate-800/40 text-slate-300 border border-slate-700/50',
};

/** Variantes visuais para badges de ciclo de vida. */
const lifecycleColors: Record<string, string> = {
  Active: 'bg-emerald-900/40 text-emerald-300 border border-emerald-700/50',
  Deprecated: 'bg-orange-900/40 text-orange-300 border border-orange-700/50',
  Draft: 'bg-slate-800/40 text-slate-300 border border-slate-700/50',
  Retired: 'bg-red-900/40 text-red-300 border border-red-700/50',
};

/** Variantes visuais para badges de protocolo. */
const protocolColors: Record<string, string> = {
  OpenApi: 'bg-emerald-900/40 text-emerald-300 border border-emerald-700/50',
  Swagger: 'bg-teal-900/40 text-teal-300 border border-teal-700/50',
  Wsdl: 'bg-violet-900/40 text-violet-300 border border-violet-700/50',
  AsyncApi: 'bg-blue-900/40 text-blue-300 border border-blue-700/50',
  Protobuf: 'bg-amber-900/40 text-amber-300 border border-amber-700/50',
  GraphQl: 'bg-pink-900/40 text-pink-300 border border-pink-700/50',
};

/** Chaves dos indicadores de cobertura para iteração. */
const COVERAGE_KEYS: (keyof CoverageIndicators)[] = [
  'hasOwner',
  'hasContracts',
  'hasDocumentation',
  'hasRunbook',
  'hasRecentChangeHistory',
  'hasDependenciesMapped',
  'hasEventTopics',
];

/** Raio e geometria do anel SVG de progresso de cobertura. */
const RING_RADIUS = 40;
const RING_CIRCUMFERENCE = 2 * Math.PI * RING_RADIUS;

/** Página consolidada de Source of Truth de um serviço. */
export function ServiceSourceOfTruthPage() {
  const { t } = useTranslation();
  const { serviceId } = useParams<{ serviceId: string }>();

  const { data: sot, isLoading, isError } = useQuery({
    queryKey: ['sot-service', serviceId],
    queryFn: () => sourceOfTruthApi.getServiceSot(serviceId!),
    enabled: !!serviceId,
    staleTime: 15_000,
  });

  const { data: coverage } = useQuery({
    queryKey: ['sot-service-coverage', serviceId],
    queryFn: () => sourceOfTruthApi.getServiceCoverage(serviceId!),
    enabled: !!serviceId,
    staleTime: 15_000,
  });

  if (isLoading) {
    return (
      <div className="p-6 lg:p-8 animate-fade-in">
        <div className="flex items-center justify-center py-24">
          <p className="text-sm text-muted">{t('common.loading')}</p>
        </div>
      </div>
    );
  }

  if (isError || !sot) {
    return (
      <div className="p-6 lg:p-8 animate-fade-in">
        <div className="flex items-center justify-center py-24">
          <p className="text-sm text-muted">{t('common.error')}</p>
        </div>
      </div>
    );
  }

  const coveragePercent = coverage?.coverageScore ?? 0;
  const dashOffset = RING_CIRCUMFERENCE - (coveragePercent / 100) * RING_CIRCUMFERENCE;

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* Navigation */}
      <div className="flex items-center gap-4 mb-6">
        <Link to="/source-of-truth" className="flex items-center gap-1.5 text-sm text-muted hover:text-accent transition-colors">
          <ArrowLeft size={16} />
          <span>{t('sourceOfTruth.service.backToExplorer')}</span>
        </Link>
        <Link to={`/services/${serviceId}`} className="flex items-center gap-1.5 text-sm text-accent hover:text-accent/80 transition-colors">
          <ExternalLink size={14} />
          <span>{t('sourceOfTruth.service.viewServiceDetail')}</span>
        </Link>
      </div>

      {/* Header */}
      <div className="flex items-start gap-4 mb-8">
        <div className="w-12 h-12 rounded-lg bg-accent/15 flex items-center justify-center shrink-0">
          <Server size={24} className="text-accent" />
        </div>
        <div className="flex-1 min-w-0">
          <h1 className="text-2xl font-bold text-heading truncate">{sot.displayName || sot.name}</h1>
          <div className="flex items-center gap-2 mt-1 flex-wrap">
            <span className="text-sm text-muted">{sot.domain}</span>
            {sot.criticality && (
              <span className={`text-[11px] px-2 py-0.5 rounded-full ${criticalityColors[sot.criticality] ?? 'bg-slate-800/40 text-slate-300 border border-slate-700/50'}`}>
                {sot.criticality}
              </span>
            )}
            <span className={`text-[11px] px-2 py-0.5 rounded-full ${lifecycleColors[sot.lifecycleStatus] ?? 'bg-slate-800/40 text-slate-300 border border-slate-700/50'}`}>
              {sot.lifecycleStatus}
            </span>
          </div>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Left column — 2/3 */}
        <div className="lg:col-span-2 space-y-6">
          {/* Overview */}
          <Card>
            <CardHeader>
              <h2 className="text-base font-semibold text-heading">{t('sourceOfTruth.service.overview')}</h2>
            </CardHeader>
            <CardBody>
              <dl className="grid sm:grid-cols-2 gap-4 text-sm">
                <div>
                  <dt className="text-muted text-xs mb-0.5">{t('sourceOfTruth.service.description')}</dt>
                  <dd className="text-body">{sot.description || '—'}</dd>
                </div>
                <div>
                  <dt className="text-muted text-xs mb-0.5">{t('sourceOfTruth.service.serviceType')}</dt>
                  <dd className="text-body">{sot.serviceType || '—'}</dd>
                </div>
                <div>
                  <dt className="text-muted text-xs mb-0.5">{t('sourceOfTruth.service.systemArea')}</dt>
                  <dd className="text-body">{sot.systemArea || '—'}</dd>
                </div>
                <div>
                  <dt className="text-muted text-xs mb-0.5">{t('sourceOfTruth.service.exposureType')}</dt>
                  <dd className="text-body">{sot.exposureType || '—'}</dd>
                </div>
              </dl>
            </CardBody>
          </Card>

          {/* Ownership */}
          <Card>
            <CardHeader>
              <h2 className="text-base font-semibold text-heading flex items-center gap-2">
                <Users size={16} className="text-muted" />
                {t('sourceOfTruth.service.ownership')}
              </h2>
            </CardHeader>
            <CardBody>
              <dl className="grid sm:grid-cols-3 gap-4 text-sm">
                <div>
                  <dt className="text-muted text-xs mb-0.5">{t('sourceOfTruth.service.team')}</dt>
                  <dd className="text-body font-medium">{sot.teamName || '—'}</dd>
                </div>
                <div>
                  <dt className="text-muted text-xs mb-0.5">{t('sourceOfTruth.service.technicalOwner')}</dt>
                  <dd className="text-body">{sot.technicalOwner || '—'}</dd>
                </div>
                <div>
                  <dt className="text-muted text-xs mb-0.5">{t('sourceOfTruth.service.businessOwner')}</dt>
                  <dd className="text-body">{sot.businessOwner || '—'}</dd>
                </div>
              </dl>
            </CardBody>
          </Card>

          {/* APIs */}
          <Card>
            <CardHeader>
              <h2 className="text-base font-semibold text-heading flex items-center gap-2">
                <Globe size={16} className="text-muted" />
                {t('sourceOfTruth.service.apis')}
                <span className="ml-auto text-xs text-muted">{sot.totalApis}</span>
              </h2>
            </CardHeader>
            <CardBody className="p-0">
              {sot.apis.length === 0 ? (
                <div className="px-6 py-8">
                  <EmptyState icon={<Globe size={20} />} title={t('sourceOfTruth.service.noApis')} />
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b border-edge text-xs text-muted">
                        <th className="px-6 py-3 text-left font-medium">API</th>
                        <th className="px-6 py-3 text-left font-medium">Route</th>
                        <th className="px-6 py-3 text-left font-medium">Version</th>
                        <th className="px-6 py-3 text-left font-medium">Visibility</th>
                        <th className="px-6 py-3 text-right font-medium">Consumers</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-edge">
                      {sot.apis.map((api) => (
                        <tr key={api.apiAssetId} className="hover:bg-hover transition-colors">
                          <td className="px-6 py-3 text-body font-medium">{api.name}</td>
                          <td className="px-6 py-3 text-muted font-mono text-xs">{api.routePattern}</td>
                          <td className="px-6 py-3 text-muted">{api.version}</td>
                          <td className="px-6 py-3">
                            <span className="text-[11px] px-2 py-0.5 rounded-full bg-slate-800/40 text-slate-300 border border-slate-700/50">
                              {api.visibility}
                            </span>
                          </td>
                          <td className="px-6 py-3 text-right text-muted">{api.consumerCount}</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </CardBody>
          </Card>

          {/* Contracts */}
          <Card>
            <CardHeader>
              <h2 className="text-base font-semibold text-heading flex items-center gap-2">
                <FileText size={16} className="text-muted" />
                {t('sourceOfTruth.service.contracts')}
                <span className="ml-auto text-xs text-muted">{sot.totalContracts}</span>
              </h2>
            </CardHeader>
            <CardBody className="p-0">
              {sot.contracts.length === 0 ? (
                <div className="px-6 py-8">
                  <EmptyState icon={<FileText size={20} />} title={t('sourceOfTruth.service.noContracts')} />
                </div>
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b border-edge text-xs text-muted">
                        <th className="px-6 py-3 text-left font-medium">API</th>
                        <th className="px-6 py-3 text-left font-medium">Version</th>
                        <th className="px-6 py-3 text-left font-medium">Protocol</th>
                        <th className="px-6 py-3 text-left font-medium">Lifecycle</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-edge">
                      {sot.contracts.map((c) => (
                        <tr key={c.versionId} className="hover:bg-hover transition-colors">
                          <td className="px-6 py-3">
                            <Link to={`/source-of-truth/contracts/${c.versionId}`} className="text-accent hover:text-accent/80 font-medium">
                              {c.apiAssetId}
                            </Link>
                          </td>
                          <td className="px-6 py-3 text-muted">v{c.semVer}</td>
                          <td className="px-6 py-3">
                            <span className={`text-[11px] px-2 py-0.5 rounded-full ${protocolColors[c.protocol] ?? 'bg-slate-800/40 text-slate-300 border border-slate-700/50'}`}>
                              {c.protocol}
                            </span>
                          </td>
                          <td className="px-6 py-3">
                            <span className="text-[11px] px-2 py-0.5 rounded-full bg-slate-800/40 text-slate-300 border border-slate-700/50">
                              {c.lifecycleState}
                            </span>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </CardBody>
          </Card>

          {/* References */}
          <Card>
            <CardHeader>
              <h2 className="text-base font-semibold text-heading flex items-center gap-2">
                <BookOpen size={16} className="text-muted" />
                {t('sourceOfTruth.service.references')}
                <span className="ml-auto text-xs text-muted">{sot.totalReferences}</span>
              </h2>
            </CardHeader>
            <CardBody>
              {sot.references.length === 0 ? (
                <EmptyState icon={<BookOpen size={20} />} title={t('sourceOfTruth.service.noReferences')} />
              ) : (
                <ul className="space-y-3">
                  {sot.references.map((ref) => (
                    <li key={ref.referenceId} className="flex items-start gap-3 p-3 rounded-lg bg-elevated border border-edge">
                      <FileText size={16} className="text-muted shrink-0 mt-0.5" />
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium text-heading truncate">{ref.title}</p>
                        <p className="text-xs text-muted line-clamp-2">{ref.description}</p>
                        <span className="inline-block mt-1 text-[11px] px-2 py-0.5 rounded-full bg-slate-800/40 text-slate-300 border border-slate-700/50">
                          {ref.referenceType}
                        </span>
                      </div>
                      {ref.url && (
                        <a href={ref.url} target="_blank" rel="noopener noreferrer" className="text-accent hover:text-accent/80 shrink-0">
                          <ExternalLink size={14} />
                        </a>
                      )}
                    </li>
                  ))}
                </ul>
              )}
            </CardBody>
          </Card>
        </div>

        {/* Right column — 1/3 */}
        <div className="space-y-6">
          {/* Coverage Ring */}
          <Card>
            <CardHeader>
              <h2 className="text-base font-semibold text-heading flex items-center gap-2">
                <Layers size={16} className="text-muted" />
                {t('sourceOfTruth.service.coverage')}
              </h2>
            </CardHeader>
            <CardBody>
              <div className="flex flex-col items-center mb-4">
                <svg width="100" height="100" viewBox="0 0 100 100" className="mb-2">
                  <circle
                    cx="50" cy="50" r={RING_RADIUS}
                    fill="none" stroke="currentColor"
                    className="text-edge" strokeWidth="8"
                  />
                  <circle
                    cx="50" cy="50" r={RING_RADIUS}
                    fill="none" stroke="currentColor"
                    className="text-accent" strokeWidth="8"
                    strokeDasharray={RING_CIRCUMFERENCE}
                    strokeDashoffset={dashOffset}
                    strokeLinecap="round"
                    transform="rotate(-90 50 50)"
                  />
                  <text x="50" y="50" textAnchor="middle" dominantBaseline="central" className="fill-heading text-lg font-bold">
                    {Math.round(coveragePercent)}%
                  </text>
                </svg>
                <p className="text-xs text-muted">
                  {t('sourceOfTruth.service.coverageScore')}
                  {coverage ? ` — ${coverage.metIndicators}/${coverage.totalIndicators}` : ''}
                </p>
              </div>

              {/* Indicator checklist */}
              <ul className="space-y-2">
                {COVERAGE_KEYS.map((key) => {
                  const met = sot.coverage[key];
                  return (
                    <li key={key} className="flex items-center gap-2 text-sm">
                      {met ? (
                        <CheckCircle size={14} className="text-emerald-400 shrink-0" />
                      ) : (
                        <XCircle size={14} className="text-red-400 shrink-0" />
                      )}
                      <span className={met ? 'text-body' : 'text-muted'}>
                        {t(`sourceOfTruth.service.indicators.${key}`)}
                      </span>
                    </li>
                  );
                })}
              </ul>
            </CardBody>
          </Card>

          {/* Quick Links */}
          <Card>
            <CardHeader>
              <h2 className="text-base font-semibold text-heading">{t('sourceOfTruth.service.quickLinks')}</h2>
            </CardHeader>
            <CardBody>
              <ul className="space-y-2">
                {sot.documentationUrl && (
                  <li>
                    <a
                      href={sot.documentationUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="flex items-center gap-2 text-sm text-accent hover:text-accent/80 transition-colors"
                    >
                      <BookOpen size={14} />
                      {t('sourceOfTruth.service.documentationUrl')}
                    </a>
                  </li>
                )}
                {sot.repositoryUrl && (
                  <li>
                    <a
                      href={sot.repositoryUrl}
                      target="_blank"
                      rel="noopener noreferrer"
                      className="flex items-center gap-2 text-sm text-accent hover:text-accent/80 transition-colors"
                    >
                      <ExternalLink size={14} />
                      {t('sourceOfTruth.service.repositoryUrl')}
                    </a>
                  </li>
                )}
                {!sot.documentationUrl && !sot.repositoryUrl && (
                  <p className="text-xs text-muted">—</p>
                )}
              </ul>
            </CardBody>
          </Card>
        </div>
      </div>
    </div>
  );
}
