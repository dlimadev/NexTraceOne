import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  ToggleLeft,
  ToggleRight,
  RefreshCw,
  XCircle,
  AlertTriangle,
  CheckCircle,
  Search,
  Layers,
} from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { platformAdminApi, type FeatureFlagRuntimeEntry } from '../api/platformAdmin';

export function FeatureFlagsRuntimePage() {
  const { t } = useTranslation('featureFlagsRuntime');
  const queryClient = useQueryClient();
  const [search, setSearch] = useState('');
  const [scopeFilter, setScopeFilter] = useState<string>('all');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['feature-flags-runtime'],
    queryFn: platformAdminApi.getFeatureFlagsRuntime,
  });

  const toggleMutation = useMutation({
    mutationFn: ({ key, enabled, scope }: { key: string; enabled: boolean; scope: string }) =>
      platformAdminApi.setFeatureFlagRuntimeOverride({ key, enabled, scope }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['feature-flags-runtime'] });
    },
  });

  const flags = data?.flags ?? [];
  const scopes = data ? ['all', ...Array.from(new Set(flags.map((f) => f.scope)))] : ['all'];

  const filtered = flags.filter((f) => {
    const matchesSearch =
      !search ||
      f.key.toLowerCase().includes(search.toLowerCase()) ||
      f.displayName.toLowerCase().includes(search.toLowerCase());
    const matchesScope = scopeFilter === 'all' || f.scope === scopeFilter;
    return matchesSearch && matchesScope;
  });

  const enabledCount = flags.filter((f) => f.enabled).length;

  return (
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          icon={<Layers size={24} className="text-accent" />}
          actions={
            <Button variant="ghost" onClick={() => refetch()}>
              <RefreshCw size={14} />
              {t('refresh')}
            </Button>
          }
        />

        {/* Summary */}
        <div className="grid grid-cols-3 gap-4">
          <div className="bg-card border border-edge rounded-lg p-4">
            <p className="text-xs text-muted">{t('totalFlags')}</p>
            <p className="text-2xl font-semibold text-heading mt-1">{flags.length}</p>
          </div>
          <div className="bg-card border border-edge rounded-lg p-4">
            <p className="text-xs text-muted">{t('enabledFlags')}</p>
            <p className="text-2xl font-semibold text-success mt-1">{enabledCount}</p>
          </div>
          <div className="bg-card border border-edge rounded-lg p-4">
            <p className="text-xs text-muted">{t('disabledFlags')}</p>
            <p className="text-2xl font-semibold text-muted mt-1">{flags.length - enabledCount}</p>
          </div>
        </div>

        {/* Filters */}
        <div className="flex items-center gap-3">
          <div className="relative flex-1 max-w-sm">
            <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-faded" />
            <input
              type="text"
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              placeholder={t('searchPlaceholder')}
              className="w-full pl-9 pr-4 py-2 text-sm border border-edge rounded-lg bg-canvas text-body focus:outline-none focus:ring-2 focus:ring-accent/50"
            />
          </div>
          <select
            value={scopeFilter}
            onChange={(e) => setScopeFilter(e.target.value)}
            className="text-sm border border-edge rounded-lg px-3 py-2 bg-canvas text-body focus:outline-none"
          >
            {scopes.map((s) => (
              <option key={s} value={s}>
                {s === 'all' ? t('allScopes') : s}
              </option>
            ))}
          </select>
        </div>

        {isLoading && (
          <div className="flex items-center justify-center h-48 text-faded text-sm">
            {t('loading')}
          </div>
        )}

        {isError && (
          <div className="flex items-center gap-3 p-4 bg-critical/10 border border-critical/20 rounded-lg text-critical text-sm">
            <XCircle size={18} />
            {t('error')}
          </div>
        )}

        {data && filtered.length === 0 && (
          <div className="flex items-center justify-center h-32 text-faded text-sm">
            {t('noFlags')}
          </div>
        )}

        {data && filtered.length > 0 && (
          <div className="bg-card border border-edge rounded-lg overflow-hidden">
            <table className="w-full text-sm">
              <thead>
                <tr className="bg-elevated border-b border-edge">
                  <th className="text-left px-4 py-3 text-xs font-medium text-muted uppercase tracking-wide">
                    {t('colFlag')}
                  </th>
                  <th className="text-left px-4 py-3 text-xs font-medium text-muted uppercase tracking-wide">
                    {t('colScope')}
                  </th>
                  <th className="text-left px-4 py-3 text-xs font-medium text-muted uppercase tracking-wide">
                    {t('colDefault')}
                  </th>
                  <th className="text-center px-4 py-3 text-xs font-medium text-muted uppercase tracking-wide">
                    {t('colStatus')}
                  </th>
                  <th className="text-center px-4 py-3 text-xs font-medium text-muted uppercase tracking-wide">
                    {t('colToggle')}
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge/50">
                {filtered.map((flag) => (
                  <tr key={flag.key} className="hover:bg-elevated">
                    <td className="px-4 py-3">
                      <div>
                        <p className="font-medium text-heading">{flag.displayName}</p>
                        <p className="text-xs text-faded font-mono">{flag.key}</p>
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-accent/10 text-accent">
                        {flag.scope}
                      </span>
                    </td>
                    <td className="px-4 py-3 text-center">
                      {flag.defaultEnabled ? (
                        <CheckCircle size={16} className="text-success mx-auto" />
                      ) : (
                        <XCircle size={16} className="text-faded mx-auto" />
                      )}
                    </td>
                    <td className="px-4 py-3 text-center">
                      {flag.hasOverride && (
                        <span className="inline-flex items-center gap-1 text-xs text-warning">
                          <AlertTriangle size={12} />
                          {t('override')}
                        </span>
                      )}
                      {!flag.hasOverride && (
                        <span className="text-xs text-faded">{t('default')}</span>
                      )}
                    </td>
                    <td className="px-4 py-3 text-center">
                      <button
                        onClick={() =>
                          toggleMutation.mutate({
                            key: flag.key,
                            enabled: !flag.enabled,
                            scope: flag.scope,
                          })
                        }
                        disabled={toggleMutation.isPending}
                        aria-label={flag.enabled ? t('disable') : t('enable')}
                        className="text-accent hover:text-accent/80 disabled:opacity-50"
                      >
                        {flag.enabled ? <ToggleRight size={22} /> : <ToggleLeft size={22} className="text-faded" />}
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </PageContainer>
  );
}
