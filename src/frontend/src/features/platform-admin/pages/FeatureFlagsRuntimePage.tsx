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
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center gap-3">
          <Layers size={24} className="text-indigo-600" />
          <div>
            <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
            <p className="mt-1 text-sm text-slate-500">{t('subtitle')}</p>
          </div>
        </div>
        <button
          onClick={() => refetch()}
          className="flex items-center gap-2 px-4 py-2 text-sm border border-slate-300 rounded-lg hover:bg-slate-50"
        >
          <RefreshCw size={14} />
          {t('refresh')}
        </button>
      </div>

      {/* Summary */}
      <div className="grid grid-cols-3 gap-4">
        <div className="bg-white border border-slate-200 rounded-lg p-4">
          <p className="text-xs text-slate-500">{t('totalFlags')}</p>
          <p className="text-2xl font-semibold text-slate-900 mt-1">{flags.length}</p>
        </div>
        <div className="bg-white border border-slate-200 rounded-lg p-4">
          <p className="text-xs text-slate-500">{t('enabledFlags')}</p>
          <p className="text-2xl font-semibold text-green-600 mt-1">{enabledCount}</p>
        </div>
        <div className="bg-white border border-slate-200 rounded-lg p-4">
          <p className="text-xs text-slate-500">{t('disabledFlags')}</p>
          <p className="text-2xl font-semibold text-slate-500 mt-1">{flags.length - enabledCount}</p>
        </div>
      </div>

      {/* Filters */}
      <div className="flex items-center gap-3">
        <div className="relative flex-1 max-w-sm">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
          <input
            type="text"
            value={search}
            onChange={(e) => setSearch(e.target.value)}
            placeholder={t('searchPlaceholder')}
            className="w-full pl-9 pr-4 py-2 text-sm border border-slate-300 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500"
          />
        </div>
        <select
          value={scopeFilter}
          onChange={(e) => setScopeFilter(e.target.value)}
          className="text-sm border border-slate-300 rounded-lg px-3 py-2 focus:outline-none"
        >
          {scopes.map((s) => (
            <option key={s} value={s}>
              {s === 'all' ? t('allScopes') : s}
            </option>
          ))}
        </select>
      </div>

      {isLoading && (
        <div className="flex items-center justify-center h-48 text-slate-400 text-sm">
          {t('loading')}
        </div>
      )}

      {isError && (
        <div className="flex items-center gap-3 p-4 bg-red-50 border border-red-200 rounded-lg text-red-700 text-sm">
          <XCircle size={18} />
          {t('error')}
        </div>
      )}

      {data && filtered.length === 0 && (
        <div className="flex items-center justify-center h-32 text-slate-400 text-sm">
          {t('noFlags')}
        </div>
      )}

      {data && filtered.length > 0 && (
        <div className="bg-white border border-slate-200 rounded-lg overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-slate-50 border-b border-slate-200">
                <th className="text-left px-4 py-3 text-xs font-medium text-slate-500 uppercase tracking-wide">
                  {t('colFlag')}
                </th>
                <th className="text-left px-4 py-3 text-xs font-medium text-slate-500 uppercase tracking-wide">
                  {t('colScope')}
                </th>
                <th className="text-left px-4 py-3 text-xs font-medium text-slate-500 uppercase tracking-wide">
                  {t('colDefault')}
                </th>
                <th className="text-center px-4 py-3 text-xs font-medium text-slate-500 uppercase tracking-wide">
                  {t('colStatus')}
                </th>
                <th className="text-center px-4 py-3 text-xs font-medium text-slate-500 uppercase tracking-wide">
                  {t('colToggle')}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {filtered.map((flag) => (
                <tr key={flag.key} className="hover:bg-slate-50">
                  <td className="px-4 py-3">
                    <div>
                      <p className="font-medium text-slate-900">{flag.displayName}</p>
                      <p className="text-xs text-slate-400 font-mono">{flag.key}</p>
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <span className="inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium bg-indigo-50 text-indigo-700">
                      {flag.scope}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-center">
                    {flag.defaultEnabled ? (
                      <CheckCircle size={16} className="text-green-500 mx-auto" />
                    ) : (
                      <XCircle size={16} className="text-slate-300 mx-auto" />
                    )}
                  </td>
                  <td className="px-4 py-3 text-center">
                    {flag.hasOverride && (
                      <span className="inline-flex items-center gap-1 text-xs text-amber-600">
                        <AlertTriangle size={12} />
                        {t('override')}
                      </span>
                    )}
                    {!flag.hasOverride && (
                      <span className="text-xs text-slate-400">{t('default')}</span>
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
                      className="text-indigo-600 hover:text-indigo-800 disabled:opacity-50"
                    >
                      {flag.enabled ? <ToggleRight size={22} /> : <ToggleLeft size={22} className="text-slate-400" />}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  );
}
