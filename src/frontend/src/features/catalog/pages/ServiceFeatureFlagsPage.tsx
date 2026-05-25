import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Sliders,
  RefreshCw,
  Search,
  ToggleLeft,
  ToggleRight,
  Server,
  AlertTriangle,
  CheckCircle,
  XCircle,
  Filter,
} from 'lucide-react';
import client from '../../../api/client';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

/**
 * Página de Feature Flags por Serviço.
 * Exibe e permite gerenciar feature flags escopadas por serviço
 * (tabela ctr_feature_flag_records com TenantId + ServiceId + FlagKey).
 */

export interface ServiceFeatureFlag {
  id: string;
  serviceId: string;
  serviceName: string;
  flagKey: string;
  displayName: string;
  description?: string;
  enabled: boolean;
  environment: string;
  updatedAt: string;
  updatedBy?: string;
}

export interface ServiceFeatureFlagDashboard {
  totalFlags: number;
  enabledFlags: number;
  disabledFlags: number;
  affectedServices: number;
  flags: ServiceFeatureFlag[];
}

const serviceFeatureFlagsApi = {
  getDashboard: async (): Promise<ServiceFeatureFlagDashboard> => {
    const res = await client.get<ServiceFeatureFlagDashboard>('/catalog/feature-flags');
    return res.data;
  },
  toggle: async (flagId: string, enabled: boolean): Promise<void> => {
    await client.patch(`/catalog/feature-flags/${flagId}`, { enabled });
  },
};

export function ServiceFeatureFlagsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { activeEnvironmentId } = useEnvironment();

  const [search, setSearch] = useState('');
  const [serviceFilter, setServiceFilter] = useState('');
  const [statusFilter, setStatusFilter] = useState<'all' | 'enabled' | 'disabled'>('all');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['service-feature-flags', activeEnvironmentId],
    queryFn: () => serviceFeatureFlagsApi.getDashboard(),
  });

  const toggleMutation = useMutation({
    mutationFn: ({ flagId, enabled }: { flagId: string; enabled: boolean }) =>
      serviceFeatureFlagsApi.toggle(flagId, enabled),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['service-feature-flags'] });
    },
  });

  const flags = data?.flags ?? [];

  const services = Array.from(new Set(flags.map((f) => f.serviceName))).sort();

  const filtered = flags.filter((f) => {
    const matchesSearch =
      !search ||
      f.flagKey.toLowerCase().includes(search.toLowerCase()) ||
      f.displayName.toLowerCase().includes(search.toLowerCase()) ||
      f.serviceName.toLowerCase().includes(search.toLowerCase());
    const matchesService = !serviceFilter || f.serviceName === serviceFilter;
    const matchesStatus =
      statusFilter === 'all' ||
      (statusFilter === 'enabled' && f.enabled) ||
      (statusFilter === 'disabled' && !f.enabled);
    return matchesSearch && matchesService && matchesStatus;
  });

  return (
    <div className="p-6 space-y-6">
      {/* ── Header ── */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900 flex items-center gap-2">
            <Sliders size={24} className="text-indigo-600" />
            {t('featureFlags.title', 'Feature Flags')}
          </h1>
          <p className="mt-1 text-sm text-slate-500">
            {t('featureFlags.subtitle', 'Gerencie feature flags escopadas por serviço')}
          </p>
        </div>
        <button
          onClick={() => refetch()}
          className="flex items-center gap-2 px-4 py-2 text-sm bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
        >
          <RefreshCw size={14} />
          {t('common.refresh', 'Atualizar')}
        </button>
      </div>

      {/* ── Stats Cards ── */}
      {data && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div className="bg-white border border-slate-200 rounded-lg p-4 shadow-sm">
            <p className="text-xs font-medium text-slate-500 uppercase tracking-wide">
              {t('featureFlags.totalFlags', 'Total de Flags')}
            </p>
            <p className="mt-1 text-2xl font-bold text-slate-900">{data.totalFlags}</p>
          </div>
          <div className="bg-white border border-slate-200 rounded-lg p-4 shadow-sm">
            <div className="flex items-center gap-2">
              <CheckCircle size={16} className="text-emerald-500" />
              <p className="text-xs font-medium text-slate-500 uppercase tracking-wide">
                {t('featureFlags.enabled', 'Habilitadas')}
              </p>
            </div>
            <p className="mt-1 text-2xl font-bold text-emerald-700">{data.enabledFlags}</p>
          </div>
          <div className="bg-white border border-slate-200 rounded-lg p-4 shadow-sm">
            <div className="flex items-center gap-2">
              <XCircle size={16} className="text-slate-400" />
              <p className="text-xs font-medium text-slate-500 uppercase tracking-wide">
                {t('featureFlags.disabled', 'Desabilitadas')}
              </p>
            </div>
            <p className="mt-1 text-2xl font-bold text-slate-600">{data.disabledFlags}</p>
          </div>
          <div className="bg-white border border-slate-200 rounded-lg p-4 shadow-sm">
            <div className="flex items-center gap-2">
              <Server size={16} className="text-indigo-500" />
              <p className="text-xs font-medium text-slate-500 uppercase tracking-wide">
                {t('featureFlags.affectedServices', 'Serviços Afetados')}
              </p>
            </div>
            <p className="mt-1 text-2xl font-bold text-indigo-700">{data.affectedServices}</p>
          </div>
        </div>
      )}

      {/* ── Filters ── */}
      <div className="flex flex-wrap gap-3 items-center">
        <div className="relative flex-1 min-w-[200px]">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
          <input
            className="w-full pl-9 pr-3 py-2 text-sm border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500"
            placeholder={t('featureFlags.searchPlaceholder', 'Buscar por flag ou serviço...')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
        {services.length > 0 && (
          <div className="relative">
            <Filter size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-slate-400" />
            <select
              className="pl-9 pr-8 py-2 text-sm border border-slate-200 rounded-lg focus:outline-none focus:ring-2 focus:ring-indigo-500 appearance-none bg-white"
              value={serviceFilter}
              onChange={(e) => setServiceFilter(e.target.value)}
            >
              <option value="">{t('featureFlags.allServices', 'Todos os Serviços')}</option>
              {services.map((s) => (
                <option key={s} value={s}>{s}</option>
              ))}
            </select>
          </div>
        )}
        <div className="flex rounded-lg border border-slate-200 overflow-hidden">
          {(['all', 'enabled', 'disabled'] as const).map((s) => (
            <button
              key={s}
              onClick={() => setStatusFilter(s)}
              className={`px-3 py-2 text-xs font-medium transition-colors ${
                statusFilter === s
                  ? 'bg-indigo-600 text-white'
                  : 'bg-white text-slate-600 hover:bg-slate-50'
              }`}
            >
              {s === 'all'
                ? t('featureFlags.statusAll', 'Todas')
                : s === 'enabled'
                ? t('featureFlags.statusEnabled', 'Ativas')
                : t('featureFlags.statusDisabled', 'Inativas')}
            </button>
          ))}
        </div>
      </div>

      {/* ── Content ── */}
      {isLoading && (
        <div className="flex justify-center py-16">
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-indigo-500 border-t-transparent" />
        </div>
      )}

      {isError && (
        <div className="bg-red-50 border border-red-200 rounded-lg p-4 flex items-start gap-3">
          <AlertTriangle size={18} className="text-red-500 mt-0.5 shrink-0" />
          <div>
            <p className="text-sm font-medium text-red-800">
              {t('featureFlags.errorTitle', 'Erro ao carregar feature flags')}
            </p>
            <p className="text-xs text-red-600 mt-1">
              {t('featureFlags.errorDesc', 'Verifique a conectividade e tente novamente.')}
            </p>
          </div>
        </div>
      )}

      {!isLoading && !isError && filtered.length === 0 && (
        <div className="text-center py-16 text-slate-400">
          <Sliders size={40} className="mx-auto mb-4 opacity-40" />
          <p className="text-sm font-medium">
            {t('featureFlags.empty', 'Nenhuma feature flag encontrada')}
          </p>
          <p className="text-xs mt-1">
            {t('featureFlags.emptyDesc', 'Ajuste os filtros ou registre feature flags nos serviços.')}
          </p>
        </div>
      )}

      {!isLoading && !isError && filtered.length > 0 && (
        <div className="bg-white border border-slate-200 rounded-lg shadow-sm overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-slate-50 border-b border-slate-200">
                <th className="text-left px-4 py-3 text-xs font-semibold text-slate-500 uppercase tracking-wide">
                  {t('featureFlags.colFlag', 'Flag')}
                </th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-slate-500 uppercase tracking-wide">
                  {t('featureFlags.colService', 'Serviço')}
                </th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-slate-500 uppercase tracking-wide">
                  {t('featureFlags.colEnvironment', 'Ambiente')}
                </th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-slate-500 uppercase tracking-wide">
                  {t('featureFlags.colUpdated', 'Atualizado')}
                </th>
                <th className="text-center px-4 py-3 text-xs font-semibold text-slate-500 uppercase tracking-wide">
                  {t('featureFlags.colStatus', 'Status')}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-slate-100">
              {filtered.map((flag) => (
                <tr key={flag.id} className="hover:bg-slate-50 transition-colors">
                  <td className="px-4 py-3">
                    <p className="font-medium text-slate-900">{flag.displayName}</p>
                    <p className="text-xs text-slate-400 font-mono mt-0.5">{flag.flagKey}</p>
                    {flag.description && (
                      <p className="text-xs text-slate-500 mt-0.5">{flag.description}</p>
                    )}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-1.5">
                      <Server size={13} className="text-slate-400" />
                      <span className="text-slate-700">{flag.serviceName}</span>
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <span className="inline-flex items-center px-2 py-0.5 rounded text-xs bg-slate-100 text-slate-600 font-mono">
                      {flag.environment}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-slate-500 text-xs">
                    {new Date(flag.updatedAt).toLocaleDateString('pt-BR', {
                      day: '2-digit',
                      month: '2-digit',
                      year: '2-digit',
                      hour: '2-digit',
                      minute: '2-digit',
                    })}
                    {flag.updatedBy && (
                      <span className="block text-slate-400">{flag.updatedBy}</span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-center">
                    <button
                      onClick={() => toggleMutation.mutate({ flagId: flag.id, enabled: !flag.enabled })}
                      disabled={toggleMutation.isPending}
                      title={flag.enabled ? t('featureFlags.disable', 'Desabilitar') : t('featureFlags.enable', 'Habilitar')}
                      className="inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium transition-colors disabled:opacity-50"
                      style={{
                        backgroundColor: flag.enabled ? '#dcfce7' : '#f1f5f9',
                        color: flag.enabled ? '#166534' : '#475569',
                      }}
                    >
                      {flag.enabled ? (
                        <>
                          <ToggleRight size={14} className="text-emerald-600" />
                          {t('featureFlags.on', 'Ativo')}
                        </>
                      ) : (
                        <>
                          <ToggleLeft size={14} className="text-slate-400" />
                          {t('featureFlags.off', 'Inativo')}
                        </>
                      )}
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
