import { useState, useEffect } from 'react';
import { useSearchParams, Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Search,
  GitBranch,
  FileText,
  FileCode,
  BookOpen,
  Loader2,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';
import { Button, SearchInput } from '../../../shared/ui';
import { globalSearchApi } from '../api/globalSearch';
import type { SearchResultItem } from '../api/globalSearch';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageErrorState } from '../../../components/PageErrorState';
import { isRouteAvailableInFinalProductionScope } from '../../../releaseScope';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

/** Intervalo de debounce para pesquisa (ms). */
const SEARCH_DEBOUNCE_MS = 350;

/** Scopes disponíveis para filtragem — alinhados com ValidScopes do backend. */
const SCOPES = ['all', 'services', 'contracts', 'runbooks', 'docs'] as const;
type Scope = (typeof SCOPES)[number];

/** Mapeamento scope → chave i18n. */
const scopeLabelKeys: Record<Scope, string> = {
  all: 'commandPalette.globalSearch.scopeAll',
  services: 'commandPalette.globalSearch.scopeServices',
  contracts: 'commandPalette.globalSearch.scopeContracts',
  runbooks: 'commandPalette.globalSearch.scopeRunbooks',
  docs: 'commandPalette.globalSearch.scopeDocs',
};

/** Ícone por tipo de entidade. */
const entityTypeIcons: Record<string, React.ReactNode> = {
  service: <GitBranch size={16} />,
  contract: <FileText size={16} />,
  runbook: <FileCode size={16} />,
  doc: <BookOpen size={16} />,
};

/** Cores de badge por status de entidade (mesmo padrão do CommandPalette). */
const STATUS_COLOR_DEFAULT = 'bg-elevated text-muted';
const statusColors: Record<string, string> = {
  active: 'bg-success/20 text-success',
  healthy: 'bg-success/20 text-success',
  degraded: 'bg-warning/20 text-warning',
  draft: STATUS_COLOR_DEFAULT,
  deprecated: 'bg-critical/15 text-critical',
};

/** Página de resultados de pesquisa global — acessível via /search?q={query}. */
export function GlobalSearchPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [searchParams, setSearchParams] = useSearchParams();

  const initialQuery = searchParams.get('q') ?? '';
  const [inputValue, setInputValue] = useState(initialQuery);
  const [debouncedQuery, setDebouncedQuery] = useState(initialQuery);
  const [scope, setScope] = useState<Scope>('all');

  /** Debounce da pesquisa. */
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedQuery(inputValue), SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [inputValue]);

  /** Sincroniza a query string do URL quando o debounced query muda. */
  useEffect(() => {
    if (debouncedQuery) {
      setSearchParams({ q: debouncedQuery }, { replace: true });
    }
  }, [debouncedQuery, setSearchParams]);

  const searchEnabled = debouncedQuery.trim().length >= 1;

  const { data, isLoading, isError } = useQuery({
    queryKey: ['globalSearch-page', debouncedQuery, scope, activeEnvironmentId],
    queryFn: () =>
      globalSearchApi.search({
        q: debouncedQuery.trim(),
        scope: scope === 'all' ? undefined : scope,
        maxResults: 100,
      }),
    enabled: searchEnabled,
    staleTime: 30_000,
  });

  const results = data?.items ?? [];
  const facetCounts = data?.facetCounts ?? {};
  const totalResults = data?.totalResults ?? 0;

  /** Resultados da API — já filtrados por scope no backend. */
  const filteredResults = results.filter((item) => isRouteAvailableInFinalProductionScope(item.route));

  return (
    <PageContainer>
      <PageHeader
        title={t('commandPalette.globalSearch.title')}
        subtitle={t('commandPalette.globalSearch.subtitle')}
        icon={<Search size={24} />}
      />

      {/* ── Pesquisa ── */}
      <Card className="mb-6">
        <CardBody>
          <SearchInput
            placeholder={t('commandPalette.globalSearch.searchPlaceholder')}
            value={inputValue}
            onChange={(e) => setInputValue(e.target.value)}
            autoFocus
          />
        </CardBody>
      </Card>

      {/* ── Scope filter pills ── */}
      <div className="flex flex-wrap items-center gap-2 mb-6">
        {SCOPES.map((s) => {
          const isActive = scope === s;
          const count = s === 'all' ? totalResults : (facetCounts[s] ?? 0);
          return (
            <Button
              key={s}
              variant="ghost"
              size="xs"
              onClick={() => setScope(s)}
              className={`rounded-full ${isActive ? 'bg-accent/15 text-accent' : 'bg-elevated text-muted'}`}
            >
              {t(scopeLabelKeys[s])}
              {searchEnabled && (
                <span className={`text-[10px] ${isActive ? 'text-accent/70' : 'text-faded'}`}>
                  {count}
                </span>
              )}
            </Button>
          );
        })}
      </div>

      {/* ── Contagem de resultados ── */}
      {searchEnabled && !isLoading && !isError && (
        <p className="text-xs text-muted mb-4">
          {t('commandPalette.globalSearch.resultCount', { count: filteredResults.length })}
        </p>
      )}

      {/* ── Estado de loading ── */}
      {isLoading && (
        <div className="flex items-center justify-center py-16">
          <Loader2 size={24} className="text-muted animate-spin" />
        </div>
      )}

      {/* ── Erro ── */}
      {isError && (
        <PageErrorState />
      )}

      {/* ── Sem resultados ── */}
      {searchEnabled && !isLoading && !isError && filteredResults.length === 0 && (
        <EmptyState
          icon={<Search size={24} />}
          title={t('commandPalette.globalSearch.noResults')}
          description={t('commandPalette.globalSearch.noResultsHint')}
        />
      )}

      {/* ── Resultados ── */}
      {!isLoading && !isError && filteredResults.length > 0 && (
        <div className="flex flex-col gap-3">
          {filteredResults.map((item) => (
            <SearchResultCard key={item.entityId} item={item} />
          ))}
        </div>
      )}

      {/* ── Sem query ainda ── */}
      {!searchEnabled && !isLoading && (
        <EmptyState
          icon={<Search size={24} />}
          title={t('commandPalette.globalSearch.title')}
          description={t('commandPalette.globalSearch.subtitle')}
        />
      )}
    </PageContainer>
  );
}

/* ── Componente interno: card de resultado individual ─────────────── */

function SearchResultCard({
  item,
}: {
  item: SearchResultItem;
}) {
  const { t } = useTranslation();
  const isService = item.entityType.toLowerCase() === 'service';

  return (
    <div className="bg-panel border border-edge rounded-lg hover:bg-hover transition-colors flex items-center gap-2">
      <Link
        to={item.route}
        className="flex-1 min-w-0 flex items-center gap-4 p-4"
      >
        {/* Ícone por tipo */}
        <div className="flex items-center justify-center w-9 h-9 rounded-lg bg-elevated text-muted shrink-0">
          {entityTypeIcons[item.entityType.toLowerCase()] ?? <Search size={16} />}
        </div>

        {/* Texto principal */}
        <div className="flex-1 min-w-0">
          <p className="text-sm font-medium text-heading truncate">{item.title}</p>
          <p className="text-xs text-muted truncate mt-0.5">
            {item.subtitle ?? t(`commandPalette.entity${capitalize(item.entityType)}`, item.entityType)}
            {item.owner && (
              <span className="text-faded"> · {item.owner}</span>
            )}
          </p>
        </div>

        {/* Status badge */}
        {item.status && (
          <span
            className={`shrink-0 rounded-full px-2 py-0.5 text-[10px] font-medium ${
              statusColors[item.status] ?? STATUS_COLOR_DEFAULT
            }`}
          >
            {item.status}
          </span>
        )}
      </Link>

      {/* Ponte para a vista Source of Truth — apenas serviços (entityId===serviceId) */}
      {isService && (
        <Link
          to={`/source-of-truth/services/${item.entityId}`}
          className="shrink-0 pr-4 text-xs text-accent hover:underline whitespace-nowrap"
        >
          {t('commandPalette.globalSearch.sourceOfTruthLink', 'Source of truth')}
        </Link>
      )}
    </div>
  );
}

/** Capitaliza a primeira letra de uma string. */
function capitalize(s: string): string {
  return s.charAt(0).toUpperCase() + s.slice(1);
}
