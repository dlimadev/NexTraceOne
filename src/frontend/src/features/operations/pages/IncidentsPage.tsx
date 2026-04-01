import * as React from 'react';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { NavLink } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  AlertTriangle, AlertCircle, ShieldAlert, Eye,
  Search, CheckCircle, XCircle, Clock, Shield,
  GitBranch, Wrench,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { OnboardingHints } from '../../../components/OnboardingHints';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, PageSection, ContentGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { incidentsApi, type IncidentListItem } from '../api/incidents';
import { usePermissions } from '../../../hooks/usePermissions';
import { resolveApiError } from '../../../utils/apiErrors';

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
    case 'Open': return <AlertCircle size={14} className="text-critical" />;
    case 'Investigating': return <Search size={14} className="text-warning" />;
    case 'Mitigating': return <Wrench size={14} className="text-warning" />;
    case 'Monitoring': return <Eye size={14} className="text-info" />;
    case 'Resolved': return <CheckCircle size={14} className="text-success" />;
    case 'Closed': return <XCircle size={14} className="text-muted" />;
    default: return <Clock size={14} className="text-muted" />;
  }
};

function timeAgo(dateStr: string, t: (key: string, options?: Record<string, unknown>) => string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const hours = Math.floor(diff / (1000 * 60 * 60));
  if (hours < 1) return t('common.timeAgo.lessThanHour');
  if (hours < 24) return t('common.timeAgo.hours', { count: hours });
  const days = Math.floor(hours / 24);
  return t('common.timeAgo.days', { count: days });
}

const defaultCreateForm = {
  title: '',
  description: '',
  incidentType: 'ServiceDegradation',
  severity: 'Major',
  serviceId: '',
  serviceDisplayName: '',
  ownerTeam: '',
  impactedDomain: '',
  environment: 'Production',
};

/**
 * Página de Incidentes — visão consolidada de incidentes com correlação contextualizada.
 * Correlaciona incidentes com serviços, mudanças, contratos, ownership e mitigação.
 * Persona-aware: Engineer vê por serviço, Tech Lead vê por equipa.
 */
export function IncidentsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { can } = usePermissions();
  const canCreateIncident = can('operations:incidents:write');
  const [filter, setFilter] = useState<StatusFilter>('all');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [createSuccess, setCreateSuccess] = useState<{ incidentId: string; reference: string } | null>(null);
  const [createForm, setCreateForm] = useState(defaultCreateForm);

  const incidentsQuery = useQuery({
    queryKey: ['incidents', filter, search, page, pageSize],
    queryFn: () => incidentsApi.listIncidents({
      status: filter !== 'all' ? filter : undefined,
      search: search || undefined,
      page,
      pageSize,
    }),
  });

  const summaryQuery = useQuery({
    queryKey: ['incidents-summary'],
    queryFn: () => incidentsApi.getIncidentSummary(),
  });

  const incidents: IncidentListItem[] = incidentsQuery.data?.items ?? [];
  const totalCount = incidentsQuery.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const canGoToPreviousPage = page > 1;
  const canGoToNextPage = page < totalPages;
  const currentRangeStart = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const currentRangeEnd = totalCount === 0 ? 0 : Math.min(page * pageSize, totalCount);
  const isCreateFormValid = useMemo(() => (
    createForm.title.trim().length > 0
    && createForm.description.trim().length > 0
    && createForm.serviceId.trim().length > 0
    && createForm.serviceDisplayName.trim().length > 0
    && createForm.ownerTeam.trim().length > 0
    && createForm.environment.trim().length > 0
  ), [createForm]);

  const createIncidentMutation = useMutation({
    mutationFn: () => incidentsApi.createIncident({
      ...createForm,
      impactedDomain: createForm.impactedDomain || undefined,
    }),
    onSuccess: async (response) => {
      setCreateError(null);
      setCreateSuccess({
        incidentId: response.incidentId,
        reference: response.reference,
      });
      setIsCreateOpen(false);
      setFilter('all');
      setSearch('');
      setPage(1);
      setCreateForm(defaultCreateForm);
      await queryClient.invalidateQueries({ queryKey: ['incidents'] });
      await queryClient.invalidateQueries({ queryKey: ['incidents-summary'] });
      await queryClient.refetchQueries({ queryKey: ['incidents'] });
      await queryClient.refetchQueries({ queryKey: ['incidents-summary'] });
    },
    onError: (error: unknown) => {
      setCreateSuccess(null);
      setCreateError(resolveApiError(error));
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
      <PageHeader
        title={t('incidents.title')}
        subtitle={t('incidents.subtitle')}
      />

      {/* Onboarding hints — orientação contextual para novos utilizadores */}
      <OnboardingHints module="operations" />

      {/* Stats */}
      <PageSection className="!mb-6">
        <ContentGrid className="!grid-cols-2 lg:!grid-cols-5">
          <StatCard title={t('incidents.totalOpen')} value={stats.totalOpen} icon={<AlertTriangle size={20} />} color="text-critical" />
          <StatCard title={t('incidents.critical')} value={stats.criticalIncidents} icon={<ShieldAlert size={20} />} color="text-critical" />
          <StatCard title={t('incidents.withCorrelation')} value={stats.withCorrelatedChanges} icon={<GitBranch size={20} />} color="text-warning" />
          <StatCard title={t('incidents.withMitigation')} value={stats.withMitigationAvailable} icon={<Wrench size={20} />} color="text-info" />
          <StatCard title={t('incidents.servicesImpacted')} value={stats.servicesImpacted} icon={<Shield size={20} />} color="text-accent" />
        </ContentGrid>
      </PageSection>

      {/* Filters + Incident list */}
      <PageSection>
        <div className="mb-4 flex flex-wrap items-center justify-between gap-3">
          <div className="text-xs text-muted">
            {t('incidents.list.countSummary', 'Showing {{start}}-{{end}} of {{total}} incidents', {
              start: currentRangeStart,
              end: currentRangeEnd,
              total: totalCount,
            })}
          </div>
          {canCreateIncident ? (
            <button
              type="button"
              onClick={() => {
                setCreateSuccess(null);
                setCreateError(null);
                setIsCreateOpen(prev => !prev);
              }}
              className="px-3 py-2 text-sm rounded-md border border-accent/30 text-accent hover:bg-accent/10 transition-colors"
            >
              {t('incidents.create.button', 'Create Incident')}
            </button>
          ) : (
            <p className="text-xs text-muted">
              {t('incidents.create.readOnlyHint', 'Your current role can review incidents but cannot create new ones.')}
            </p>
          )}
        </div>

        {createSuccess && (
          <Card className="mb-4 border border-success/25">
            <CardBody className="flex flex-wrap items-center justify-between gap-3">
              <p className="text-sm text-body">
                {t('incidents.create.successMessage', 'Incident {{reference}} was created and persisted successfully.', {
                  reference: createSuccess.reference,
                })}
              </p>
              <NavLink
                to={`/operations/incidents/${createSuccess.incidentId}`}
                className="text-sm text-accent hover:underline"
              >
                {t('incidents.create.openDetail', 'Open incident detail')}
              </NavLink>
            </CardBody>
          </Card>
        )}

        {isCreateOpen && canCreateIncident && (
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
                  setCreateSuccess(null);
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
                    <option key={type} value={type}>{t(`incidents.type.${type}`)}</option>
                  ))}
                </select>
                <select
                  value={createForm.severity}
                  onChange={(e) => setCreateForm(prev => ({ ...prev, severity: e.target.value }))}
                  className="px-3 py-2 text-sm rounded-md bg-surface border border-edge text-body"
                >
                  {['Warning', 'Minor', 'Major', 'Critical'].map(sev => (
                    <option key={sev} value={sev}>{t(`incidents.severity.${sev}`)}</option>
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
                    onClick={() => {
                      setCreateError(null);
                      setIsCreateOpen(false);
                    }}
                    className="px-3 py-2 text-sm rounded-md border border-edge text-muted hover:text-body"
                  >
                    {t('common.cancel', 'Cancel')}
                  </button>
                  <button
                    type="submit"
                    disabled={!isCreateFormValid || createIncidentMutation.isPending}
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
              onChange={e => {
                setSearch(e.target.value);
                setPage(1);
              }}
              placeholder={t('incidents.searchPlaceholder', 'Search incidents...')}
              className="w-full pl-9 pr-3 py-2 text-sm rounded-md bg-surface border border-edge text-body placeholder:text-muted focus:outline-none focus:ring-1 focus:ring-accent"
            />
          </div>
          {(['all', 'Open', 'Investigating', 'Mitigating', 'Monitoring', 'Resolved', 'Closed'] as StatusFilter[]).map(f => (
            <button
              key={f}
              onClick={() => {
                setFilter(f);
                setPage(1);
              }}
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
            <div className="flex flex-wrap items-center justify-between gap-3">
              <h2 className="text-sm font-semibold text-heading flex items-center gap-2">
                <AlertTriangle size={16} className="text-accent" />
                {t('incidents.list.title')}
              </h2>
              <span className="text-xs text-muted">
                {t('incidents.list.pageSummary', 'Page {{page}} of {{totalPages}}', { page, totalPages })}
              </span>
            </div>
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
                        className={`flex items-center gap-4 px-4 py-3 hover:bg-hover transition-colors ${
                          createSuccess?.incidentId === inc.incidentId ? 'bg-accent/5' : ''
                        }`}
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
                          <span className="w-16 text-right">{timeAgo(inc.createdAt, t)}</span>
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

        <div className="mt-4 flex items-center justify-between gap-3">
          <button
            type="button"
            onClick={() => setPage(prev => Math.max(1, prev - 1))}
            disabled={!canGoToPreviousPage || incidentsQuery.isFetching}
            className="px-3 py-2 text-sm rounded-md border border-edge text-muted hover:text-body disabled:opacity-50"
          >
            {t('common.back', 'Back')}
          </button>
          <span className="text-xs text-muted">
            {t('incidents.list.countSummary', 'Showing {{start}}-{{end}} of {{total}} incidents', {
              start: currentRangeStart,
              end: currentRangeEnd,
              total: totalCount,
            })}
          </span>
          <button
            type="button"
            onClick={() => setPage(prev => Math.min(totalPages, prev + 1))}
            disabled={!canGoToNextPage || incidentsQuery.isFetching}
            className="px-3 py-2 text-sm rounded-md border border-edge text-muted hover:text-body disabled:opacity-50"
          >
            {t('common.next', 'Next')}
          </button>
        </div>
      </PageSection>
    </PageContainer>
  );
}
