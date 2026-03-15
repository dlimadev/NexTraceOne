import { useState, useEffect, useMemo } from 'react';
import { useQuery } from '@tanstack/react-query';
import { useNavigate } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import {
  Activity,
  CheckCircle,
  AlertTriangle,
  XCircle,
  AlertOctagon,
  Search,
  ChevronLeft,
  ChevronRight,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { changeConfidenceApi } from '../api/changeConfidence';
import type { ChangesFilterParams } from '../api/changeConfidence';
import type { ChangeDto } from '../../../types';

/** Delay em ms para debounce da pesquisa textual. */
const SEARCH_DEBOUNCE_MS = 350;

/** Tamanho padrão de página. */
const PAGE_SIZE = 20;

/** Opções de ambiente disponíveis. */
const ENVIRONMENTS = ['dev', 'staging', 'prod'] as const;

/** Tipos de mudança suportados pelo backend. */
const CHANGE_TYPES = [
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
    queryKey: ['change-confidence-summary', environment],
    queryFn: () => changeConfidenceApi.getSummary(environment ? { environment } : undefined),
  });

  const changesQuery = useQuery({
    queryKey: ['change-confidence-list', filterParams],
    queryFn: () => changeConfidenceApi.listChanges(filterParams),
  });

  const summary = summaryQuery.data;
  const changes = changesQuery.data?.changes ?? [];
  const totalCount = changesQuery.data?.totalCount ?? 0;
  const totalPages = Math.max(1, Math.ceil(totalCount / PAGE_SIZE));

  /** Configuração dos cards de resumo. */
  const summaryCards = [
    { key: 'totalChanges', value: summary?.totalChanges, icon: Activity, color: 'text-accent' },
    { key: 'validatedChanges', value: summary?.validatedChanges, icon: CheckCircle, color: 'text-success' },
    { key: 'changesNeedingAttention', value: summary?.changesNeedingAttention, icon: AlertTriangle, color: 'text-warning' },
    { key: 'suspectedRegressions', value: summary?.suspectedRegressions, icon: XCircle, color: 'text-critical' },
    { key: 'correlatedWithIncidents', value: summary?.changesCorrelatedWithIncidents, icon: AlertOctagon, color: 'text-critical' },
  ];

  return (
    <div className="p-6 lg:p-8 animate-fade-in space-y-6">
      {/* ── Header ── */}
      <div>
        <h1 className="text-2xl font-bold text-heading">{t('changeConfidence.title')}</h1>
        <p className="text-sm text-muted mt-1">{t('changeConfidence.subtitle')}</p>
      </div>

      {/* ── Summary cards ── */}
      <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-5 gap-4">
        {summaryCards.map(({ key, value, icon: Icon, color }) => (
          <Card key={key}>
            <CardBody className="flex items-center gap-3">
              <div className={`p-2 rounded-lg bg-elevated ${color}`}>
                <Icon size={20} />
              </div>
              <div>
                <p className="text-xs text-muted">{t(`changeConfidence.summary.${key}`)}</p>
                <p className="text-lg font-semibold text-heading">
                  {summaryQuery.isLoading ? '—' : (value ?? 0)}
                </p>
              </div>
            </CardBody>
          </Card>
        ))}
      </div>

      {/* ── Filters ── */}
      <Card>
        <CardBody>
          <div className="flex flex-wrap gap-3 items-center">
            {/* Pesquisa textual */}
            <div className="relative flex-1 min-w-[200px]">
              <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
              <input
                type="text"
                value={searchInput}
                onChange={(e) => setSearchInput(e.target.value)}
                placeholder={t('changeConfidence.filters.searchPlaceholder')}
                className="w-full pl-9 pr-3 py-2 rounded-md bg-elevated border border-edge text-sm text-heading placeholder:text-muted outline-none focus:border-accent transition-colors"
              />
            </div>

            {/* Serviço */}
            <input
              type="text"
              value={serviceName}
              onChange={(e) => setServiceName(e.target.value)}
              placeholder={t('changeConfidence.filters.serviceName')}
              className="w-40 px-3 py-2 rounded-md bg-elevated border border-edge text-sm text-heading placeholder:text-muted outline-none focus:border-accent transition-colors"
            />

            {/* Ambiente */}
            <select
              value={environment}
              onChange={(e) => setEnvironment(e.target.value)}
              className="px-3 py-2 rounded-md bg-elevated border border-edge text-sm text-heading outline-none focus:border-accent transition-colors"
            >
              <option value="">{t('changeConfidence.filters.allEnvironments')}</option>
              {ENVIRONMENTS.map((env) => (
                <option key={env} value={env}>{env}</option>
              ))}
            </select>

            {/* Tipo de mudança */}
            <select
              value={changeType}
              onChange={(e) => setChangeType(e.target.value)}
              className="px-3 py-2 rounded-md bg-elevated border border-edge text-sm text-heading outline-none focus:border-accent transition-colors"
            >
              <option value="">{t('changeConfidence.filters.allTypes')}</option>
              {CHANGE_TYPES.map((ct) => (
                <option key={ct} value={ct}>{t(`changeConfidence.changeType.${ct}`)}</option>
              ))}
            </select>

            {/* Status de confiança */}
            <select
              value={confidenceStatus}
              onChange={(e) => setConfidenceStatus(e.target.value)}
              className="px-3 py-2 rounded-md bg-elevated border border-edge text-sm text-heading outline-none focus:border-accent transition-colors"
            >
              <option value="">{t('changeConfidence.filters.allStatuses')}</option>
              {CONFIDENCE_STATUSES.map((cs) => (
                <option key={cs} value={cs}>{t(`changeConfidence.confidenceStatus.${cs}`)}</option>
              ))}
            </select>
          </div>
        </CardBody>
      </Card>

      {/* ── Table ── */}
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
                  <td colSpan={8} className="px-4 py-12 text-center text-muted">
                    {t('common.loading')}
                  </td>
                </tr>
              ) : changes.length === 0 ? (
                <tr>
                  <td colSpan={8} className="px-4 py-12 text-center">
                    <p className="text-muted font-medium">{t('changeConfidence.table.noChanges')}</p>
                    <p className="text-xs text-faded mt-1">{t('changeConfidence.table.noChangesDescription')}</p>
                  </td>
                </tr>
              ) : (
                changes.map((change: ChangeDto) => (
                  <ChangeRow key={change.changeId} change={change} onClick={() => navigate(`/changes/${change.changeId}`)} t={t} />
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
            <div className="flex items-center gap-2">
              <button
                disabled={page <= 1}
                onClick={() => setPage((p) => Math.max(1, p - 1))}
                className="p-1.5 rounded-md bg-elevated border border-edge text-muted hover:text-heading disabled:opacity-40 transition-colors"
              >
                <ChevronLeft size={16} />
              </button>
              <span className="text-xs text-muted">
                {page} / {totalPages}
              </span>
              <button
                disabled={page >= totalPages}
                onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                className="p-1.5 rounded-md bg-elevated border border-edge text-muted hover:text-heading disabled:opacity-40 transition-colors"
              >
                <ChevronRight size={16} />
              </button>
            </div>
          </div>
        )}
      </Card>
    </div>
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
