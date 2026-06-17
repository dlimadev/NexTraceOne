import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Server,
  Database,
  CheckCircle2,
  AlertTriangle,
  XCircle,
  RefreshCw,
  Shield,
  ArrowRight,
  Layers,
  Settings,
  BrainCircuit,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageContainer, PageSection, StatsGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { StatCard } from '../../../components/StatCard';
import { Tabs, TabPanel } from '../../../components/Tabs';
import { platformAdminApi } from '../api/platformAdmin';
import { platformOpsApi } from '../../operations/api/platformOps';
import type { ConfigCheckStatus, MigrationRiskLevel } from '../api/platformAdmin';

// ─── Tab type ─────────────────────────────────────────────────────────────────

type Tab = 'overview' | 'config' | 'migrations';

// ─── Helpers ─────────────────────────────────────────────────────────────────

const configStatusIcon = (status: ConfigCheckStatus, size = 15) => {
  switch (status) {
    case 'ok': return <CheckCircle2 size={size} className="text-success shrink-0" />;
    case 'warning': return <AlertTriangle size={size} className="text-warning shrink-0" />;
    case 'degraded': return <XCircle size={size} className="text-critical shrink-0" />;
  }
};

const configStatusBadge = (status: ConfigCheckStatus): 'success' | 'warning' | 'danger' => {
  switch (status) {
    case 'ok': return 'success';
    case 'warning': return 'warning';
    case 'degraded': return 'danger';
  }
};

const riskBadge = (risk: MigrationRiskLevel): 'success' | 'warning' | 'danger' | 'default' => {
  switch (risk) {
    case 'Low': return 'success';
    case 'Medium': return 'warning';
    case 'High': return 'danger';
    case 'Critical': return 'danger';
    default: return 'default';
  }
};

// ─── Overview tab content ─────────────────────────────────────────────────────

function OverviewTab() {
  const { t } = useTranslation();

  const healthQuery = useQuery({
    queryKey: ['platform-health'],
    queryFn: () => platformOpsApi.getHealth(),
    staleTime: 15_000,
  });

  const preflightQuery = useQuery({
    queryKey: ['preflight'],
    queryFn: () => platformAdminApi.getPreflight(),
    staleTime: 30_000,
  });

  if (healthQuery.isLoading || preflightQuery.isLoading) {
    return <PageLoadingState />;
  }

  const health = healthQuery.data;
  const preflight = preflightQuery.data;

  return (
    <div className="space-y-4">
      {/* Sumário dos preflight checks */}
      {preflight && (
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Server size={15} className="text-accent" />
              <span className="text-sm font-semibold text-heading">{t('platformHealth.preflightTitle')}</span>
            </div>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-border">
              {preflight.checks.slice(0, 6).map((check) => (
                <div key={check.name} className="flex items-center justify-between px-4 py-2.5">
                  <div className="flex items-center gap-2 min-w-0">
                    {configStatusIcon(check.status === 'Ok' ? 'ok' : check.status === 'Warning' ? 'warning' : 'degraded')}
                    <span className="text-sm text-heading truncate">{check.name}</span>
                  </div>
                  <Badge variant={configStatusBadge(check.status === 'Ok' ? 'ok' : check.status === 'Warning' ? 'warning' : 'degraded')}>
                    {t(`platformHealth.status.${check.status.toLowerCase()}`)}
                  </Badge>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>
      )}

      {/* Subsistemas da plataforma */}
      {health && (
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Database size={15} className="text-accent" />
              <span className="text-sm font-semibold text-heading">{t('platformHealth.subsystemsTitle')}</span>
            </div>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-border">
              {health.subsystems.map((sub: { name: string; status: string; description: string }) => (
                <div key={sub.name} className="flex items-center justify-between px-4 py-2.5">
                  <span className="text-sm text-heading">{sub.name}</span>
                  <div className="flex items-center gap-2">
                    <span className="text-xs text-muted truncate max-w-[200px]">{sub.description}</span>
                    <Badge variant={sub.status === 'Healthy' ? 'success' : sub.status === 'Degraded' ? 'warning' : 'danger'}>
                      {sub.status}
                    </Badge>
                  </div>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>
      )}
    </div>
  );
}

// ─── Config health tab ────────────────────────────────────────────────────────

function ConfigHealthTab() {
  const { t } = useTranslation();

  const query = useQuery({
    queryKey: ['platform-config-health'],
    queryFn: () => platformAdminApi.getConfigHealth(),
    staleTime: 60_000,
  });

  if (query.isLoading) return <PageLoadingState />;
  if (query.isError) return <PageErrorState message={t('platformHealth.fetchError')} onRetry={() => query.refetch()} />;

  const data = query.data!;

  const counts = {
    ok: data.checks.filter((c) => c.status === 'ok').length,
    warning: data.checks.filter((c) => c.status === 'warning').length,
    degraded: data.checks.filter((c) => c.status === 'degraded').length,
  };

  return (
    <div className="space-y-4">
      {/* KPIs de estado via StatCard */}
      <StatsGrid columns={3}>
        <StatCard
          title={t('platformHealth.config.ok')}
          value={counts.ok}
          icon={<CheckCircle2 size={18} />}
          color="text-success"
        />
        <StatCard
          title={t('platformHealth.config.warning')}
          value={counts.warning}
          icon={<AlertTriangle size={18} />}
          color="text-warning"
        />
        <StatCard
          title={t('platformHealth.config.degraded')}
          value={counts.degraded}
          icon={<XCircle size={18} />}
          color="text-critical"
        />
      </StatsGrid>

      {/* Lista de checks detalhados */}
      <Card>
        <CardBody className="p-0">
          <ul className="divide-y divide-border">
            {data.checks.map((check) => (
              <li key={check.key} className="px-4 py-3">
                <div className="flex items-start gap-3">
                  {configStatusIcon(check.status)}
                  <div className="flex-1 min-w-0">
                    <div className="flex items-center justify-between gap-2">
                      <code className="text-xs font-mono text-accent truncate">{check.key}</code>
                      <Badge variant={configStatusBadge(check.status)} className="shrink-0">
                        {check.status}
                      </Badge>
                    </div>
                    <p className="text-xs text-muted mt-0.5">{check.message}</p>
                    {check.suggestion && (
                      <p className="text-xs text-warning mt-1 flex items-start gap-1">
                        <AlertTriangle size={11} className="mt-0.5 shrink-0" />
                        {check.suggestion}
                      </p>
                    )}
                  </div>
                </div>
              </li>
            ))}
          </ul>
        </CardBody>
      </Card>
    </div>
  );
}

// ─── Migrations tab ───────────────────────────────────────────────────────────

function MigrationsTab() {
  const { t } = useTranslation();

  const query = useQuery({
    queryKey: ['platform-migrations-pending'],
    queryFn: () => platformAdminApi.getPendingMigrations(),
    staleTime: 60_000,
  });

  if (query.isLoading) return <PageLoadingState />;
  if (query.isError) return <PageErrorState message={t('platformHealth.fetchError')} onRetry={() => query.refetch()} />;

  const data = query.data!;

  return (
    <div className="space-y-4">
      {/* Banner de status da migration */}
      <div className={`rounded-md p-4 border ${data.totalPending === 0 ? 'bg-success/5 border-success/20' : data.isSafeToApply ? 'bg-warning/5 border-warning/20' : 'bg-critical/5 border-critical/20'}`}>
        <div className="flex items-center justify-between">
          <div className="flex items-center gap-3">
            {data.totalPending === 0
              ? <CheckCircle2 size={20} className="text-success" />
              : data.isSafeToApply
                ? <AlertTriangle size={20} className="text-warning" />
                : <XCircle size={20} className="text-critical" />}
            <div>
              <p className="font-semibold text-heading text-sm">
                {data.totalPending === 0
                  ? t('platformHealth.migrations.upToDate')
                  : t('platformHealth.migrations.pendingCount', { count: data.totalPending })}
              </p>
              <p className="text-xs text-muted mt-0.5">
                {data.isSafeToApply
                  ? t('platformHealth.migrations.safeToApply')
                  : t('platformHealth.migrations.requiresReview')}
              </p>
            </div>
          </div>
          <Badge variant={data.totalPending === 0 ? 'success' : data.isSafeToApply ? 'warning' : 'danger'}>
            {data.totalPending === 0 ? t('platformHealth.migrations.clean') : `${data.totalPending} ${t('platformHealth.migrations.pending')}`}
          </Badge>
        </div>
      </div>

      {/* Lista de migrations pendentes ou EmptyState */}
      {data.migrations.length > 0 ? (
        <Card>
          <CardHeader>
            <span className="text-sm font-semibold text-heading">{t('platformHealth.migrations.listTitle')}</span>
          </CardHeader>
          <CardBody className="p-0">
            <div className="divide-y divide-border">
              {data.migrations.map((m) => (
                <div key={`${m.context}-${m.migrationId}`} className="px-4 py-3">
                  <div className="flex items-start justify-between gap-2">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2">
                        <Layers size={13} className="text-accent shrink-0" />
                        <span className="text-xs font-mono text-heading truncate">{m.migrationId}</span>
                      </div>
                      <div className="flex items-center gap-2 mt-1">
                        <span className="text-xs text-muted">{t('platformHealth.migrations.context')}: {m.context}</span>
                        {m.requiresDowntime && (
                          <Badge variant="danger" className="text-xs">{t('platformHealth.migrations.downtime')}</Badge>
                        )}
                        {!m.isReversible && (
                          <Badge variant="warning" className="text-xs">{t('platformHealth.migrations.irreversible')}</Badge>
                        )}
                      </div>
                    </div>
                    <Badge variant={riskBadge(m.riskLevel)}>{m.riskLevel}</Badge>
                  </div>
                </div>
              ))}
            </div>
          </CardBody>
        </Card>
      ) : (
        <EmptyState
          icon={<CheckCircle2 size={24} />}
          title={t('platformHealth.migrations.upToDate', 'All migrations applied')}
          description={t('platformHealth.migrations.noMigrationsDesc', 'No pending database migrations detected.')}
          size="compact"
        />
      )}

      {/* Instruções de aplicação */}
      <Card>
        <CardBody>
          <div className="flex items-start gap-3">
            <div className="flex items-center justify-center w-8 h-8 bg-accent/10 rounded-lg shrink-0">
              <Settings size={15} className="text-accent" />
            </div>
            <div>
              <p className="text-sm font-medium text-heading">{t('platformHealth.migrations.applyTitle')}</p>
              <code className="block text-xs font-mono text-muted mt-1 bg-subtle px-2 py-1.5 rounded">
                dotnet ef database update --project src/modules/&lt;module&gt;/...
              </code>
              <p className="text-xs text-muted mt-1">{t('platformHealth.migrations.applyHint')}</p>
            </div>
          </div>
        </CardBody>
      </Card>
    </div>
  );
}

// ─── Main page ────────────────────────────────────────────────────────────────

export function PlatformHealthDashboardPage() {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState<Tab>('overview');

  const tabItems = [
    { id: 'overview', label: t('platformHealth.tabOverview'), icon: <Server size={14} /> },
    { id: 'config',   label: t('platformHealth.tabConfig'),   icon: <Shield size={14} /> },
    { id: 'migrations', label: t('platformHealth.tabMigrations'), icon: <Database size={14} /> },
  ];

  return (
    <PageContainer>
      <PageHeader
        title={t('platformHealth.title')}
        subtitle={t('platformHealth.subtitle')}
        actions={
          <Button variant="outline" size="sm" onClick={() => window.location.reload()}>
            <RefreshCw size={14} />
            {t('platformHealth.refresh')}
          </Button>
        }
      />

      <PageSection>
        {/* Atalhos rápidos para secções relacionadas */}
        <div className="grid grid-cols-1 sm:grid-cols-3 gap-3 mb-6">
          {[
            { label: t('platformHealth.quick.preflight'), to: '/preflight', icon: <Server size={16} />, color: 'text-accent' },
            { label: t('platformHealth.quick.setup'),     to: '/setup',     icon: <Settings size={16} />, color: 'text-warning' },
            { label: t('platformHealth.quick.ai'),        to: '/ai/models', icon: <BrainCircuit size={16} />, color: 'text-success' },
          ].map(({ label, to, icon, color }) => (
            <a
              key={to}
              href={to}
              className="flex items-center justify-between px-4 py-3 bg-card border border-edge rounded-md hover:border-accent/40 transition-colors"
            >
              <div className="flex items-center gap-2">
                <span className={color}>{icon}</span>
                <span className="text-sm font-medium text-heading">{label}</span>
              </div>
              <ArrowRight size={14} className="text-muted" />
            </a>
          ))}
        </div>

        {/* Navegação por abas usando o componente Tabs do DS */}
        <Tabs
          id="platform-health"
          items={tabItems}
          activeId={activeTab}
          onChange={(id) => setActiveTab(id as Tab)}
          className="mb-4"
        />

        {/* Conteúdo das abas via TabPanel para acessibilidade WCAG */}
        <TabPanel tabId="overview"    tabsId="platform-health" active={activeTab === 'overview'}>
          <OverviewTab />
        </TabPanel>
        <TabPanel tabId="config"      tabsId="platform-health" active={activeTab === 'config'}>
          <ConfigHealthTab />
        </TabPanel>
        <TabPanel tabId="migrations"  tabsId="platform-health" active={activeTab === 'migrations'}>
          <MigrationsTab />
        </TabPanel>
      </PageSection>
    </PageContainer>
  );
}
