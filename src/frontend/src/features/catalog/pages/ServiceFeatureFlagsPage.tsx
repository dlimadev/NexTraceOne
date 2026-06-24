import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Sliders,
  RefreshCw,
  Server,
  AlertTriangle,
  CheckCircle2,
  XCircle,
} from 'lucide-react';
import client from '../../../api/client';
import { useEnvironment } from '../../../contexts/EnvironmentContext';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button, SearchInput, Select, Toggle, Tabs } from '../../../shared/ui';

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

  const serviceOptions = [
    { value: '', label: t('featureFlags.allServices', 'Todos os Serviços') },
    ...services.map((s) => ({ value: s, label: s })),
  ];

  const statusItems = [
    { id: 'all', label: t('featureFlags.statusAll', 'Todas') },
    { id: 'enabled', label: t('featureFlags.statusEnabled', 'Ativas') },
    { id: 'disabled', label: t('featureFlags.statusDisabled', 'Inativas') },
  ];

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
        <Button
          variant="primary"
          size="sm"
          icon={<RefreshCw size={14} />}
          onClick={() => refetch()}
        >
          {t('common.refresh', 'Atualizar')}
        </Button>
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
        <SearchInput
          className="flex-1 min-w-[200px]"
          size="sm"
          placeholder={t('featureFlags.searchPlaceholder', 'Buscar por flag ou serviço...')}
          value={search}
          onChange={(e) => setSearch(e.target.value)}
        />
        {services.length > 0 && (
          <Select
            options={serviceOptions}
            size="sm"
            value={serviceFilter}
            onChange={(e) => setServiceFilter(e.target.value)}
          />
        )}
        <Tabs
          variant="pill"
          size="sm"
          activeId={statusFilter}
          onChange={(id) => setStatusFilter(id as 'all' | 'enabled' | 'disabled')}
          items={statusItems}
        />
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
                    <Toggle
                      checked={flag.enabled}
                      onChange={(checked) =>
                        toggleMutation.mutate({ flagId: flag.id, enabled: checked })
                      }
                      disabled={toggleMutation.isPending}
                      label={flag.enabled ? t('featureFlags.disable', 'Desabilitar') : t('featureFlags.enable', 'Habilitar')}
                    />
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
