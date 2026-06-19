import { useState, useEffect, useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  Activity,
  CheckCircle2,
  AlertTriangle,
  XCircle,
  AlertOctagon,
  ChevronLeft,
  ChevronRight,
  Plus,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { PageContainer, PageSection, ContentGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { SearchInput } from '../../../components/SearchInput';
import { Select } from '../../../components/Select';
import { changeConfidenceApi } from '../api/changeConfidence';
import type { ChangesFilterParams } from '../api/changeConfidence';
import type { ChangeDto } from '../../../types';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

/** Delay em ms para debounce da pesquisa textual. */
const SEARCH_DEBOUNCE_MS = 350;

/** Tamanho padrão de página. */
const PAGE_SIZE = 20;

/** Fallback de tipos de mudança quando o endpoint de opções ainda não estiver disponível. */
const FALLBACK_CHANGE_TYPES = [
  'Deployment',
  'ConfigurationChange',
  'ContractChange',
  'SchemaChange',
  'DependencyChange',
  'PolicyChange',
  'OperationalChange',
] as const;

/** Estados de confiança possíveis. */
const CONFIDENCE_STATUSES = [
  'NotAssessed',
  'Validated',
  'NeedsAttention',
  'SuspectedRegression',
  'CorrelatedWithIncident',
  'Mitigated',
] as const;

/** Mapeia status de confiança para variante visual do Badge. */
function confidenceVariant(status: string): 'default' | 'success' | 'warning' | 'danger' | 'info' {
  switch (status) {
    case 'Validated':
      return 'success';
    case 'NeedsAttention':
      return 'warning';
    case 'SuspectedRegression':
    case 'CorrelatedWithIncident':
      return 'danger';
    case 'Mitigated':
      return 'info';
    default:
      return 'default';
  }
}

/** Determina cor da barra de score de mudança. */
function scoreColor(score: number): string {
  if (score < 0.3) return 'bg-success';
  if (score <= 0.6) return 'bg-warning';
  return 'bg-critical';
}

/**
 * ChangeCatalogPage — catálogo de mudanças do módulo Change Confidence.
 *
 * Exibe métricas resumidas no topo, filtros avançados e tabela paginada
 * de mudanças com navegação para o detalhe individual.
 */
export function ChangeCatalogPage() {
  const { t } = useTranslation();
  const navigate = useNavigate();
  const { availableEnvironments, activeEnvironmentId } = useEnvironment();

  // ── Filtros ──
  const [serviceName, setServiceName] = useState('');
  const [environment, setEnvironment] = useState('');
  const [changeType, setChangeType] = useState('');
  const [confidenceStatus, setConfidenceStatus] = useState('');
  const [searchInput, setSearchInput] = useState('');
  const [debouncedSearch, setDebouncedSearch] = useState('');
  const [page, setPage] = useState(1);

  // Debounce da pesquisa textual
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedSearch(searchInput), SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [searchInput]);

  // Reset de página ao mudar filtros
  useEffect(() => {

    setPage(1);
  }, [serviceName, environment, changeType, confidenceStatus, debouncedSearch]);

  /** Parâmetros construídos para a query de listagem. */
  const filterParams: ChangesFilterParams = useMemo(
    () => ({
      ...(serviceName && { serviceName }),
      ...(environment && { environment }),
      ...(changeType && { changeType }),
      ...(confidenceStatus && { confidenceStatus }),
      ...(debouncedSearch && { searchTerm: debouncedSearch }),
      page,
      pageSize: PAGE_SIZE,
    }),
    [serviceName, environment, changeType, confidenceStatus, debouncedSearch, page],
  );

  // ── Queries ──
  const summaryQuery = useQuery({
    queryKey: ['change-confidence-summary', environment, activeEnvironmentId],
    queryFn: () => changeConfidenceApi.getSummary(environment ? { environment } : undefined),
  });

  const changesQuery = useQuery({
    queryKey: ['change-confidence-list', filterParams, activeEnvironmentId],
    queryFn: () => changeConfidenceApi.listChanges(filterParams),
  });

  const filterOptionsQuery = useQuery({
    queryKey: ['change-confidence-filter-options'],
    queryFn: () => changeConfidenceApi.getFilterOptions(),
    staleTime: 5 * 60_000,
  });

  const summary = summaryQuery.data;
  const changes = useMemo(() => changesQuery.data?.changes ?? [], [changesQuery.data?.changes]);
  const totalCount = changesQuery.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  const environmentOptions = useMemo(() => {
    const set = new Set<string>();

    for (const env of availableEnvironments) {
      if (env.name) set.add(env.name);
    }

    for (const change of changes) {
      if (change.environment) set.add(change.environment);
    }

    if (environment) set.add(environment);

    return Array.from(set).sort((a, b) => a.localeCompare(b));
  }, [availableEnvironments, changes, environment]);

  const changeTypeOptions = useMemo(() => {
    const fromEndpoint = filterOptionsQuery.data?.changeTypes ?? [];
    const fromChanges = changes.map((c) => c.changeType).filter((v): v is string => Boolean(v));
    const set = new Set<string>([...fromEndpoint, ...fromChanges, ...(FALLBACK_CHANGE_TYPES as readonly string[])]);

    if (changeType) set.add(changeType);

    return Array.from(set);
  }, [filterOptionsQuery.data?.changeTypes, changes, changeType]);

  /** Opções de ambiente para o Select DS. */
  const environmentSelectOptions = useMemo(
    () => environmentOptions.map((env) => ({ value: env, label: env })),
    [environmentOptions],
  );

  /** Opções de tipo de mudança para o Select DS. */
  const changeTypeSelectOptions = useMemo(
    () => changeTypeOptions.map((ct) => ({ value: ct, label: t(`changeConfidence.changeType.${ct}`) || ct })),
    [changeTypeOptions, t],
  );

  /** Opções de status de confiança para o Select DS. */
  const confidenceStatusSelectOptions = useMemo(
    () =>
      CONFIDENCE_STATUSES.map((cs) => ({
        value: cs,
        label: t(`changeConfidence.confidenceStatus.${cs}`) || cs,
      })),
    [t],
  );

  /** Configuração dos StatCards de resumo. */
  const summaryCards = [
    {
      key: 'totalChanges',
      value: summaryQuery.isLoading ? '—' : String(summary?.totalChanges ?? 0),
      icon: <Activity size={18} />,
      color: 'text-accent' as const,
    },
    {
      key: 'validatedChanges',
      value: summaryQuery.isLoading ? '—' : String(summary?.validatedChanges ?? 0),
      icon: <CheckCircle2 size={18} />,
      color: 'text-success' as const,
    },
    {
      key: 'changesNeedingAttention',
      value: summaryQuery.isLoading ? '—' : String(summary?.changesNeedingAttention ?? 0),
      icon: <AlertTriangle size={18} />,
      color: 'text-warning' as const,
    },
    {
      key: 'suspectedRegressions',
      value: summaryQuery.isLoading ? '—' : String(summary?.suspectedRegressions ?? 0),
      icon: <XCircle size={18} />,
      color: 'text-critical' as const,
    },
    {
      key: 'correlatedWithIncidents',
      value: summaryQuery.isLoading ? '—' : String(summary?.changesCorrelatedWithIncidents ?? 0),
      icon: <AlertOctagon size={18} />,
      color: 'text-critical' as const,
    },
  ];

  return (
    <PageContainer>
      {/* ── Cabeçalho da página com CTA primário ── */}
      <PageHeader
        title={t('changeConfidence.title')}
        subtitle={t('changeConfidence.subtitle')}
        actions={
          <Button
            variant="primary"
            size="sm"
            icon={<Plus size={14} />}
            onClick={() => navigate('/changes/new')}
          >
            {t('common.register') ?? 'Register Change'}
          </Button>
        }
      />

      {/* ── KPI Cards de resumo ── */}
      <PageSection>
        <ContentGrid className="!grid-cols-2 lg:!grid-cols-5">
          {summaryCards.map(({ key, value, icon, color }) => (
            <StatCard
              key={key}
              title={t(`changeConfidence.summary.${key}`)}
              value={value}
              icon={icon}
              color={color}
            />
          ))}
        </ContentGrid>
      </PageSection>

      {/* ── Filtros + Tabela ── */}
      <PageSection>
        {/* Barra de filtros */}
        <Card>
          <CardBody>
            <div className="flex flex-wrap gap-3 items-center">
              {/* Pesquisa textual */}
              <SearchInput
                size="sm"
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                placeholder={t('changeConfidence.filters.searchPlaceholder')}
                className="flex-1 min-w-[200px]"
              />

              {/* Serviço */}
              <SearchInput
                size="sm"
                value={serviceName}
                onChange={(e) => setServiceName(e.target.value)}
                placeholder={t('changeConfidence.filters.serviceName')}
                className="w-44"
              />

              {/* Ambiente */}
              <Select
                size="sm"
                value={environment}
                onChange={(e) => setEnvironment(e.target.value)}
                placeholder={t('changeConfidence.filters.allEnvironments')}
                options={environmentSelectOptions}
                className="w-44"
              />

              {/* Tipo de mudança */}
              <Select
                size="sm"
                value={changeType}
                onChange={(e) => setChangeType(e.target.value)}
                placeholder={t('changeConfidence.filters.allTypes')}
                options={changeTypeSelectOptions}
                className="w-48"
              />

              {/* Status de confiança */}
              <Select
                size="sm"
                value={confidenceStatus}
                onChange={(e) => setConfidenceStatus(e.target.value)}
                placeholder={t('changeConfidence.filters.allStatuses')}
                options={confidenceStatusSelectOptions}
                className="w-48"
              />
            </div>
          </CardBody>
        </Card>

        {/* Tabela de mudanças */}
        <Card>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-edge text-left text-muted">
                  <th className="px-4 py-3 font-medium">{t('changeConfidence.table.service')}</th>
                  <th className="px-4 py-3 font-medium">{t('changeConfidence.table.version')}</th>
                  <th className="px-4 py-3 font-medium">{t('changeConfidence.table.environment')}</th>
                  <th className="px-4 py-3 font-medium">{t('changeConfidence.table.changeType')}</th>
                  <th className="px-4 py-3 font-medium">{t('changeConfidence.table.deploymentStatus')}</th>
                  <th className="px-4 py-3 font-medium">{t('changeConfidence.table.confidence')}</th>
                  <th className="px-4 py-3 font-medium">{t('changeConfidence.table.score')}</th>
                  <th className="px-4 py-3 font-medium">{t('changeConfidence.table.date')}</th>
                </tr>
              </thead>
              <tbody>
                {changesQuery.isLoading ? (
                  <tr>
                    <td colSpan={8}>
                      <PageLoadingState size="sm" />
                    </td>
                  </tr>
                ) : changesQuery.isError ? (
                  <tr>
                    <td colSpan={8}>
                      <PageErrorState className="py-8" />
                    </td>
                  </tr>
                ) : changes.length === 0 ? (
                  <tr>
                    <td colSpan={8}>
                      <EmptyState
                        title={t('changeConfidence.table.noChanges')}
                        description={t('changeConfidence.table.noChangesDescription')}
                        size="compact"
                      />
                    </td>
                  </tr>
                ) : (
                  changes.map((change: ChangeDto) => (
                    <ChangeRow
                      key={change.changeId}
                      change={change}
                      onClick={() => navigate(`/changes/${change.changeId}`)}
                      t={t}
                    />
                  ))
                )}
              </tbody>
            </table>
          </div>

          {/* ── Paginação ── */}
          {totalPages > 1 && (
            <div className="flex items-center justify-between px-4 py-3 border-t border-edge">
              <span className="text-xs text-muted">
                {totalCount} {t('common.total')}
              </span>
              <div className="flex items-center gap-1">
                <Button
                  variant="ghost"
                  size="xs"
                  icon={<ChevronLeft size={14} />}
                  disabled={page <= 1}
                  onClick={() => setPage((p) => Math.max(1, p - 1))}
                  aria-label="Previous page"
                >
                  {''}
                </Button>
                <span className="text-xs text-muted px-2">
                  {page} / {totalPages}
                </span>
                <Button
                  variant="ghost"
                  size="xs"
                  icon={<ChevronRight size={14} />}
                  disabled={page >= totalPages}
                  onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                  aria-label="Next page"
                >
                  {''}
                </Button>
              </div>
            </div>
          )}
        </Card>
      </PageSection>
    </PageContainer>
  );
}

// ── Sub-component ────────────────────────────────────────────────────────────

interface ChangeRowProps {
  change: ChangeDto;
  onClick: () => void;
  t: (key: string) => string;
}

/** Linha individual de mudança na tabela. */
function ChangeRow({ change, onClick, t }: ChangeRowProps) {
  return (
    <tr
      onClick={onClick}
      className="border-b border-edge last:border-b-0 hover:bg-hover cursor-pointer transition-colors"
    >
      <td className="px-4 py-3 font-medium text-heading">{change.serviceName}</td>
      <td className="px-4 py-3 text-body font-mono text-xs">{change.version}</td>
      <td className="px-4 py-3">
        <Badge>{change.environment}</Badge>
      </td>
      <td className="px-4 py-3 text-body">
        {t(`changeConfidence.changeType.${change.changeType}`) || change.changeType}
      </td>
      <td className="px-4 py-3 text-body">{change.deploymentStatus}</td>
      <td className="px-4 py-3">
        <Badge variant={confidenceVariant(change.confidenceStatus)}>
          {t(`changeConfidence.confidenceStatus.${change.confidenceStatus}`) || change.confidenceStatus}
        </Badge>
      </td>
      <td className="px-4 py-3">
        <div className="flex items-center gap-2">
          <div className="w-16 h-2 rounded-full bg-elevated overflow-hidden">
            <div
              className={`h-full rounded-full ${scoreColor(change.changeScore)}`}
              style={{ width: `${Math.min(100, change.changeScore * 100)}%` }}
            />
          </div>
          <span className="text-xs text-muted">{(change.changeScore * 100).toFixed(0)}%</span>
        </div>
      </td>
      <td className="px-4 py-3 text-xs text-muted">
        {new Date(change.createdAt).toLocaleDateString()}
      </td>
    </tr>
  );
}
