import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Zap, GitBranch, FileText, CheckSquare, Activity, AlertTriangle } from 'lucide-react';
import { StatCard } from '../../../components/StatCard';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';
import { QuickActions } from '../../../components/QuickActions';
import { PersonaQuickstart } from '../../../components/PersonaQuickstart';
import { HomeWidgetCard } from '../../../components/HomeWidgetCard';
import { usePersona } from '../../../contexts/PersonaContext';
import { serviceCatalogApi } from '../../catalog/api/serviceCatalog';

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

  const stats = [
    {
      title: t('dashboard.activeServices'),
      value: graphLoading ? '…' : (graph?.services?.length ?? '—'),
      icon: <Activity size={24} />,
      color: 'text-brand-blue',
    },
    {
      title: t('dashboard.registeredApis'),
      value: graphLoading ? '…' : (graph?.apis?.length ?? '—'),
      icon: <GitBranch size={24} />,
      color: 'text-accent',
    },
    {
      title: t('dashboard.consumerRelations'),
      value: graphLoading ? '…' : (graph?.apis?.reduce((sum: number, a: { consumers?: unknown[] }) => sum + (a.consumers?.length ?? 0), 0) ?? '—'),
      icon: <Zap size={24} />,
      color: 'text-warning',
    },
    {
      title: t('dashboard.totalContracts'),
      value: '—',
      icon: <FileText size={24} />,
      color: 'text-success',
    },
    {
      title: t('dashboard.pendingApprovals'),
      value: '—',
      icon: <CheckSquare size={24} />,
      color: 'text-brand-purple',
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

      {/* KPI Stats — comuns a todas as personas */}
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

      {/* Services & APIs overview — mantido para contexto geral */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <Card>
          <CardHeader>
            <h2 className="text-base font-semibold text-heading">{t('dashboard.recentServices')}</h2>
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
                  <li key={svc.serviceAssetId} className="px-6 py-3 flex items-center gap-3 hover:bg-hover transition-colors">
                    <div className="w-8 h-8 rounded bg-accent/15 flex items-center justify-center text-accent font-semibold text-sm">
                      {svc.name[0]}
                    </div>
                    <div>
                      <p className="text-sm font-medium text-heading">{svc.name}</p>
                      <p className="text-xs text-muted">{svc.teamName}</p>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <h2 className="text-base font-semibold text-heading">{t('dashboard.registeredApisTitle')}</h2>
          </CardHeader>
          <CardBody className="p-0">
            {!graph?.apis?.length ? (
              <EmptyState
                icon={<GitBranch size={20} />}
                title={t('dashboard.noApis')}
                description={t('productPolish.emptyContracts')}
              />
            ) : (
              <ul className="divide-y divide-edge">
                {graph.apis.slice(0, 5).map((api) => (
                  <li key={api.apiAssetId} className="px-6 py-3 flex items-center justify-between hover:bg-hover transition-colors">
                    <div>
                      <p className="text-sm font-medium text-heading">{api.name}</p>
                      <p className="text-xs text-muted font-mono">{api.routePattern}</p>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </CardBody>
        </Card>
      </div>
    </div>
  );
}
