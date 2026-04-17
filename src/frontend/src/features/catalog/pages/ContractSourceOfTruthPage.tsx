import { useParams, Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import {
  ArrowLeft,
  FileText,
  Shield,
  BarChart3,
  BookOpen,
  ExternalLink,
  Lock,
  Unlock,
  CheckCircle,
  XCircle,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { sourceOfTruthApi } from '../api/sourceOfTruth';
import { PageContainer } from '../../../components/shell';
import { isRouteAvailableInFinalProductionScope } from '../../../releaseScope';
import type { SourceOfTruthReferenceItem } from '../../../types';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

/** Variantes visuais para badges de protocolo. */
const protocolColors: Record<string, string> = {
  OpenApi: 'bg-success/15 text-success border border-success/25',
  Swagger: 'bg-teal-900/40 text-teal-300 border border-teal-700/50',
  Wsdl: 'bg-violet-900/40 text-violet-300 border border-violet-700/50',
  AsyncApi: 'bg-info/15 text-info border border-info/25',
  Protobuf: 'bg-warning/15 text-warning border border-warning/25',
  GraphQl: 'bg-pink-900/40 text-pink-300 border border-pink-700/50',
};

/** Variantes visuais para badges de ciclo de vida. */
const lifecycleColors: Record<string, string> = {
  Draft: 'bg-elevated text-muted border border-edge',
  InReview: 'bg-info/15 text-info border border-info/25',
  Approved: 'bg-success/15 text-success border border-success/25',
  Locked: 'bg-info/15 text-info border border-info/25',
  Deprecated: 'bg-warning/15 text-warning border border-warning/25',
  Sunset: 'bg-critical/15 text-critical border border-critical/25',
  Retired: 'bg-elevated text-muted border border-edge',
};

/** Página consolidada de Source of Truth de um contrato. */
export function ContractSourceOfTruthPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const { contractVersionId } = useParams<{ contractVersionId: string }>();

  const { data: sot, isLoading, isError } = useQuery({
    queryKey: ['sot-contract', contractVersionId, activeEnvironmentId],
    queryFn: () => sourceOfTruthApi.getContractSot(contractVersionId!),
    enabled: !!contractVersionId,
    staleTime: 15_000,
  });

  if (isLoading) {
    return (
      <PageContainer>
        <PageLoadingState size="lg" />
      </PageContainer>
    );
  }

  if (isError || !sot) {
    return (
      <PageContainer>
        <PageErrorState
          action={
            <Link to="/source-of-truth" className="text-sm text-accent hover:underline">
              {t('common.back')}
            </Link>
          }
        />
      </PageContainer>
    );
  }

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* Navigation */}
      <div className="flex items-center gap-4 mb-6">
        <Link to="/source-of-truth" className="flex items-center gap-1.5 text-sm text-muted hover:text-accent transition-colors">
          <ArrowLeft size={16} />
          <span>{t('sourceOfTruth.contract.backToExplorer')}</span>
        </Link>
        {isRouteAvailableInFinalProductionScope(`/contracts/${contractVersionId}`) && (
        <Link to={`/contracts/${contractVersionId}`} className="flex items-center gap-1.5 text-sm text-accent hover:text-accent/80 transition-colors">
          <ExternalLink size={14} />
          <span>{t('sourceOfTruth.contract.viewContractDetail')}</span>
        </Link>
        )}
      </div>

      {/* Header */}
      <div className="flex items-start gap-4 mb-8">
        <div className="w-12 h-12 rounded-lg bg-accent/15 flex items-center justify-center shrink-0">
          <FileText size={24} className="text-accent" />
        </div>
        <div className="flex-1 min-w-0">
          <h1 className="text-2xl font-bold text-heading">{t('sourceOfTruth.contract.title')}</h1>
          <div className="flex items-center gap-2 mt-1 flex-wrap">
            <span className="text-sm text-muted">{sot.apiAssetId}</span>
            <span className="text-sm text-muted">v{sot.semVer}</span>
            <span className={`text-[11px] px-2 py-0.5 rounded-full ${protocolColors[sot.protocol] ?? 'bg-elevated text-muted border border-edge'}`}>
              {t(`contractGov.badges.protocols.${sot.protocol}`, sot.protocol)}
            </span>
            <span className={`text-[11px] px-2 py-0.5 rounded-full ${lifecycleColors[sot.governance.lifecycleState] ?? 'bg-elevated text-muted border border-edge'}`}>
              {t(`contractGov.badges.lifecycle.${sot.governance.lifecycleState}`, sot.governance.lifecycleState)}
            </span>
          </div>
        </div>
      </div>

      <div className="grid gap-6 lg:grid-cols-3">
        {/* Left column */}
        <div className="lg:col-span-2 space-y-6">
          {/* Overview */}
          <Card>
            <CardHeader>
              <h2 className="text-base font-semibold text-heading">{t('sourceOfTruth.contract.overview')}</h2>
            </CardHeader>
            <CardBody>
              <dl className="grid sm:grid-cols-2 gap-4 text-sm">
                <div>
                  <dt className="text-muted text-xs mb-0.5">{t('sourceOfTruth.contract.apiAssetId')}</dt>
                  <dd className="text-body font-medium">{sot.apiAssetId}</dd>
                </div>
                <div>
                  <dt className="text-muted text-xs mb-0.5">{t('sourceOfTruth.contract.format')}</dt>
                  <dd className="text-body">{sot.format || t('common.noData')}</dd>
                </div>
                <div>
                  <dt className="text-muted text-xs mb-0.5">{t('sourceOfTruth.contract.importedFrom')}</dt>
                  <dd className="text-body">{sot.importedFrom || t('common.noData')}</dd>
                </div>
              </dl>
            </CardBody>
          </Card>

          {/* Governance */}
          <Card>
            <CardHeader>
              <h2 className="text-base font-semibold text-heading flex items-center gap-2">
                <Shield size={16} className="text-muted" />
                {t('sourceOfTruth.contract.governance')}
              </h2>
            </CardHeader>
            <CardBody>
              <dl className="grid sm:grid-cols-2 gap-4 text-sm">
                <div>
                  <dt className="text-muted text-xs mb-0.5">{t('sourceOfTruth.contract.lifecycleState')}</dt>
                  <dd>
                    <span className={`text-[11px] px-2 py-0.5 rounded-full ${lifecycleColors[sot.governance.lifecycleState] ?? 'bg-elevated text-muted border border-edge'}`}>
                      {t(`contractGov.badges.lifecycle.${sot.governance.lifecycleState}`, sot.governance.lifecycleState)}
                    </span>
                  </dd>
                </div>
                <div>
                  <dt className="text-muted text-xs mb-0.5">{t('sourceOfTruth.contract.lockStatus')}</dt>
                  <dd className="flex items-center gap-1.5 text-body">
                    {sot.governance.isLocked ? (
                      <><Lock size={14} className="text-info" /><span>{t('common.yes')}</span></>
                    ) : (
                      <><Unlock size={14} className="text-muted" /><span>{t('common.no')}</span></>
                    )}
                  </dd>
                </div>
                <div>
                  <dt className="text-muted text-xs mb-0.5">{t('sourceOfTruth.contract.signedStatus')}</dt>
                  <dd className="flex items-center gap-1.5 text-body">
                    {sot.governance.isSigned ? (
                      <><CheckCircle size={14} className="text-success" /><span>{t('common.yes')}</span></>
                    ) : (
                      <><XCircle size={14} className="text-muted" /><span>{t('common.no')}</span></>
                    )}
                  </dd>
                </div>
                {sot.governance.deprecationNotice && (
                  <div className="sm:col-span-2">
                    <dt className="text-muted text-xs mb-0.5">{t('sourceOfTruth.contract.deprecationNotice')}</dt>
                    <dd className="text-warning text-sm">{sot.governance.deprecationNotice}</dd>
                  </div>
                )}
                {sot.governance.deprecationDate && (
                  <div>
                    <dt className="text-muted text-xs mb-0.5">{t('sourceOfTruth.contract.deprecationDate')}</dt>
                    <dd className="text-body">{sot.governance.deprecationDate}</dd>
                  </div>
                )}
                {sot.governance.sunsetDate && (
                  <div>
                    <dt className="text-muted text-xs mb-0.5">{t('sourceOfTruth.contract.sunsetDate')}</dt>
                    <dd className="text-body">{sot.governance.sunsetDate}</dd>
                  </div>
                )}
              </dl>
            </CardBody>
          </Card>

          {/* References */}
          <Card>
            <CardHeader>
              <h2 className="text-base font-semibold text-heading flex items-center gap-2">
                <BookOpen size={16} className="text-muted" />
                {t('sourceOfTruth.contract.references')}
              </h2>
            </CardHeader>
            <CardBody>
              {sot.references.length === 0 ? (
                <EmptyState icon={<BookOpen size={20} />} title={t('sourceOfTruth.contract.noReferences')} />
              ) : (
                <ul className="space-y-3">
                  {sot.references.map((ref: SourceOfTruthReferenceItem) => (
                    <li key={ref.referenceId} className="flex items-start gap-3 p-3 rounded-lg bg-elevated border border-edge">
                      <FileText size={16} className="text-muted shrink-0 mt-0.5" />
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium text-heading truncate">{ref.title}</p>
                        <p className="text-xs text-muted line-clamp-2">{ref.description}</p>
                        <span className="inline-block mt-1 text-[11px] px-2 py-0.5 rounded-full bg-elevated text-muted border border-edge">
                          {String(t(`sourceOfTruth.referenceTypes.${ref.referenceType}`, ref.referenceType))}
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

        {/* Right column — Metrics */}
        <div className="space-y-6">
          <Card>
            <CardHeader>
              <h2 className="text-base font-semibold text-heading flex items-center gap-2">
                <BarChart3 size={16} className="text-muted" />
                {t('sourceOfTruth.contract.metrics')}
              </h2>
            </CardHeader>
            <CardBody>
              <div className="space-y-4">
                <div className="flex items-center justify-between p-3 rounded-lg bg-elevated border border-edge">
                  <span className="text-sm text-muted">{t('sourceOfTruth.contract.artifacts')}</span>
                  <span className="text-lg font-bold text-heading">{sot.artifactCount}</span>
                </div>
                <div className="flex items-center justify-between p-3 rounded-lg bg-elevated border border-edge">
                  <span className="text-sm text-muted">{t('sourceOfTruth.contract.diffs')}</span>
                  <span className="text-lg font-bold text-heading">{sot.diffCount}</span>
                </div>
                <div className="flex items-center justify-between p-3 rounded-lg bg-elevated border border-edge">
                  <span className="text-sm text-muted">{t('sourceOfTruth.contract.violations')}</span>
                  <span className={`text-lg font-bold ${sot.violationCount > 0 ? 'text-critical' : 'text-heading'}`}>
                    {sot.violationCount}
                  </span>
                </div>
              </div>
            </CardBody>
          </Card>
        </div>
      </div>
    </div>
  );
}
