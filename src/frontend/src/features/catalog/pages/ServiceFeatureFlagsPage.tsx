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
  CheckCircle2,
  XCircle,
  Filter,
} from 'lucide-react';
import client from '../../../api/client';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';

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
    <PageContainer>
      <PageHeader
        title={t('featureFlags.title', 'Feature Flags')}
        subtitle={t('featureFlags.subtitle', 'Gerencie feature flags escopadas por serviço')}
      />
      {/* ── Header actions ── */}
      <div className="flex justify-end">
        <button
          onClick={() => refetch()}
          className="flex items-center gap-2 px-4 py-2 text-sm bg-accent text-on-accent rounded-lg hover:bg-accent/90 transition-colors"
        >
          <RefreshCw size={14} />
          {t('common.refresh', 'Atualizar')}
        </button>
      </div>

      {/* ── Stats Cards ── */}
      {data && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <div className="bg-card border border-edge rounded-lg p-4">
            <p className="text-xs font-medium text-muted uppercase tracking-wide">
              {t('featureFlags.totalFlags', 'Total de Flags')}
            </p>
            <p className="mt-1 text-2xl font-bold text-heading">{data.totalFlags}</p>
          </div>
          <div className="bg-card border border-edge rounded-lg p-4">
            <div className="flex items-center gap-2">
              <CheckCircle2 size={16} className="text-success" />
              <p className="text-xs font-medium text-muted uppercase tracking-wide">
                {t('featureFlags.enabled', 'Habilitadas')}
              </p>
            </div>
            <p className="mt-1 text-2xl font-bold text-success">{data.enabledFlags}</p>
          </div>
          <div className="bg-card border border-edge rounded-lg p-4">
            <div className="flex items-center gap-2">
              <XCircle size={16} className="text-muted" />
              <p className="text-xs font-medium text-muted uppercase tracking-wide">
                {t('featureFlags.disabled', 'Desabilitadas')}
              </p>
            </div>
            <p className="mt-1 text-2xl font-bold text-body">{data.disabledFlags}</p>
          </div>
          <div className="bg-card border border-edge rounded-lg p-4">
            <div className="flex items-center gap-2">
              <Server size={16} className="text-accent" />
              <p className="text-xs font-medium text-muted uppercase tracking-wide">
                {t('featureFlags.affectedServices', 'Serviços Afetados')}
              </p>
            </div>
            <p className="mt-1 text-2xl font-bold text-accent">{data.affectedServices}</p>
          </div>
        </div>
      )}

      {/* ── Filters ── */}
      <div className="flex flex-wrap gap-3 items-center">
        <div className="relative flex-1 min-w-[200px]">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
          <input
            className="w-full pl-9 pr-3 py-2 text-sm border border-edge rounded-lg bg-panel text-body placeholder:text-muted/50 focus:outline-none focus:ring-1 focus:ring-accent"
            placeholder={t('featureFlags.searchPlaceholder', 'Buscar por flag ou serviço...')}
            value={search}
            onChange={(e) => setSearch(e.target.value)}
          />
        </div>
        {services.length > 0 && (
          <div className="relative">
            <Filter size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
            <select
              className="pl-9 pr-8 py-2 text-sm border border-edge rounded-lg bg-panel text-body focus:outline-none focus:ring-1 focus:ring-accent appearance-none"
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
        <div className="flex rounded-lg border border-edge overflow-hidden">
          {(['all', 'enabled', 'disabled'] as const).map((s) => (
            <button
              key={s}
              onClick={() => setStatusFilter(s)}
              className={`px-3 py-2 text-xs font-medium transition-colors ${
                statusFilter === s
                  ? 'bg-accent text-on-accent'
                  : 'bg-panel text-body hover:bg-elevated'
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
          <div className="h-8 w-8 animate-spin rounded-full border-4 border-accent border-t-transparent" />
        </div>
      )}

      {isError && (
        <div className="bg-critical/10 border border-critical/30 rounded-lg p-4 flex items-start gap-3">
          <AlertTriangle size={18} className="text-critical mt-0.5 shrink-0" />
          <div>
            <p className="text-sm font-medium text-critical">
              {t('featureFlags.errorTitle', 'Erro ao carregar feature flags')}
            </p>
            <p className="text-xs text-critical/80 mt-1">
              {t('featureFlags.errorDesc', 'Verifique a conectividade e tente novamente.')}
            </p>
          </div>
        </div>
      )}

      {!isLoading && !isError && filtered.length === 0 && (
        <div className="text-center py-16 text-muted">
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
        <div className="bg-panel border border-edge rounded-lg overflow-hidden">
          <table className="w-full text-sm">
            <thead>
              <tr className="bg-elevated border-b border-edge">
                <th className="text-left px-4 py-3 text-xs font-semibold text-muted uppercase tracking-wide">
                  {t('featureFlags.colFlag', 'Flag')}
                </th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-muted uppercase tracking-wide">
                  {t('featureFlags.colService', 'Serviço')}
                </th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-muted uppercase tracking-wide">
                  {t('featureFlags.colEnvironment', 'Ambiente')}
                </th>
                <th className="text-left px-4 py-3 text-xs font-semibold text-muted uppercase tracking-wide">
                  {t('featureFlags.colUpdated', 'Atualizado')}
                </th>
                <th className="text-center px-4 py-3 text-xs font-semibold text-muted uppercase tracking-wide">
                  {t('featureFlags.colStatus', 'Status')}
                </th>
              </tr>
            </thead>
            <tbody className="divide-y divide-edge">
              {filtered.map((flag) => (
                <tr key={flag.id} className="hover:bg-elevated/30 transition-colors">
                  <td className="px-4 py-3">
                    <p className="font-medium text-heading">{flag.displayName}</p>
                    <p className="text-xs text-muted font-mono mt-0.5">{flag.flagKey}</p>
                    {flag.description && (
                      <p className="text-xs text-muted mt-0.5">{flag.description}</p>
                    )}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-1.5">
                      <Server size={13} className="text-muted" />
                      <span className="text-body">{flag.serviceName}</span>
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <span className="inline-flex items-center px-2 py-0.5 rounded text-xs bg-elevated text-body font-mono border border-edge">
                      {flag.environment}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-muted text-xs">
                    {new Date(flag.updatedAt).toLocaleDateString('pt-BR', {
                      day: '2-digit',
                      month: '2-digit',
                      year: '2-digit',
                      hour: '2-digit',
                      minute: '2-digit',
                    })}
                    {flag.updatedBy && (
                      <span className="block text-muted/70">{flag.updatedBy}</span>
                    )}
                  </td>
                  <td className="px-4 py-3 text-center">
                    <button
                      onClick={() => toggleMutation.mutate({ flagId: flag.id, enabled: !flag.enabled })}
                      disabled={toggleMutation.isPending}
                      title={flag.enabled ? t('featureFlags.disable', 'Desabilitar') : t('featureFlags.enable', 'Habilitar')}
                      className={`inline-flex items-center gap-1.5 px-2.5 py-1 rounded-full text-xs font-medium transition-colors disabled:opacity-50 ${
                        flag.enabled
                          ? 'bg-success/10 text-success border border-success/20'
                          : 'bg-elevated text-muted border border-edge'
                      }`}
                    >
                      {flag.enabled ? (
                        <>
                          <ToggleRight size={14} className="text-success" />
                          {t('featureFlags.on', 'Ativo')}
                        </>
                      ) : (
                        <>
                          <ToggleLeft size={14} className="text-muted" />
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
    </PageContainer>
  );
}
