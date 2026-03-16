import { useState, useMemo, useEffect } from 'react';
import { useQuery } from '@tanstack/react-query';
import { Link } from 'react-router-dom';
import {
  Search,
  Server,
  FileText,
  BookOpen,
  ExternalLink,
  Globe,
  AlertTriangle,
} from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';
import { OnboardingHints } from '../../../components/OnboardingHints';
import { sourceOfTruthApi } from '../api/sourceOfTruth';

/** Delay de debounce para a pesquisa (ms). */
const SEARCH_DEBOUNCE_MS = 350;

/** Opções de escopo do filtro de pesquisa. */
const SCOPE_OPTIONS = [
  { value: '', labelKey: 'sourceOfTruth.search.all' },
  { value: 'services', labelKey: 'sourceOfTruth.search.services' },
  { value: 'contracts', labelKey: 'sourceOfTruth.search.contracts' },
  { value: 'docs', labelKey: 'sourceOfTruth.search.docs' },
  { value: 'runbooks', labelKey: 'sourceOfTruth.search.runbooks' },
] as const;

/** Variantes visuais para badges de criticidade. */
const criticalityColors: Record<string, string> = {
  Critical: 'bg-red-900/40 text-red-300 border border-red-700/50',
  High: 'bg-orange-900/40 text-orange-300 border border-orange-700/50',
  Medium: 'bg-yellow-900/40 text-yellow-300 border border-yellow-700/50',
  Low: 'bg-slate-800/40 text-slate-300 border border-slate-700/50',
};

/** Variantes visuais para badges de protocolo. */
const protocolColors: Record<string, string> = {
  OpenApi: 'bg-emerald-900/40 text-emerald-300 border border-emerald-700/50',
  Swagger: 'bg-teal-900/40 text-teal-300 border border-teal-700/50',
  Wsdl: 'bg-violet-900/40 text-violet-300 border border-violet-700/50',
  AsyncApi: 'bg-blue-900/40 text-blue-300 border border-blue-700/50',
  Protobuf: 'bg-amber-900/40 text-amber-300 border border-amber-700/50',
  GraphQl: 'bg-pink-900/40 text-pink-300 border border-pink-700/50',
};

/** Página de exploração e descoberta do Source of Truth — pesquisa unificada. */
export function SourceOfTruthExplorerPage() {
  const { t } = useTranslation();
  const [rawQuery, setRawQuery] = useState('');
  const [debouncedQuery, setDebouncedQuery] = useState('');
  const [scope, setScope] = useState('');

  /* Debounce da query de pesquisa. */
  useEffect(() => {
    const timer = setTimeout(() => setDebouncedQuery(rawQuery), SEARCH_DEBOUNCE_MS);
    return () => clearTimeout(timer);
  }, [rawQuery]);

  const { data, isLoading, isError } = useQuery({
    queryKey: ['source-of-truth-search', debouncedQuery, scope],
    queryFn: () =>
      sourceOfTruthApi.search({
        q: debouncedQuery,
        scope: scope || undefined,
        maxResults: 50,
      }),
    enabled: debouncedQuery.trim().length > 0,
    staleTime: 15_000,
  });

  const hasResults = useMemo(
    () =>
      !!data &&
      (data.services.length > 0 || data.contracts.length > 0 || data.references.length > 0),
    [data],
  );

  const showEmpty = debouncedQuery.trim().length > 0 && !isLoading && !hasResults;
  const showError = isError && debouncedQuery.trim().length > 0;

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* Onboarding hints — orientação contextual para Source of Truth */}
      <OnboardingHints module="knowledge" />

      {/* Header */}
      <div className="mb-8">
        <div className="flex items-center gap-3 mb-2">
          <div className="w-10 h-10 rounded-lg bg-accent/15 flex items-center justify-center">
            <Globe size={22} className="text-accent" />
          </div>
          <div>
            <h1 className="text-2xl font-bold text-heading">{t('sourceOfTruth.title')}</h1>
            <p className="text-sm text-muted">{t('sourceOfTruth.subtitle')}</p>
          </div>
        </div>
      </div>

      {/* Search bar */}
      <Card className="mb-8">
        <CardBody>
          <div className="flex flex-col sm:flex-row gap-3">
            <div className="relative flex-1">
              <Search size={18} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
              <input
                type="text"
                value={rawQuery}
                onChange={(e) => setRawQuery(e.target.value)}
                placeholder={t('sourceOfTruth.search.placeholder')}
                className="w-full pl-10 pr-4 py-2.5 rounded-lg bg-elevated border border-edge text-heading text-sm placeholder:text-muted outline-none focus:border-accent transition-colors"
                aria-label={t('sourceOfTruth.search.placeholder')}
              />
            </div>
            <div className="flex items-center gap-2">
              <span className="text-xs text-muted whitespace-nowrap">{t('sourceOfTruth.search.scope')}:</span>
              <div className="flex gap-1">
                {SCOPE_OPTIONS.map((opt) => (
                  <button
                    key={opt.value}
                    onClick={() => setScope(opt.value)}
                    className={`px-3 py-1.5 rounded-md text-xs font-medium transition-colors ${
                      scope === opt.value
                        ? 'bg-accent/15 text-accent border border-accent/30'
                        : 'bg-elevated text-muted border border-edge hover:text-body hover:bg-hover'
                    }`}
                  >
                    {t(opt.labelKey)}
                  </button>
                ))}
              </div>
            </div>
          </div>
        </CardBody>
      </Card>

      {/* Loading */}
      {isLoading && (
        <div className="flex items-center justify-center py-16">
          <p className="text-sm text-muted">{t('common.loading')}</p>
        </div>
      )}

      {/* Empty initial state */}
      {!debouncedQuery.trim() && !isLoading && (
        <EmptyState
          icon={<Globe size={24} />}
          title={t('sourceOfTruth.empty.title')}
          description={t('sourceOfTruth.empty.description')}
        />
      )}

      {/* No results */}
      {showEmpty && (
        <EmptyState
          icon={<Search size={24} />}
          title={t('sourceOfTruth.results.noResults')}
          description={t('sourceOfTruth.results.noResultsDescription')}
        />
      )}

      {/* Error */}
      {showError && (
        <EmptyState
          icon={<AlertTriangle size={24} />}
          title={t('common.error')}
          description={t('common.errorDescription')}
        />
      )}

      {/* Results */}
      {hasResults && data && (
        <div className="space-y-8">
          {/* Total */}
          <p className="text-sm text-muted">
            {data.totalResults} {t('sourceOfTruth.results.totalResults')}
          </p>

          {/* Services */}
          {data.services.length > 0 && (
            <section>
              <h2 className="text-lg font-semibold text-heading mb-4 flex items-center gap-2">
                <Server size={18} className="text-accent" />
                {t('sourceOfTruth.results.services')}
              </h2>
              <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {data.services.map((svc) => (
                  <Link
                    key={svc.serviceId}
                    to={`/source-of-truth/services/${svc.serviceId}`}
                    className="block group"
                  >
                    <Card className="hover:border-accent/40 transition-colors h-full">
                      <CardBody>
                        <div className="flex items-start justify-between mb-2">
                          <h3 className="text-sm font-semibold text-heading group-hover:text-accent transition-colors truncate">
                            {svc.displayName || svc.name}
                          </h3>
                          <ExternalLink size={14} className="text-muted shrink-0 opacity-0 group-hover:opacity-100 transition-opacity" />
                        </div>
                        <p className="text-xs text-muted mb-3">{svc.domain}</p>
                        <div className="flex items-center gap-2 flex-wrap">
                          <span className="text-[11px] px-2 py-0.5 rounded-full bg-blue-900/40 text-blue-300 border border-blue-700/50">
                            {svc.teamName}
                          </span>
                          {svc.criticality && (
                            <span className={`text-[11px] px-2 py-0.5 rounded-full ${criticalityColors[svc.criticality] ?? 'bg-slate-800/40 text-slate-300 border border-slate-700/50'}`}>
                              {t(`catalog.badges.criticality.${svc.criticality}`, svc.criticality)}
                            </span>
                          )}
                          <span className="text-[11px] px-2 py-0.5 rounded-full bg-slate-800/40 text-slate-300 border border-slate-700/50">
                            {t(`catalog.badges.lifecycle.${svc.lifecycleStatus}`, svc.lifecycleStatus)}
                          </span>
                        </div>
                      </CardBody>
                    </Card>
                  </Link>
                ))}
              </div>
            </section>
          )}

          {/* Contracts */}
          {data.contracts.length > 0 && (
            <section>
              <h2 className="text-lg font-semibold text-heading mb-4 flex items-center gap-2">
                <FileText size={18} className="text-accent" />
                {t('sourceOfTruth.results.contracts')}
              </h2>
              <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {data.contracts.map((c) => (
                  <Link
                    key={c.versionId}
                    to={`/source-of-truth/contracts/${c.versionId}`}
                    className="block group"
                  >
                    <Card className="hover:border-accent/40 transition-colors h-full">
                      <CardBody>
                        <div className="flex items-start justify-between mb-2">
                          <h3 className="text-sm font-semibold text-heading group-hover:text-accent transition-colors truncate">
                            {c.apiAssetId}
                          </h3>
                          <ExternalLink size={14} className="text-muted shrink-0 opacity-0 group-hover:opacity-100 transition-opacity" />
                        </div>
                        <div className="flex items-center gap-2 flex-wrap">
                          <span className="text-[11px] px-2 py-0.5 rounded-full bg-slate-800/40 text-slate-300 border border-slate-700/50">
                            v{c.semVer}
                          </span>
                          <span className={`text-[11px] px-2 py-0.5 rounded-full ${protocolColors[c.protocol] ?? 'bg-slate-800/40 text-slate-300 border border-slate-700/50'}`}>
                            {t(`contractGov.badges.protocols.${c.protocol}`, c.protocol)}
                          </span>
                          <span className="text-[11px] px-2 py-0.5 rounded-full bg-slate-800/40 text-slate-300 border border-slate-700/50">
                            {t(`contractGov.badges.lifecycle.${c.lifecycleState}`, c.lifecycleState)}
                          </span>
                        </div>
                      </CardBody>
                    </Card>
                  </Link>
                ))}
              </div>
            </section>
          )}

          {/* References */}
          {data.references.length > 0 && (
            <section>
              <h2 className="text-lg font-semibold text-heading mb-4 flex items-center gap-2">
                <BookOpen size={18} className="text-accent" />
                {t('sourceOfTruth.results.references')}
              </h2>
              <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-3">
                {data.references.map((ref) => (
                  <Card key={ref.referenceId} className="h-full">
                    <CardBody>
                      <div className="flex items-start justify-between mb-2">
                        <h3 className="text-sm font-semibold text-heading truncate">{ref.title}</h3>
                        {ref.url && (
                          <a
                            href={ref.url}
                            target="_blank"
                            rel="noopener noreferrer"
                            className="text-accent hover:text-accent/80 shrink-0"
                          >
                            <ExternalLink size={14} />
                          </a>
                        )}
                      </div>
                      <p className="text-xs text-muted mb-3 line-clamp-2">{ref.description}</p>
                      <div className="flex items-center gap-2">
                        <span className="text-[11px] px-2 py-0.5 rounded-full bg-slate-800/40 text-slate-300 border border-slate-700/50">
                          {t(`sourceOfTruth.assetTypes.${ref.assetType}`, ref.assetType)}
                        </span>
                        <span className="text-[11px] px-2 py-0.5 rounded-full bg-slate-800/40 text-slate-300 border border-slate-700/50">
                          {t(`sourceOfTruth.referenceTypes.${ref.referenceType}`, ref.referenceType)}
                        </span>
                      </div>
                    </CardBody>
                  </Card>
                ))}
              </div>
            </section>
          )}
        </div>
      )}
    </div>
  );
}
