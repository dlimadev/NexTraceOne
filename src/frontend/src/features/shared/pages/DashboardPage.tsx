import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  Zap, FileText, ShieldCheck, Activity, AlertTriangle,
  ArrowRight, AlertCircle, Clock, ChevronRight, Download,
} from 'lucide-react';
import { StatCard } from '../../../components/StatCard';
import { Card, CardHeader, CardBody, CardFooter } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { EmptyState } from '../../../components/EmptyState';
import { StackedProgressBar } from '../../../components/StackedProgressBar';
import { PageContainer, PageSection, ContentGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { usePersona } from '../../../contexts/PersonaContext';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import { serviceCatalogApi } from '../../catalog/api/serviceCatalog';
import { contractsApi } from '../../catalog/api/contracts';
import { changeConfidenceApi } from '../../change-governance/api/changeConfidence';
import { incidentsApi } from '../../operations/api/incidents';
import { isRouteAvailableInFinalProductionScope } from '../../../releaseScope';
import { queryKeys } from '../../../shared/api/queryKeys';
import type { Persona } from '../../../auth/persona';

/** Chips decorativos de persona — apresentam o perfil activo e os disponíveis. */
const PERSONA_CHIPS: { id: Persona; labelKey: string }[] = [
  { id: 'Engineer',     labelKey: 'persona.Engineer.label' },
  { id: 'TechLead',     labelKey: 'persona.TechLead.label' },
  { id: 'Architect',    labelKey: 'persona.Architect.label' },
  { id: 'Executive',    labelKey: 'persona.Executive.label' },
  { id: 'PlatformAdmin',labelKey: 'persona.PlatformAdmin.label' },
];

/**
 * Página principal do dashboard — experiência persona-aware com narrativa operacional.
 *
 * Layout:
 * 1. Context bar (title + persona badge + subtítulo)
 * 2. Persona switcher decorativo (chips horizontais)
 * 3. KPI storytelling row — 4 colunas
 * 4. Attention alerts estruturados
 * 5. Operational grid 2×2
 *
 * @see docs/PERSONA-UX-MAPPING.md
 */
export function DashboardPage() {
  const { t } = useTranslation();
  const { persona, config } = usePersona();
  const { activeEnvironmentId } = useEnvironment();
  const showContractsSurface = isRouteAvailableInFinalProductionScope('/contracts');

  const { data: graph, isLoading: graphLoading, isError: graphError } = useQuery({
    queryKey: queryKeys.catalog.graph(activeEnvironmentId),
    queryFn: () => serviceCatalogApi.getGraph(),
    staleTime: 30_000,
  });

  const { data: contractsSummary, isLoading: contractsLoading } = useQuery({
    queryKey: queryKeys.contracts.summary(activeEnvironmentId),
    queryFn: () => contractsApi.getContractsSummary(),
    staleTime: 30_000,
  });

  const { data: changesSummary, isLoading: changesLoading } = useQuery({
    queryKey: queryKeys.changes.summary(activeEnvironmentId),
    queryFn: () => changeConfidenceApi.getSummary(),
    staleTime: 30_000,
  });

  const { data: incidentsSummary, isLoading: incidentsLoading } = useQuery({
    queryKey: queryKeys.incidents.summary(activeEnvironmentId),
    queryFn: () => incidentsApi.getIncidentSummary(),
    staleTime: 30_000,
  });

  const totalServices = graph?.services?.length ?? 0;
  const totalContracts = contractsSummary?.totalVersions ?? 0;
  const totalChanges = changesSummary?.totalChanges ?? 0;
  const openIncidents = incidentsSummary?.totalOpen ?? 0;

  /** 4 KPIs — "APIs registadas" movida para /services como stat local. */
  const stats = [
    {
      title: t('dashboard.activeServices'),
      value: graphLoading ? '…' : totalServices,
      icon: <Activity size={20} />,
      color: 'text-cyan',
      trend: totalServices > 0 ? { direction: 'up' as const, label: t('dashboard.trendHealthy') } : undefined,
      sparkline: totalServices > 0 ? { data: [3, 5, 8, 12, 15, totalServices, totalServices], color: 'var(--t-cyan)' } : undefined,
      footer: totalServices > 0 ? t('dashboard.vsLastPeriod', { value: Math.max(0, totalServices - 2) }) : undefined,
      footerHref: '/services',
    },
    {
      title: t('dashboard.totalContracts'),
      value: contractsLoading ? '…' : totalContracts,
      icon: <FileText size={20} />,
      color: 'text-success',
      trend: (contractsSummary?.inReviewCount ?? 0) > 0
        ? { direction: 'up' as const, label: `${contractsSummary?.inReviewCount} ${t('dashboard.inReviewShort')}` }
        : undefined,
      sparkline: totalContracts > 0 ? { data: [2, 4, 6, 8, 10, totalContracts, totalContracts], color: 'var(--t-success)' } : undefined,
    },
    {
      title: t('dashboard.recentChanges'),
      value: changesLoading ? '…' : totalChanges,
      icon: <ShieldCheck size={20} />,
      color: 'text-warning',
      trend: (changesSummary?.changesNeedingAttention ?? 0) > 0
        ? { direction: 'down' as const, label: `${changesSummary?.changesNeedingAttention} ${t('dashboard.needAttention')}` }
        : undefined,
      sparkline: totalChanges > 0 ? { data: [1, 3, 2, 5, 4, totalChanges, totalChanges], color: 'var(--t-warning)' } : undefined,
    },
    {
      title: t('dashboard.openIncidents'),
      value: incidentsLoading ? '…' : openIncidents,
      icon: <AlertTriangle size={20} />,
      color: 'text-critical',
      trend: openIncidents > 0
        ? { direction: 'down' as const, label: `${openIncidents} ${t('dashboard.activeNow')}` }
        : undefined,
      sparkline: openIncidents > 0 ? { data: [5, 3, 4, 2, 1, openIncidents, openIncidents], color: 'var(--t-critical)' } : undefined,
    },
  ];

  // Alertas de atenção com severidade
  const attentionAlerts: Array<{
    icon: React.ReactNode;
    title: string;
    description?: string;
    severity: 'critical' | 'warning' | 'info';
    to: string;
  }> = [];

  if (openIncidents > 0 && isRouteAvailableInFinalProductionScope('/operations/incidents')) {
    attentionAlerts.push({
      icon: <AlertTriangle size={14} />,
      title: t('dashboard.alertOpenIncidents', { count: openIncidents }),
      description: t('dashboard.alertOpenIncidentsDesc', 'Requires immediate attention'),
      severity: 'critical',
      to: '/operations/incidents',
    });
  }
  if ((changesSummary?.changesNeedingAttention ?? 0) > 0) {
    attentionAlerts.push({
      icon: <Clock size={14} />,
      title: t('dashboard.alertPendingChanges', { count: changesSummary?.changesNeedingAttention }),
      description: t('dashboard.alertPendingChangesDesc', 'Pending review or approval'),
      severity: 'warning',
      to: '/changes',
    });
  }
  if ((changesSummary?.suspectedRegressions ?? 0) > 0) {
    attentionAlerts.push({
      icon: <AlertCircle size={14} />,
      title: t('dashboard.alertSuspectedRegressions', { count: changesSummary?.suspectedRegressions }),
      severity: 'critical',
      to: '/changes',
    });
  }
  if ((contractsSummary?.deprecatedCount ?? 0) > 0 && isRouteAvailableInFinalProductionScope('/contracts')) {
    attentionAlerts.push({
      icon: <FileText size={14} />,
      title: t('dashboard.alertDeprecatedContracts', { count: contractsSummary?.deprecatedCount }),
      severity: 'warning',
      to: '/contracts',
    });
  }

  /** Paleta de estilos dos alertas estruturados. */
  const alertStyles = {
    critical: {
      wrap: 'bg-critical/6 border-critical/18 hover:bg-critical/10',
      iconBox: 'bg-critical/12 text-critical',
      chevron: 'text-critical/50',
    },
    warning: {
      wrap: 'bg-warning/6 border-warning/18 hover:bg-warning/10',
      iconBox: 'bg-warning/12 text-warning',
      chevron: 'text-warning/50',
    },
    info: {
      wrap: 'bg-info/6 border-info/18 hover:bg-info/10',
      iconBox: 'bg-info/12 text-info',
      chevron: 'text-info/50',
    },
  };

  return (
    <PageContainer>
      {/* ── Cabeçalho padronizado com persona badge ───────────────────────────── */}
      <PageHeader
        title={t('dashboard.title')}
        subtitle={t(config.homeSubtitleKey)}
        badge={
          <span className="text-[11px] font-semibold px-2.5 py-1 rounded-full bg-accent/10 text-accent border border-accent/15">
            {t(`persona.${persona}.label`)}
          </span>
        }
      />

      {/* ── Persona switcher decorativo ───────────────────────────────────────── */}
      <div className="flex items-center gap-1.5 mb-6 overflow-x-auto pb-0.5 scrollbar-none">
        {PERSONA_CHIPS.map((chip) => (
          <span
            key={chip.id}
            className={
              chip.id === persona
                ? 'shrink-0 px-3 py-1 rounded-full text-[11px] font-semibold bg-accent/12 text-accent border border-accent/25 cursor-default'
                : 'shrink-0 px-3 py-1 rounded-full text-[11px] font-medium text-muted border border-edge bg-elevated cursor-default'
            }
          >
            {t(chip.labelKey)}
          </span>
        ))}
      </div>

      {/* Erro de carregamento */}
      {graphError && (
        <div role="alert" className="mb-6 rounded-lg bg-critical/10 border border-critical/25 px-4 py-3 flex items-start gap-3 animate-fade-in">
          <AlertTriangle size={18} className="text-critical shrink-0 mt-0.5" />
          <div>
            <p className="text-sm font-medium text-critical">{t('common.error')}</p>
            <p className="text-xs text-muted mt-0.5">{t('common.errorDescription')}</p>
          </div>
        </div>
      )}

      {/* ── KPI Stats — 4 colunas ────────────────────────────────────────────── */}
      <div className="mb-6">
        <ContentGrid columns={4}>
          {stats.map((s) => (
            <StatCard key={s.title} {...s} />
          ))}
        </ContentGrid>
      </div>

      {/* ── Alertas estruturados ─────────────────────────────────────────────── */}
      {attentionAlerts.length > 0 && (
        <div className="mb-6 space-y-2">
          {attentionAlerts.map((alert, idx) => {
            const styles = alertStyles[alert.severity];
            return (
              <Link
                key={`${alert.to}-${idx}`}
                to={alert.to}
                className={`flex items-center gap-3 rounded-md border px-4 py-3 transition-colors ${styles.wrap}`}
              >
                {/* Icon box 28×28 tonal */}
                <div
                  className={`shrink-0 flex items-center justify-center rounded-lg ${styles.iconBox}`}
                  style={{ width: 28, height: 28 }}
                  aria-hidden="true"
                >
                  {alert.icon}
                </div>
                <div className="flex-1 min-w-0">
                  <p className="text-sm font-semibold leading-tight">{alert.title}</p>
                  {alert.description && (
                    <p className="text-xs mt-0.5 opacity-70">{alert.description}</p>
                  )}
                </div>
                <ChevronRight size={14} className={`shrink-0 ${styles.chevron}`} />
              </Link>
            );
          })}
        </div>
      )}

      {/* ── Operational views: Services + Contract Health ────────────────────── */}
      <PageSection>
        <ContentGrid columns={showContractsSurface ? 2 : 1}>
        <Card>
          <CardHeader dot="var(--t-cyan)">
            <div className="flex items-center justify-between w-full">
              <h2 className="text-sm font-semibold text-heading">{t('dashboard.recentServices')}</h2>
              <Link to="/services" className="text-xs text-accent hover:text-accent-hover flex items-center gap-1 transition-colors">
                {t('common.viewAll')} <ArrowRight size={12} />
              </Link>
            </div>
          </CardHeader>
          <CardBody className="p-0">
            {graphLoading ? (
              <div className="space-y-0">
                {[1, 2, 3].map((i) => (
                  <div key={i} className="px-5 py-3 flex items-center gap-3 border-b border-edge last:border-b-0">
                    <div className="w-8 h-8 skeleton rounded" />
                    <div className="flex-1">
                      <div className="h-3.5 w-32 skeleton mb-1.5" />
                      <div className="h-3 w-20 skeleton" />
                    </div>
                  </div>
                ))}
              </div>
            ) : !graph?.services?.length ? (
              <EmptyState
                icon={<Activity size={20} />}
                title={t('dashboard.noServices')}
                description={t('productPolish.emptyServices')}
                action={
                  <Link to="/services" className="text-xs text-accent hover:text-accent-hover transition-colors">
                    {t('common.getStarted')} <ArrowRight size={10} className="inline" />
                  </Link>
                }
              />
            ) : (
              <ul className="divide-y divide-edge">
                {graph.services.slice(0, 5).map((svc) => (
                  <li key={svc.serviceAssetId}>
                    <Link
                      to={`/services/${svc.serviceAssetId}`}
                      className="px-5 py-2.5 flex items-center gap-3 hover:bg-hover transition-colors"
                    >
                      <div className="w-8 h-8 rounded-md bg-accent/10 flex items-center justify-center text-accent font-semibold text-xs">
                        {svc.name[0]}
                      </div>
                      <div className="flex-1 min-w-0">
                        <p className="text-sm font-medium text-heading truncate">{svc.name}</p>
                        <p className="text-xs text-muted truncate">{svc.teamName}</p>
                      </div>
                      <Badge variant={svc.criticality === 'Critical' || svc.criticality === 'High' ? 'danger' : svc.criticality === 'Medium' ? 'warning' : 'info'}>
                        {svc.criticality}
                      </Badge>
                    </Link>
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>

        {showContractsSurface && (
        <Card>
          <CardHeader dot="var(--t-success)">
            <div className="flex items-center justify-between w-full">
              <h2 className="text-sm font-semibold text-heading">{t('dashboard.contractHealth')}</h2>
              <Link to="/contracts" className="text-xs text-accent hover:text-accent-hover flex items-center gap-1 transition-colors">
                {t('common.viewAll')} <ArrowRight size={12} />
              </Link>
            </div>
          </CardHeader>
          <CardBody>
            {contractsLoading ? (
              <div className="grid grid-cols-2 gap-3">
                {[1, 2, 3, 4].map((i) => (
                  <div key={i} className="p-3 rounded-lg bg-elevated">
                    <div className="h-3 w-16 skeleton mb-2" />
                    <div className="h-5 w-8 skeleton" />
                  </div>
                ))}
              </div>
            ) : !contractsSummary ? (
              <EmptyState
                icon={<FileText size={20} />}
                title={t('dashboard.noContracts')}
                description={t('productPolish.emptyContracts')}
                action={
                  <Link to="/contracts/studio" className="text-xs text-accent hover:text-accent-hover transition-colors">
                    {t('common.getStarted')} <ArrowRight size={10} className="inline" />
                  </Link>
                }
              />
            ) : (
              <div className="grid grid-cols-2 gap-3">
                <div className="p-3 rounded-lg bg-elevated border border-edge/50">
                  <p className="text-xs text-muted mb-1">{t('dashboard.contractDrafts')}</p>
                  <p className="text-lg font-semibold text-heading">{contractsSummary.draftCount ?? 0}</p>
                </div>
                <div className="p-3 rounded-lg bg-elevated border border-edge/50">
                  <p className="text-xs text-muted mb-1">{t('dashboard.contractInReview')}</p>
                  <p className="text-lg font-semibold text-warning">{contractsSummary.inReviewCount ?? 0}</p>
                </div>
                <div className="p-3 rounded-lg bg-elevated border border-edge/50">
                  <p className="text-xs text-muted mb-1">{t('dashboard.contractApproved')}</p>
                  <p className="text-lg font-semibold text-success">{contractsSummary.approvedCount ?? 0}</p>
                </div>
                <div className="p-3 rounded-lg bg-elevated border border-edge/50">
                  <p className="text-xs text-muted mb-1">{t('dashboard.contractDeprecated')}</p>
                  <p className="text-lg font-semibold text-critical">{contractsSummary.deprecatedCount ?? 0}</p>
                </div>
              </div>
            )}
          </CardBody>
          <CardFooter>
            <div className="space-y-3">
              <StackedProgressBar
                segments={[
                  { value: Math.round(((contractsSummary?.approvedCount ?? 0) / Math.max(totalContracts, 1)) * 100), color: 'bg-success', label: t('dashboard.contractApproved') },
                  { value: Math.round(((contractsSummary?.inReviewCount ?? 0) / Math.max(totalContracts, 1)) * 100), color: 'bg-warning', label: t('dashboard.contractInReview') },
                  { value: Math.round(((contractsSummary?.draftCount ?? 0) / Math.max(totalContracts, 1)) * 100), color: 'bg-muted', label: t('dashboard.contractDrafts') },
                  { value: Math.round(((contractsSummary?.deprecatedCount ?? 0) / Math.max(totalContracts, 1)) * 100), color: 'bg-critical', label: t('dashboard.contractDeprecated') },
                ]}
                height="sm"
              />
              <div className="flex items-center justify-between">
                <p className="text-[11px] text-muted">{t('dashboard.contractDistribution')}</p>
                <Link
                  to="/governance/reports"
                  className="inline-flex items-center gap-1 text-[11px] text-accent hover:text-accent-hover transition-colors font-medium"
                >
                  <Download size={10} />
                  {t('dashboard.viewReport')}
                </Link>
              </div>
            </div>
          </CardFooter>
        </Card>
        )}
        </ContentGrid>
      </PageSection>

      {/* ── Operational Intelligence: Changes + Incidents ────────────────────── */}
      <PageSection>
        <ContentGrid columns={2}>
        <Card>
          <CardHeader dot="var(--t-warning)">
            <div className="flex items-center justify-between w-full">
              <h2 className="text-sm font-semibold text-heading">{t('dashboard.changeOverview')}</h2>
              <Link to="/changes" className="text-xs text-accent hover:text-accent-hover flex items-center gap-1 transition-colors">
                {t('common.viewAll')} <ArrowRight size={12} />
              </Link>
            </div>
          </CardHeader>
          <CardBody>
            {changesLoading ? (
              <div className="grid grid-cols-3 gap-3">
                {[1, 2, 3].map((i) => (
                  <div key={i} className="p-3 rounded-lg bg-elevated text-center">
                    <div className="h-3 w-16 skeleton mx-auto mb-2" />
                    <div className="h-5 w-8 skeleton mx-auto" />
                  </div>
                ))}
              </div>
            ) : !changesSummary ? (
              <EmptyState
                icon={<Zap size={20} />}
                title={t('dashboard.noChanges')}
                description={t('productPolish.guidanceChanges')}
                action={
                  <Link to="/changes" className="text-xs text-accent hover:text-accent-hover transition-colors">
                    {t('common.explore')} <ArrowRight size={10} className="inline" />
                  </Link>
                }
              />
            ) : (
              <div className="grid grid-cols-3 gap-3">
                <div className="p-3 rounded-lg bg-elevated border border-edge/50 text-center">
                  <p className="text-xs text-muted mb-1">{t('dashboard.changePending')}</p>
                  <p className="text-lg font-semibold text-warning">{changesSummary.changesNeedingAttention ?? 0}</p>
                </div>
                <div className="p-3 rounded-lg bg-elevated border border-edge/50 text-center">
                  <p className="text-xs text-muted mb-1">{t('dashboard.changeApproved')}</p>
                  <p className="text-lg font-semibold text-success">{changesSummary.validatedChanges ?? 0}</p>
                </div>
                <div className="p-3 rounded-lg bg-elevated border border-edge/50 text-center">
                  <p className="text-xs text-muted mb-1">{t('dashboard.changeRejected')}</p>
                  <p className="text-lg font-semibold text-critical">{changesSummary.suspectedRegressions ?? 0}</p>
                </div>
              </div>
            )}
          </CardBody>
        </Card>

        <Card>
          <CardHeader dot="var(--t-critical)" pulsing={openIncidents > 0}>
            <div className="flex items-center justify-between w-full">
              <h2 className="text-sm font-semibold text-heading">{t('dashboard.incidentOverview')}</h2>
              <Link to="/operations/incidents" className="text-xs text-accent hover:text-accent-hover flex items-center gap-1 transition-colors">
                {t('common.viewAll')} <ArrowRight size={12} />
              </Link>
            </div>
          </CardHeader>
          <CardBody>
            {incidentsLoading ? (
              <div className="grid grid-cols-3 gap-3">
                {[1, 2, 3].map((i) => (
                  <div key={i} className="p-3 rounded-lg bg-elevated text-center">
                    <div className="h-3 w-16 skeleton mx-auto mb-2" />
                    <div className="h-5 w-8 skeleton mx-auto" />
                  </div>
                ))}
              </div>
            ) : !incidentsSummary ? (
              <EmptyState
                icon={<AlertTriangle size={20} />}
                title={t('dashboard.noIncidents')}
                description={t('productPolish.guidanceOperations')}
                action={
                  <Link to="/operations/incidents" className="text-xs text-accent hover:text-accent-hover transition-colors">
                    {t('common.explore')} <ArrowRight size={10} className="inline" />
                  </Link>
                }
              />
            ) : (
              <div className="grid grid-cols-3 gap-3">
                <div className="p-3 rounded-lg bg-elevated border border-edge/50 text-center">
                  <p className="text-xs text-muted mb-1">{t('dashboard.incidentOpen')}</p>
                  <p className="text-lg font-semibold text-critical">{incidentsSummary.statusBreakdown?.open ?? incidentsSummary.totalOpen ?? 0}</p>
                </div>
                <div className="p-3 rounded-lg bg-elevated border border-edge/50 text-center">
                  <p className="text-xs text-muted mb-1">{t('dashboard.incidentMitigating')}</p>
                  <p className="text-lg font-semibold text-warning">{incidentsSummary.statusBreakdown?.mitigating ?? 0}</p>
                </div>
                <div className="p-3 rounded-lg bg-elevated border border-edge/50 text-center">
                  <p className="text-xs text-muted mb-1">{t('dashboard.incidentResolved')}</p>
                  <p className="text-lg font-semibold text-success">{incidentsSummary.statusBreakdown?.resolved ?? 0}</p>
                </div>
              </div>
            )}
          </CardBody>
        </Card>
        </ContentGrid>
      </PageSection>
    </PageContainer>
  );
}
