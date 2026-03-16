import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Link } from 'react-router-dom';
import {
  Zap, GitBranch, FileText, ShieldCheck, Activity, AlertTriangle,
  ArrowRight,
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
 * Página principal do dashboard — experiência persona-aware.
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
 * The dashboard now shows real operational data from all four core flows:
 * 1. Services & Source of Truth (graph)
 * 2. Contract Governance (summary)
 * 3. Change Confidence (summary)
 * 4. Incident & Operations (summary)
 *
 * @see docs/PERSONA-UX-MAPPING.md
 * @see docs/CORE-FLOW-GAPS.md
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

  const stats = [
    {
      title: t('dashboard.activeServices'),
      value: graphLoading ? '…' : (graph?.services?.length ?? '—'),
      icon: <Activity size={24} />,
      color: 'text-brand-blue',
    },
    {
      title: t('dashboard.totalContracts'),
      value: contractsLoading ? '…' : (contractsSummary?.totalVersions ?? '—'),
      icon: <FileText size={24} />,
      color: 'text-success',
    },
    {
      title: t('dashboard.recentChanges'),
      value: changesLoading ? '…' : (changesSummary?.totalChanges ?? '—'),
      icon: <ShieldCheck size={24} />,
      color: 'text-warning',
    },
    {
      title: t('dashboard.openIncidents'),
      value: incidentsLoading ? '…' : (incidentsSummary?.totalOpen ?? '—'),
      icon: <AlertTriangle size={24} />,
      color: 'text-critical',
    },
    {
      title: t('dashboard.registeredApis'),
      value: graphLoading ? '…' : (graph?.apis?.length ?? '—'),
      icon: <GitBranch size={24} />,
      color: 'text-accent',
    },
  ];

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* Page title — persona-aware subtitle */}
      <div className="mb-6">
        <div className="flex items-center gap-3">
          <h1 className="text-2xl font-bold text-heading">{t('dashboard.title')}</h1>
          <span className="text-xs font-medium px-2.5 py-1 rounded-full bg-accent/10 text-accent">
            {t(`persona.${persona}.label`)}
          </span>
        </div>
        <p className="text-muted mt-1">{t(config.homeSubtitleKey)}</p>
      </div>

      {/* Quick Actions — adaptadas à persona */}
      <QuickActions />

      {/* Quickstart persona-aware — orientação para novos utilizadores */}
      <PersonaQuickstart />

      {/* Error state */}
      {graphError && (
        <Card className="mb-8">
          <CardBody>
            <div className="flex items-center gap-3 py-2">
              <AlertTriangle size={20} className="text-critical shrink-0" />
              <div>
                <p className="text-sm font-medium text-critical">{t('common.error')}</p>
                <p className="text-xs text-muted">{t('common.errorDescription')}</p>
              </div>
            </div>
          </CardBody>
        </Card>
      )}

      {/* KPI Stats — operational metrics across all four core flows */}
      <div className="grid grid-cols-2 lg:grid-cols-5 gap-4 mb-8">
        {stats.map((s) => (
          <StatCard key={s.title} {...s} />
        ))}
      </div>

      {/* Persona-specific widgets */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-8">
        {config.homeWidgets.map((widget) => (
          <HomeWidgetCard key={widget.id} widget={widget} />
        ))}
      </div>

      {/* Core operational views: Services + Contract Health */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 mb-6">
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between w-full">
              <h2 className="text-base font-semibold text-heading">{t('dashboard.recentServices')}</h2>
              <Link to="/services" className="text-xs text-accent hover:text-accent/80 flex items-center gap-1">
                {t('common.viewAll')} <ArrowRight size={12} />
              </Link>
            </div>
          </CardHeader>
          <CardBody className="p-0">
            {!graph?.services?.length ? (
              <EmptyState
                icon={<Activity size={20} />}
                title={t('dashboard.noServices')}
                description={t('productPolish.emptyServices')}
              />
            ) : (
              <ul className="divide-y divide-edge">
                {graph.services.slice(0, 5).map((svc) => (
                  <li key={svc.serviceAssetId}>
                    <Link
                      to={`/services/${svc.serviceAssetId}`}
                      className="px-6 py-3 flex items-center gap-3 hover:bg-hover transition-colors"
                    >
                      <div className="w-8 h-8 rounded bg-accent/15 flex items-center justify-center text-accent font-semibold text-sm">
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
              <h2 className="text-base font-semibold text-heading">{t('dashboard.contractHealth')}</h2>
              <Link to="/contracts" className="text-xs text-accent hover:text-accent/80 flex items-center gap-1">
                {t('common.viewAll')} <ArrowRight size={12} />
              </Link>
            </div>
          </CardHeader>
          <CardBody>
            {contractsLoading ? (
              <div className="flex items-center justify-center py-8">
                <div className="h-6 w-6 animate-spin rounded-full border-2 border-accent border-t-transparent" />
              </div>
            ) : !contractsSummary ? (
              <EmptyState
                icon={<FileText size={20} />}
                title={t('dashboard.noContracts')}
                description={t('productPolish.emptyContracts')}
              />
            ) : (
              <div className="grid grid-cols-2 gap-3">
                <div className="p-3 rounded-lg bg-elevated">
                  <p className="text-xs text-muted mb-1">{t('dashboard.contractDrafts')}</p>
                  <p className="text-lg font-semibold text-heading">{contractsSummary.draftCount ?? 0}</p>
                </div>
                <div className="p-3 rounded-lg bg-elevated">
                  <p className="text-xs text-muted mb-1">{t('dashboard.contractInReview')}</p>
                  <p className="text-lg font-semibold text-warning">{contractsSummary.inReviewCount ?? 0}</p>
                </div>
                <div className="p-3 rounded-lg bg-elevated">
                  <p className="text-xs text-muted mb-1">{t('dashboard.contractApproved')}</p>
                  <p className="text-lg font-semibold text-success">{contractsSummary.approvedCount ?? 0}</p>
                </div>
                <div className="p-3 rounded-lg bg-elevated">
                  <p className="text-xs text-muted mb-1">{t('dashboard.contractDeprecated')}</p>
                  <p className="text-lg font-semibold text-critical">{contractsSummary.deprecatedCount ?? 0}</p>
                </div>
              </div>
            )}
          </CardBody>
        </Card>
      </div>

      {/* Operational intelligence: Changes + Incidents */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between w-full">
              <h2 className="text-base font-semibold text-heading">{t('dashboard.changeOverview')}</h2>
              <Link to="/changes" className="text-xs text-accent hover:text-accent/80 flex items-center gap-1">
                {t('common.viewAll')} <ArrowRight size={12} />
              </Link>
            </div>
          </CardHeader>
          <CardBody>
            {changesLoading ? (
              <div className="flex items-center justify-center py-8">
                <div className="h-6 w-6 animate-spin rounded-full border-2 border-accent border-t-transparent" />
              </div>
            ) : !changesSummary ? (
              <EmptyState
                icon={<Zap size={20} />}
                title={t('dashboard.noChanges')}
                description={t('productPolish.guidanceChanges')}
              />
            ) : (
              <div className="grid grid-cols-3 gap-3">
                <div className="p-3 rounded-lg bg-elevated text-center">
                  <p className="text-xs text-muted mb-1">{t('dashboard.changePending')}</p>
                  <p className="text-lg font-semibold text-warning">{changesSummary.changesNeedingAttention ?? 0}</p>
                </div>
                <div className="p-3 rounded-lg bg-elevated text-center">
                  <p className="text-xs text-muted mb-1">{t('dashboard.changeApproved')}</p>
                  <p className="text-lg font-semibold text-success">{changesSummary.validatedChanges ?? 0}</p>
                </div>
                <div className="p-3 rounded-lg bg-elevated text-center">
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
              <h2 className="text-base font-semibold text-heading">{t('dashboard.incidentOverview')}</h2>
              <Link to="/operations/incidents" className="text-xs text-accent hover:text-accent/80 flex items-center gap-1">
                {t('common.viewAll')} <ArrowRight size={12} />
              </Link>
            </div>
          </CardHeader>
          <CardBody>
            {incidentsLoading ? (
              <div className="flex items-center justify-center py-8">
                <div className="h-6 w-6 animate-spin rounded-full border-2 border-accent border-t-transparent" />
              </div>
            ) : !incidentsSummary ? (
              <EmptyState
                icon={<AlertTriangle size={20} />}
                title={t('dashboard.noIncidents')}
                description={t('productPolish.guidanceOperations')}
              />
            ) : (
              <div className="grid grid-cols-3 gap-3">
                <div className="p-3 rounded-lg bg-elevated text-center">
                  <p className="text-xs text-muted mb-1">{t('dashboard.incidentOpen')}</p>
                  <p className="text-lg font-semibold text-critical">{incidentsSummary.statusBreakdown?.open ?? incidentsSummary.totalOpen ?? 0}</p>
                </div>
                <div className="p-3 rounded-lg bg-elevated text-center">
                  <p className="text-xs text-muted mb-1">{t('dashboard.incidentMitigating')}</p>
                  <p className="text-lg font-semibold text-warning">{incidentsSummary.statusBreakdown?.mitigating ?? 0}</p>
                </div>
                <div className="p-3 rounded-lg bg-elevated text-center">
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
