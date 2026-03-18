import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { NavLink } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  AlertTriangle, AlertCircle, ShieldAlert, Eye,
  Search, CheckCircle, XCircle, Clock, Shield,
  GitBranch, Wrench, Loader2,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { OnboardingHints } from '../../../components/OnboardingHints';
import { EmptyState } from '../../../components/EmptyState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, PageSection, ContentGrid } from '../../../components/shell';
import { incidentsApi, type IncidentListItem } from '../api/incidents';

type StatusFilter = 'all' | 'Open' | 'Investigating' | 'Mitigating' | 'Monitoring' | 'Resolved' | 'Closed';

const severityBadge = (severity: string): { variant: 'success' | 'warning' | 'danger' | 'default' | 'info'; icon: React.ReactNode } => {
  switch (severity) {
    case 'Critical': return { variant: 'danger', icon: <ShieldAlert size={14} /> };
    case 'Major': return { variant: 'warning', icon: <AlertCircle size={14} /> };
    case 'Minor': return { variant: 'info', icon: <AlertTriangle size={14} /> };
    case 'Warning': return { variant: 'default', icon: <Eye size={14} /> };
    default: return { variant: 'default', icon: <AlertTriangle size={14} /> };
  }
};

const statusIcon = (status: string) => {
  switch (status) {
    case 'Open': return <AlertCircle size={14} className="text-red-400" />;
    case 'Investigating': return <Search size={14} className="text-amber-400" />;
    case 'Mitigating': return <Wrench size={14} className="text-orange-400" />;
    case 'Monitoring': return <Eye size={14} className="text-blue-400" />;
    case 'Resolved': return <CheckCircle size={14} className="text-emerald-400" />;
    case 'Closed': return <XCircle size={14} className="text-gray-400" />;
    default: return <Clock size={14} className="text-muted" />;
  }
};

function timeAgo(dateStr: string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const hours = Math.floor(diff / (1000 * 60 * 60));
  if (hours < 1) return '< 1h ago';
  if (hours < 24) return `${hours}h ago`;
  const days = Math.floor(hours / 24);
  return `${days}d ago`;
}

/**
 * Página de Incidentes — visão consolidada de incidentes com correlação contextualizada.
 * Correlaciona incidentes com serviços, mudanças, contratos, ownership e mitigação.
 * Persona-aware: Engineer vê por serviço, Tech Lead vê por equipa.
 */
export function IncidentsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [filter, setFilter] = useState<StatusFilter>('all');
  const [search, setSearch] = useState('');
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [createForm, setCreateForm] = useState({
    title: '',
    description: '',
    incidentType: 'ServiceDegradation',
    severity: 'Major',
    serviceId: '',
    serviceDisplayName: '',
    ownerTeam: '',
    impactedDomain: '',
    environment: 'Production',
  });

  const incidentsQuery = useQuery({
    queryKey: ['incidents', filter, search],
    queryFn: () => incidentsApi.listIncidents({
      status: filter !== 'all' ? filter : undefined,
      search: search || undefined,
      page: 1,
      pageSize: 50,
    }),
  });

  const summaryQuery = useQuery({
    queryKey: ['incidents-summary'],
    queryFn: () => incidentsApi.getIncidentSummary(),
  });

  const incidents: IncidentListItem[] = incidentsQuery.data?.items ?? [];

  const createIncidentMutation = useMutation({
    mutationFn: () => incidentsApi.createIncident({
      ...createForm,
      impactedDomain: createForm.impactedDomain || undefined,
    }),
    onSuccess: () => {
      setCreateError(null);
      setIsCreateOpen(false);
      setCreateForm({
        title: '',
        description: '',
        incidentType: 'ServiceDegradation',
        severity: 'Major',
        serviceId: '',
        serviceDisplayName: '',
        ownerTeam: '',
        impactedDomain: '',
        environment: 'Production',
      });
      queryClient.invalidateQueries({ queryKey: ['incidents'] });
      queryClient.invalidateQueries({ queryKey: ['incidents-summary'] });
    },
    onError: (error: unknown) => {
      const message = error instanceof Error ? error.message : t('common.errorLoading');
      setCreateError(message);
    },
  });

  const stats = summaryQuery.data ?? {
    totalOpen: 0,
    criticalIncidents: 0,
    withCorrelatedChanges: 0,
    withMitigationAvailable: 0,
    servicesImpacted: 0,
  };

  return (
    <PageContainer>
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('incidents.title')}</h1>
        <p className="text-muted mt-1">{t('incidents.subtitle')}</p>
      </div>

      {/* Onboarding hints — orientação contextual para novos utilizadores */}
      <OnboardingHints module="operations" />

      {/* Stats */}
      <PageSection className="!mb-6">
        <ContentGrid className="!grid-cols-2 lg:!grid-cols-5">
          <StatCard title={t('incidents.totalOpen')} value={stats.totalOpen} icon={<AlertTriangle size={20} />} color="text-red-500" />
          <StatCard title={t('incidents.critical')} value={stats.criticalIncidents} icon={<ShieldAlert size={20} />} color="text-critical" />
          <StatCard title={t('incidents.withCorrelation')} value={stats.withCorrelatedChanges} icon={<GitBranch size={20} />} color="text-amber-500" />
          <StatCard title={t('incidents.withMitigation')} value={stats.withMitigationAvailable} icon={<Wrench size={20} />} color="text-blue-500" />
          <StatCard title={t('incidents.servicesImpacted')} value={stats.servicesImpacted} icon={<Shield size={20} />} color="text-accent" />
        </ContentGrid>
      </PageSection>

      {/* Filters + Incident list */}
      <PageSection>
        <div className="mb-4 flex items-center justify-end">
          <button
            type="button"
            onClick={() => setIsCreateOpen(prev => !prev)}
            className="px-3 py-2 text-sm rounded-md border border-accent/30 text-accent hover:bg-accent/10 transition-colors"
          >
            {t('incidents.create.button', 'Create Incident')}
          </button>
        </div>

        {isCreateOpen && (
          <Card className="mb-4">
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading">{t('incidents.create.title', 'Create Incident')}</h2>
            </CardHeader>
            <CardBody>
              <form
                className="grid grid-cols-1 md:grid-cols-2 gap-3"
                onSubmit={(e) => {
                  e.preventDefault();
                  setCreateError(null);
                  createIncidentMutation.mutate();
                }}
              >
                <input
                  value={createForm.title}
                  onChange={(e) => setCreateForm(prev => ({ ...prev, title: e.target.value }))}
                  placeholder={t('incidents.create.titlePlaceholder', 'Incident title')}
                  className="px-3 py-2 text-sm rounded-md bg-surface border border-edge text-body"
                  required
                />
                <input
                  value={createForm.serviceId}
                  onChange={(e) => setCreateForm(prev => ({ ...prev, serviceId: e.target.value }))}
                  placeholder={t('incidents.create.serviceIdPlaceholder', 'Service ID')}
                  className="px-3 py-2 text-sm rounded-md bg-surface border border-edge text-body"
                  required
                />
                <input
                  value={createForm.serviceDisplayName}
                  onChange={(e) => setCreateForm(prev => ({ ...prev, serviceDisplayName: e.target.value }))}
                  placeholder={t('incidents.create.serviceNamePlaceholder', 'Service display name')}
                  className="px-3 py-2 text-sm rounded-md bg-surface border border-edge text-body"
                  required
                />
                <input
                  value={createForm.ownerTeam}
                  onChange={(e) => setCreateForm(prev => ({ ...prev, ownerTeam: e.target.value }))}
                  placeholder={t('incidents.create.ownerTeamPlaceholder', 'Owner team')}
                  className="px-3 py-2 text-sm rounded-md bg-surface border border-edge text-body"
                  required
                />
                <select
                  value={createForm.incidentType}
                  onChange={(e) => setCreateForm(prev => ({ ...prev, incidentType: e.target.value }))}
                  className="px-3 py-2 text-sm rounded-md bg-surface border border-edge text-body"
                >
                  {['ServiceDegradation', 'AvailabilityIssue', 'DependencyFailure', 'ContractImpact', 'MessagingIssue', 'BackgroundProcessingIssue', 'OperationalRegression'].map(type => (
                    <option key={type} value={type}>{type}</option>
                  ))}
                </select>
                <select
                  value={createForm.severity}
                  onChange={(e) => setCreateForm(prev => ({ ...prev, severity: e.target.value }))}
                  className="px-3 py-2 text-sm rounded-md bg-surface border border-edge text-body"
                >
                  {['Warning', 'Minor', 'Major', 'Critical'].map(sev => (
                    <option key={sev} value={sev}>{sev}</option>
                  ))}
                </select>
                <input
                  value={createForm.environment}
                  onChange={(e) => setCreateForm(prev => ({ ...prev, environment: e.target.value }))}
                  placeholder={t('incidents.create.environmentPlaceholder', 'Environment')}
                  className="px-3 py-2 text-sm rounded-md bg-surface border border-edge text-body"
                  required
                />
                <input
                  value={createForm.impactedDomain}
                  onChange={(e) => setCreateForm(prev => ({ ...prev, impactedDomain: e.target.value }))}
                  placeholder={t('incidents.create.domainPlaceholder', 'Impacted domain (optional)')}
                  className="px-3 py-2 text-sm rounded-md bg-surface border border-edge text-body"
                />
                <textarea
                  value={createForm.description}
                  onChange={(e) => setCreateForm(prev => ({ ...prev, description: e.target.value }))}
                  placeholder={t('incidents.create.descriptionPlaceholder', 'Describe what happened')}
                  className="md:col-span-2 px-3 py-2 text-sm rounded-md bg-surface border border-edge text-body min-h-[88px]"
                  required
                />

                {createError && (
                  <p className="md:col-span-2 text-sm text-critical">{createError}</p>
                )}

                <div className="md:col-span-2 flex justify-end gap-2">
                  <button
                    type="button"
                    onClick={() => setIsCreateOpen(false)}
                    className="px-3 py-2 text-sm rounded-md border border-edge text-muted hover:text-body"
                  >
                    {t('common.cancel', 'Cancel')}
                  </button>
                  <button
                    type="submit"
                    disabled={createIncidentMutation.isPending}
                    className="px-3 py-2 text-sm rounded-md bg-accent text-accent-contrast disabled:opacity-60"
                  >
                    {createIncidentMutation.isPending ? t('common.loading', 'Loading...') : t('incidents.create.submit', 'Create')}
                  </button>
                </div>
              </form>
            </CardBody>
          </Card>
        )}

        <div className="flex flex-wrap items-center gap-3 mb-4">
          <div className="relative flex-1 max-w-xs">
            <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
            <input
              type="text"
              value={search}
              onChange={e => setSearch(e.target.value)}
              placeholder={t('incidents.searchPlaceholder')}
              className="w-full pl-9 pr-3 py-2 text-sm rounded-md bg-surface border border-edge text-body placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
            />
          </div>
          {(['all', 'Open', 'Investigating', 'Mitigating', 'Monitoring', 'Resolved', 'Closed'] as StatusFilter[]).map(f => (
            <button
              key={f}
              onClick={() => setFilter(f)}
              className={`px-3 py-1.5 text-xs rounded-md border transition-colors ${
                filter === f
                  ? 'bg-accent/10 text-accent border-accent/30'
                  : 'bg-surface text-muted border-edge hover:text-body'
              }`}
            >
              {t(`incidents.filter.${f}`)}
            </button>
          ))}
        </div>

        <Card>
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
              <AlertTriangle size={16} className="text-accent" />
              {t('incidents.list.title')}
            </h2>
          </CardHeader>
          <CardBody className="p-0">
            {incidentsQuery.isLoading ? (
              <PageLoadingState size="sm" />
            ) : incidentsQuery.isError ? (
              <PageErrorState className="py-8" />
            ) : (
              <div className="divide-y divide-edge">
                {incidents.length === 0 ? (
                  <div className="p-8 text-center text-muted text-sm">{t('common.noResults')}</div>
                ) : (
                  incidents.map((inc: IncidentListItem) => {
                    const badge = severityBadge(inc.severity);
                    return (
                      <NavLink
                        key={inc.incidentId}
                        to={`/operations/incidents/${inc.incidentId}`}
                        className="flex items-center gap-4 px-4 py-3 hover:bg-hover transition-colors"
                      >
                        <div className="flex items-center gap-2 min-w-0 flex-1">
                          <Badge variant={badge.variant} className="flex items-center gap-1 shrink-0">
                            {badge.icon}
                            {t(`incidents.severity.${inc.severity}`)}
                          </Badge>
                          <div className="min-w-0">
                            <div className="flex items-center gap-2">
                              <span className="text-xs text-muted font-mono">{inc.reference}</span>
                              <span className="flex items-center gap-1 text-xs text-muted">
                                {statusIcon(inc.status)}
                                {t(`incidents.status.${inc.status}`)}
                              </span>
                            </div>
                            <p className="text-sm font-medium text-heading truncate">{inc.title}</p>
                          </div>
                        </div>
                        <div className="hidden md:flex items-center gap-3 text-xs text-muted shrink-0">
                          <span className="w-28 truncate">{inc.serviceDisplayName}</span>
                          <span className="w-24 truncate">{inc.ownerTeam}</span>
                          <span className="w-16 text-right">{timeAgo(inc.createdAt)}</span>
                          {inc.hasCorrelatedChanges && (
                            <Badge variant="warning" className="text-[10px] flex items-center gap-1">
                              <GitBranch size={10} />
                              {t('incidents.list.correlationIndicator')}
                            </Badge>
                          )}
                          {inc.mitigationStatus !== 'NotStarted' && inc.mitigationStatus !== 'Verified' && (
                            <Badge variant="info" className="text-[10px] flex items-center gap-1">
                              <Wrench size={10} />
                              {t('incidents.list.mitigationIndicator')}
                            </Badge>
                          )}
                        </div>
                      </NavLink>
                    );
                  })
                )}
              </div>
            )}
          </CardBody>
        </Card>
      </PageSection>
    </PageContainer>
  );
}
