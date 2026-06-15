import * as React from 'react';
import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { NavLink } from 'react-router-dom';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import {
  AlertTriangle, AlertCircle, ShieldAlert, Eye,
  Search, CheckCircle2, XCircle, Clock, Shield,
  GitBranch, Wrench, Plus, CalendarClock,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { PageContainer, PageSection, ContentGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { SearchInput } from '../../../components/SearchInput';
import { TextField } from '../../../components/TextField';
import { TextArea } from '../../../components/TextArea';
import { Select } from '../../../components/Select';
import { Drawer } from '../../../components/Drawer';
import { incidentsApi, type IncidentListItem } from '../api/incidents';
import { usePermissions } from '../../../hooks/usePermissions';
import { resolveApiError } from '../../../utils/apiErrors';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import { queryKeys } from '../../../shared/api/queryKeys';

type StatusFilter = 'all' | 'Open' | 'Investigating' | 'Mitigating' | 'Monitoring' | 'Resolved' | 'Closed';

/** Mapeia severidade para variante de Badge + ícone. */
const severityBadge = (severity: string): { variant: 'success' | 'warning' | 'danger' | 'default' | 'info'; icon: React.ReactNode } => {
  switch (severity) {
    case 'Critical': return { variant: 'danger', icon: <ShieldAlert size={14} /> };
    case 'Major':    return { variant: 'warning', icon: <AlertCircle size={14} /> };
    case 'Minor':    return { variant: 'info', icon: <AlertTriangle size={14} /> };
    case 'Warning':  return { variant: 'default', icon: <Eye size={14} /> };
    default:         return { variant: 'default', icon: <AlertTriangle size={14} /> };
  }
};

/** Ícone de status por estado. */
const statusIcon = (status: string) => {
  switch (status) {
    case 'Open':         return <AlertCircle size={14} className="text-critical" />;
    case 'Investigating':return <Search size={14} className="text-warning" />;
    case 'Mitigating':  return <Wrench size={14} className="text-warning" />;
    case 'Monitoring':  return <Eye size={14} className="text-info" />;
    case 'Resolved':    return <CheckCircle2 size={14} className="text-success" />;
    case 'Closed':      return <XCircle size={14} className="text-muted" />;
    default:            return <Clock size={14} className="text-muted" />;
  }
};

/** Formata diferença temporal em string localizada. */
function timeAgo(dateStr: string, t: (key: string, options?: Record<string, unknown>) => string): string {
  const diff = Date.now() - new Date(dateStr).getTime();
  const hours = Math.floor(diff / (1000 * 60 * 60));
  if (hours < 1) return t('common.timeAgo.lessThanHour');
  if (hours < 24) return t('common.timeAgo.hours', { count: hours });
  const days = Math.floor(hours / 24);
  return t('common.timeAgo.days', { count: days });
}

/** Estado inicial do formulário de criação de incidente. */
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

/** Opções de tipo de incidente para o Select. */
const INCIDENT_TYPE_OPTIONS = [
  'ServiceDegradation',
  'AvailabilityIssue',
  'DependencyFailure',
  'ContractImpact',
  'MessagingIssue',
  'BackgroundProcessingIssue',
  'OperationalRegression',
];

/** Opções de severidade para o Select. */
const SEVERITY_OPTIONS = ['Warning', 'Minor', 'Major', 'Critical'];

/** Filtros de status disponíveis na barra de filtros. */
const STATUS_FILTERS: StatusFilter[] = ['all', 'Open', 'Investigating', 'Mitigating', 'Monitoring', 'Resolved', 'Closed'];

/**
 * Página de Incidentes — visão consolidada de incidentes com correlação contextualizada.
 * Correlaciona incidentes com serviços, mudanças, contratos, ownership e mitigação.
 * Persona-aware: Engineer vê por serviço, Tech Lead vê por equipa.
 *
 * Redesign Betterstack: ação primária no header, form em Drawer lateral,
 * controles de filtro/busca e paginação com componentes do DS.
 */
export function IncidentsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { can } = usePermissions();
  const { activeEnvironmentId } = useEnvironment();
  const canCreateIncident = can('operations:incidents:write');

  // Estado de filtros e paginação
  const [filter, setFilter] = useState<StatusFilter>('all');
  const [search, setSearch] = useState('');
  const [page, setPage] = useState(1);
  const [pageSize] = useState(20);

  // Estado do Drawer de criação
  const [isCreateOpen, setIsCreateOpen] = useState(false);
  const [createError, setCreateError] = useState<string | null>(null);
  const [createSuccess, setCreateSuccess] = useState<{ incidentId: string; reference: string } | null>(null);
  const [createForm, setCreateForm] = useState(defaultCreateForm);

  // Queries
  const incidentsQuery = useQuery({
    queryKey: queryKeys.incidents.list({ filter, search, page, pageSize }, activeEnvironmentId),
    queryFn: () => incidentsApi.listIncidents({
      status: filter !== 'all' ? filter : undefined,
      search: search || undefined,
      page,
      pageSize,
    }),
  });

  const summaryQuery = useQuery({
    queryKey: queryKeys.incidents.summary(activeEnvironmentId),
    queryFn: () => incidentsApi.getIncidentSummary(),
  });

  // Dados derivados
  const incidents: IncidentListItem[] = incidentsQuery.data?.items ?? [];
  const totalCount = incidentsQuery.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / pageSize));
  const canGoToPreviousPage = page > 1;
  const canGoToNextPage = page < totalPages;
  const currentRangeStart = totalCount === 0 ? 0 : (page - 1) * pageSize + 1;
  const currentRangeEnd   = totalCount === 0 ? 0 : Math.min(page * pageSize, totalCount);

  /** Valida se o formulário tem os campos obrigatórios preenchidos. */
  const isCreateFormValid = useMemo(() => (
    createForm.title.trim().length > 0
    && createForm.description.trim().length > 0
    && createForm.serviceId.trim().length > 0
    && createForm.serviceDisplayName.trim().length > 0
    && createForm.ownerTeam.trim().length > 0
    && createForm.environment.trim().length > 0
  ), [createForm]);

  // Mutation: resolver incidente
  const resolveIncidentMutation = useMutation({
    mutationFn: (incidentId: string) => incidentsApi.resolveIncident(incidentId),
    onSuccess: async () => {
      await queryClient.invalidateQueries({ queryKey: ['incidents'] });
      await queryClient.invalidateQueries({ queryKey: ['incidents-summary'] });
    },
    onError: (error: unknown) => {
      setCreateError(resolveApiError(error));
    },
  });

  // Mutation: criar incidente
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

  /** Abre o Drawer limpando estado anterior de erro/sucesso. */
  const handleOpenCreate = () => {
    setCreateSuccess(null);
    setCreateError(null);
    setIsCreateOpen(true);
  };

  /** Fecha o Drawer e limpa erro inline. */
  const handleCloseCreate = () => {
    setCreateError(null);
    setIsCreateOpen(false);
  };

  /** Submete o formulário de criação. */
  const handleCreateSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setCreateError(null);
    setCreateSuccess(null);
    createIncidentMutation.mutate();
  };

  const stats = summaryQuery.data ?? {
    totalOpen: 0,
    criticalIncidents: 0,
    withCorrelatedChanges: 0,
    withMitigationAvailable: 0,
    servicesImpacted: 0,
  };

  // Opções localizadas para selects do formulário
  const incidentTypeOptions = INCIDENT_TYPE_OPTIONS.map(type => ({
    value: type,
    label: t(`incidents.type.${type}`),
  }));

  const severityOptions = SEVERITY_OPTIONS.map(sev => ({
    value: sev,
    label: t(`incidents.severity.${sev}`),
  }));

  return (
    <PageContainer>
      {/* Cabeçalho com ações primárias — CTA de criação e link de timeline */}
      <PageHeader
        title={t('incidents.title')}
        subtitle={t('incidents.subtitle')}
        actions={
          <>
            {/* Link para vista de timeline como ação secundária */}
            <NavLink to="/operations/incidents/timeline">
              <Button
                variant="outline"
                size="sm"
                icon={<CalendarClock size={15} />}
              >
                {t('incidents.timelineView.open')}
              </Button>
            </NavLink>

            {/* CTA principal — visível apenas para quem tem permissão de escrita */}
            {canCreateIncident && (
              <Button
                variant="primary"
                size="sm"
                icon={<Plus size={15} />}
                onClick={handleOpenCreate}
              >
                {t('incidents.create.button', 'Create Incident')}
              </Button>
            )}
          </>
        }
      />

      {/* Stats de resumo */}
      <PageSection className="!mb-6">
        <ContentGrid className="!grid-cols-2 lg:!grid-cols-5">
          <StatCard title={t('incidents.totalOpen')}        value={stats.totalOpen}                icon={<AlertTriangle size={20} />} color="text-critical" />
          <StatCard title={t('incidents.critical')}         value={stats.criticalIncidents}         icon={<ShieldAlert size={20} />}   color="text-critical" />
          <StatCard title={t('incidents.withCorrelation')}  value={stats.withCorrelatedChanges}    icon={<GitBranch size={20} />}     color="text-warning"  />
          <StatCard title={t('incidents.withMitigation')}   value={stats.withMitigationAvailable}  icon={<Wrench size={20} />}        color="text-info"     />
          <StatCard title={t('incidents.servicesImpacted')} value={stats.servicesImpacted}          icon={<Shield size={20} />}        color="text-accent"   />
        </ContentGrid>
      </PageSection>

      {/* Drawer lateral de criação de incidente */}
      <Drawer
        open={isCreateOpen}
        onClose={handleCloseCreate}
        title={t('incidents.create.title', 'Create Incident')}
        description={t('incidents.subtitle')}
        size="md"
        footer={
          <>
            <Button
              variant="outline"
              size="sm"
              onClick={handleCloseCreate}
            >
              {t('common.cancel', 'Cancel')}
            </Button>
            <Button
              variant="primary"
              size="sm"
              form="create-incident-form"
              type="submit"
              disabled={!isCreateFormValid}
              loading={createIncidentMutation.isPending}
            >
              {t('incidents.create.submit', 'Create')}
            </Button>
          </>
        }
      >
        <form
          id="create-incident-form"
          className="flex flex-col gap-4"
          onSubmit={handleCreateSubmit}
        >
          {/* Título do incidente */}
          <TextField
            label={t('incidents.create.titlePlaceholder', 'Incident title')}
            value={createForm.title}
            onChange={e => setCreateForm(prev => ({ ...prev, title: e.target.value }))}
            placeholder={t('incidents.create.titlePlaceholder', 'Incident title')}
            required
          />

          {/* Serviço afetado */}
          <div className="grid grid-cols-2 gap-3">
            <TextField
              label={t('incidents.create.serviceIdPlaceholder', 'Service ID')}
              value={createForm.serviceId}
              onChange={e => setCreateForm(prev => ({ ...prev, serviceId: e.target.value }))}
              placeholder={t('incidents.create.serviceIdPlaceholder', 'Service ID')}
              required
            />
            <TextField
              label={t('incidents.create.serviceNamePlaceholder', 'Service display name')}
              value={createForm.serviceDisplayName}
              onChange={e => setCreateForm(prev => ({ ...prev, serviceDisplayName: e.target.value }))}
              placeholder={t('incidents.create.serviceNamePlaceholder', 'Service display name')}
              required
            />
          </div>

          {/* Equipa e ambiente */}
          <div className="grid grid-cols-2 gap-3">
            <TextField
              label={t('incidents.create.ownerTeamPlaceholder', 'Owner team')}
              value={createForm.ownerTeam}
              onChange={e => setCreateForm(prev => ({ ...prev, ownerTeam: e.target.value }))}
              placeholder={t('incidents.create.ownerTeamPlaceholder', 'Owner team')}
              required
            />
            <TextField
              label={t('incidents.create.environmentPlaceholder', 'Environment')}
              value={createForm.environment}
              onChange={e => setCreateForm(prev => ({ ...prev, environment: e.target.value }))}
              placeholder={t('incidents.create.environmentPlaceholder', 'Environment')}
              required
            />
          </div>

          {/* Tipo e severidade */}
          <div className="grid grid-cols-2 gap-3">
            <Select
              label={t('incidents.create.incidentType', 'Type')}
              value={createForm.incidentType}
              onChange={e => setCreateForm(prev => ({ ...prev, incidentType: e.target.value }))}
              options={incidentTypeOptions}
              size="md"
            />
            <Select
              label={t('incidents.create.severity', 'Severity')}
              value={createForm.severity}
              onChange={e => setCreateForm(prev => ({ ...prev, severity: e.target.value }))}
              options={severityOptions}
              size="md"
            />
          </div>

          {/* Domínio impactado (opcional) */}
          <TextField
            label={t('incidents.create.domainPlaceholder', 'Impacted domain (optional)')}
            value={createForm.impactedDomain}
            onChange={e => setCreateForm(prev => ({ ...prev, impactedDomain: e.target.value }))}
            placeholder={t('incidents.create.domainPlaceholder', 'Impacted domain (optional)')}
          />

          {/* Descrição */}
          <TextArea
            label={t('incidents.create.descriptionPlaceholder', 'Describe what happened')}
            value={createForm.description}
            onChange={e => setCreateForm(prev => ({ ...prev, description: e.target.value }))}
            placeholder={t('incidents.create.descriptionPlaceholder', 'Describe what happened')}
            required
          />

          {/* Erro de criação inline no formulário */}
          {createError && (
            <p className="text-sm text-critical" role="alert">{createError}</p>
          )}
        </form>
      </Drawer>

      {/* Lista de incidentes + filtros */}
      <PageSection>
        {/* Banner de sucesso após criação */}
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

        {/* Barra de filtros: busca + filtros de status */}
        <div className="flex flex-wrap items-center gap-3 mb-4">
          {/* Campo de busca com componente DS */}
          <SearchInput
            className="flex-1 max-w-xs"
            size="sm"
            value={search}
            onChange={e => {
              setSearch(e.target.value);
              setPage(1);
            }}
            placeholder={t('incidents.searchPlaceholder', 'Search incidents...')}
            aria-label={t('incidents.searchPlaceholder', 'Search incidents...')}
          />

          {/* Filtros de status — variant toggleado por estado ativo */}
          {STATUS_FILTERS.map(f => (
            <Button
              key={f}
              variant={filter === f ? 'subtle' : 'ghost'}
              size="xs"
              onClick={() => {
                setFilter(f);
                setPage(1);
              }}
              className={filter === f ? 'text-accent' : undefined}
            >
              {t(`incidents.filter.${f}`)}
            </Button>
          ))}
        </div>

        {/* Card principal de listagem */}
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
                  <EmptyState
                    title={t('incidents.empty', 'No incidents found')}
                    description={t('incidents.emptyDescription', 'No incidents match your current filters.')}
                    icon={<AlertTriangle size={40} />}
                  />
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
                          {canCreateIncident && inc.status !== 'Resolved' && inc.status !== 'Closed' && (
                            <button
                              type="button"
                              onClick={(e) => {
                                e.preventDefault();
                                e.stopPropagation();
                                resolveIncidentMutation.mutate(inc.incidentId);
                              }}
                              disabled={resolveIncidentMutation.isPending}
                              className="flex items-center gap-1 px-2 py-1 text-[11px] rounded-md border border-edge text-muted hover:text-success hover:border-success transition-colors disabled:opacity-50"
                              title={t('incidents.list.resolveAction', 'Resolve incident')}
                            >
                              <CheckCircle2 size={12} />
                              {t('incidents.list.resolveAction', 'Resolve')}
                            </button>
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

        {/* Paginação com componentes Button do DS */}
        <div className="mt-4 flex items-center justify-between gap-3">
          <Button
            variant="outline"
            size="sm"
            onClick={() => setPage(prev => Math.max(1, prev - 1))}
            disabled={!canGoToPreviousPage || incidentsQuery.isFetching}
          >
            {t('common.back', 'Back')}
          </Button>

          <span className="text-xs text-muted">
            {t('incidents.list.countSummary', 'Showing {{start}}-{{end}} of {{total}} incidents', {
              start: currentRangeStart,
              end: currentRangeEnd,
              total: totalCount,
            })}
          </span>

          <Button
            variant="outline"
            size="sm"
            onClick={() => setPage(prev => Math.min(totalPages, prev + 1))}
            disabled={!canGoToNextPage || incidentsQuery.isFetching}
          >
            {t('common.next', 'Next')}
          </Button>
        </div>
      </PageSection>
    </PageContainer>
  );
}
