import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  Zap, GitBranch, FileText, ShieldCheck, Activity, AlertTriangle,
  ArrowRight, TrendingUp, TrendingDown, AlertCircle, Clock, Server,
} from 'lucide-react';
import { StatCard } from '../../../components/StatCard';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { EmptyState } from '../../../components/EmptyState';
import { QuickActions } from '../../../components/QuickActions';
import { PersonaQuickstart } from '../../../components/PersonaQuickstart';
import { HomeWidgetCard } from '../../../components/HomeWidgetCard';
import { usePersona } from '../../../contexts/PersonaContext';
import { serviceCatalogApi } from '../../catalog/api/serviceCatalog';
import { contractsApi } from '../../catalog/api/contracts';
import { changeConfidenceApi } from '../../change-governance/api/changeConfidence';
import { incidentsApi } from '../../operations/api/incidents';

/**
 * Página principal do dashboard — experiência persona-aware com narrativa operacional.
 *
 * Adapta conteúdo, ordem e linguagem com base na persona do utilizador:
 * - Engineer: foco operacional nos próprios serviços
 * - Tech Lead: visão da equipa e risco
 * - Architect: dependências e consistência
 * - Product: impacto funcional e confiança de release
 * - Executive: visão agregada e estratégica
 * - Platform Admin: governança e administração
 * - Auditor: rastreabilidade e evidências
 *
 * Layout:
 * 1. Context bar (title + persona + subtitle)
 * 2. KPI storytelling row
 * 3. Attention alerts (priority items)
 * 4. Operational widgets (real data)
 *
 * @see docs/PERSONA-UX-MAPPING.md
 */
export function DashboardPage() {
  const { t } = useTranslation();
  const { persona, config } = usePersona();

  const { data: graph, isLoading: graphLoading, isError: graphError } = useQuery({
    queryKey: ['graph'],
    queryFn: () => serviceCatalogApi.getGraph(),
    staleTime: 30_000,
  });

  const { data: contractsSummary, isLoading: contractsLoading } = useQuery({
    queryKey: ['contracts', 'summary'],
    queryFn: () => contractsApi.getContractsSummary(),
    staleTime: 30_000,
  });

  const { data: changesSummary, isLoading: changesLoading } = useQuery({
    queryKey: ['changes', 'summary'],
    queryFn: () => changeConfidenceApi.getSummary(),
    staleTime: 30_000,
  });

  const { data: incidentsSummary, isLoading: incidentsLoading } = useQuery({
    queryKey: ['incidents', 'summary'],
    queryFn: () => incidentsApi.getIncidentSummary(),
    staleTime: 30_000,
  });

  const totalServices = graph?.services?.length ?? 0;
  const totalApis = graph?.apis?.length ?? 0;
  const totalContracts = contractsSummary?.totalVersions ?? 0;
  const totalChanges = changesSummary?.totalChanges ?? 0;
  const openIncidents = incidentsSummary?.totalOpen ?? 0;

  const stats = [
    {
      title: t('dashboard.activeServices'),
      value: graphLoading ? '…' : totalServices,
      icon: <Activity size={22} />,
      color: 'text-brand-blue',
      trend: totalServices > 0 ? { direction: 'up' as const, label: t('dashboard.trendHealthy') } : undefined,
    },
    {
      title: t('dashboard.totalContracts'),
      value: contractsLoading ? '…' : totalContracts,
      icon: <FileText size={22} />,
      color: 'text-success',
      trend: (contractsSummary?.inReviewCount ?? 0) > 0
        ? { direction: 'up' as const, label: `${contractsSummary?.inReviewCount} ${t('dashboard.inReviewShort')}` }
        : undefined,
    },
    {
      title: t('dashboard.recentChanges'),
      value: changesLoading ? '…' : totalChanges,
      icon: <ShieldCheck size={22} />,
      color: 'text-warning',
      trend: (changesSummary?.changesNeedingAttention ?? 0) > 0
        ? { direction: 'down' as const, label: `${changesSummary?.changesNeedingAttention} ${t('dashboard.needAttention')}` }
        : undefined,
    },
    {
      title: t('dashboard.openIncidents'),
      value: incidentsLoading ? '…' : openIncidents,
      icon: <AlertTriangle size={22} />,
      color: 'text-critical',
      trend: openIncidents > 0
        ? { direction: 'down' as const, label: `${openIncidents} ${t('dashboard.activeNow')}` }
        : undefined,
    },
    {
      title: t('dashboard.registeredApis'),
      value: graphLoading ? '…' : totalApis,
      icon: <GitBranch size={22} />,
      color: 'text-accent',
    },
  ];

  // Build attention alerts from operational data
  const attentionAlerts: Array<{ icon: React.ReactNode; text: string; severity: 'critical' | 'warning' | 'info'; to: string }> = [];
  if (openIncidents > 0) {
    attentionAlerts.push({
      icon: <AlertTriangle size={14} />,
      text: t('dashboard.alertOpenIncidents', { count: openIncidents }),
      severity: 'critical',
      to: '/operations/incidents',
    });
  }
  if ((changesSummary?.changesNeedingAttention ?? 0) > 0) {
    attentionAlerts.push({
      icon: <Clock size={14} />,
      text: t('dashboard.alertPendingChanges', { count: changesSummary?.changesNeedingAttention }),
      severity: 'warning',
      to: '/changes',
    });
  }
  if ((changesSummary?.suspectedRegressions ?? 0) > 0) {
    attentionAlerts.push({
      icon: <AlertCircle size={14} />,
      text: t('dashboard.alertSuspectedRegressions', { count: changesSummary?.suspectedRegressions }),
      severity: 'critical',
      to: '/changes',
    });
  }
  if ((contractsSummary?.deprecatedCount ?? 0) > 0) {
    attentionAlerts.push({
      icon: <FileText size={14} />,
      text: t('dashboard.alertDeprecatedContracts', { count: contractsSummary?.deprecatedCount }),
      severity: 'warning',
      to: '/contracts',
    });
  }

  const severityColors = {
    critical: 'bg-critical/10 border-critical/25 text-critical',
    warning: 'bg-warning/10 border-warning/25 text-warning',
    info: 'bg-info/10 border-info/25 text-info',
  };

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* ── Context bar ──────────────────────────────────────────────────────── */}
      <div className="mb-6">
        <div className="flex items-center gap-3">
          <h1 className="text-2xl font-bold text-heading">{t('dashboard.title')}</h1>
          <span className="text-xs font-medium px-2.5 py-1 rounded-full bg-accent/10 text-accent">
            {t(`persona.${persona}.label`)}
          </span>
        </div>
        <p className="text-sm text-muted mt-1">{t(config.homeSubtitleKey)}</p>
      </div>

      {/* Quick Actions — adaptadas à persona */}
      <QuickActions />

      {/* Quickstart persona-aware — orientação para novos utilizadores */}
      <PersonaQuickstart />

      {/* Error state */}
      {graphError && (
        <div role="alert" className="mb-6 rounded-lg bg-critical/10 border border-critical/25 px-4 py-3 flex items-start gap-3 animate-fade-in">
          <AlertTriangle size={18} className="text-critical shrink-0 mt-0.5" />
          <div>
            <p className="text-sm font-medium text-critical">{t('common.error')}</p>
            <p className="text-xs text-muted mt-0.5">{t('common.errorDescription')}</p>
          </div>
        </div>
      )}

      {/* ── KPI Stats ────────────────────────────────────────────────────────── */}
      <div className="grid grid-cols-2 lg:grid-cols-5 gap-4 mb-6">
        {stats.map((s) => (
          <StatCard key={s.title} {...s} />
        ))}
      </div>

      {/* ── Attention Alerts ─────────────────────────────────────────────────── */}
      {attentionAlerts.length > 0 && (
        <div className="mb-6 space-y-2">
          {attentionAlerts.map((alert, idx) => (
            <Link
              key={idx}
              to={alert.to}
              className={`flex items-center gap-3 rounded-lg border px-4 py-2.5 text-sm transition-colors hover:opacity-80 ${severityColors[alert.severity]}`}
            >
              {alert.icon}
              <span className="flex-1">{alert.text}</span>
              <ArrowRight size={14} className="opacity-60" />
            </Link>
          ))}
        </div>
      )}

      {/* Persona-specific widgets */}
      {config.homeWidgets.length > 0 && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-5 mb-6">
          {config.homeWidgets.map((widget) => (
            <HomeWidgetCard key={widget.id} widget={widget} />
          ))}
        </div>
      )}

      {/* ── Operational views: Services + Contract Health ────────────────────── */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-5 mb-5">
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between w-full">
              <div className="flex items-center gap-2">
                <Server size={16} className="text-brand-blue" />
                <h2 className="text-sm font-semibold text-heading">{t('dashboard.recentServices')}</h2>
              </div>
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

        <Card>
          <CardHeader>
            <div className="flex items-center justify-between w-full">
              <div className="flex items-center gap-2">
                <FileText size={16} className="text-success" />
                <h2 className="text-sm font-semibold text-heading">{t('dashboard.contractHealth')}</h2>
              </div>
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
        </Card>
      </div>

      {/* ── Operational Intelligence: Changes + Incidents ────────────────────── */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-5">
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between w-full">
              <div className="flex items-center gap-2">
                <ShieldCheck size={16} className="text-warning" />
                <h2 className="text-sm font-semibold text-heading">{t('dashboard.changeOverview')}</h2>
              </div>
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
          <CardHeader>
            <div className="flex items-center justify-between w-full">
              <div className="flex items-center gap-2">
                <AlertTriangle size={16} className="text-critical" />
                <h2 className="text-sm font-semibold text-heading">{t('dashboard.incidentOverview')}</h2>
              </div>
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
      </div>
    </div>
  );
}
