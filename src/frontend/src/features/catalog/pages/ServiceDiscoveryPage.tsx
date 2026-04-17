import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Search,
  Radar,
  CheckCircle,
  XCircle,
  ArrowRight,
  RefreshCw,
  Eye,
  Clock,
  Server,
  Filter,
} from 'lucide-react';
import { serviceCatalogApi, type DiscoveredServiceItem, type DiscoveryStatus } from '../api/serviceCatalog';
import { PageErrorState } from '../../../components/PageErrorState';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

/**
 * Página de Service Discovery Automático.
 * Exibe serviços descobertos via telemetria OTel e permite triagem:
 * Match (associar a ServiceAsset), Register (criar novo), ou Ignore.
 * Inclui dashboard de estatísticas e histórico de execuções.
 */
export default function ServiceDiscoveryPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { activeEnvironmentId } = useEnvironment();
  const [statusFilter, setStatusFilter] = useState<string>('');
  const [envFilter, setEnvFilter] = useState('');
  const [searchTerm, setSearchTerm] = useState('');
  const [selectedService, setSelectedService] = useState<DiscoveredServiceItem | null>(null);
  const [actionType, setActionType] = useState<'match' | 'register' | 'ignore' | null>(null);

  // ── Queries ──────────────────────────────────────────────────────
  const { data: dashboard, isLoading: dashLoading, isError: dashError } = useQuery({
    queryKey: ['discovery-dashboard', activeEnvironmentId],
    queryFn: () => serviceCatalogApi.getDiscoveryDashboard(),
  });

  const { data: services, isLoading: servicesLoading, isError: servicesError } = useQuery({
    queryKey: ['discovered-services', statusFilter, envFilter, searchTerm, activeEnvironmentId],
    queryFn: () =>
      serviceCatalogApi.listDiscoveredServices({
        status: statusFilter || undefined,
        environment: envFilter || undefined,
        search: searchTerm || undefined,
      }),
  });

  // ── Mutations ────────────────────────────────────────────────────
  const matchMutation = useMutation({
    mutationFn: ({ id, serviceAssetId }: { id: string; serviceAssetId: string }) =>
      serviceCatalogApi.matchDiscoveredService(id, { serviceAssetId }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['discovered-services'] });
      queryClient.invalidateQueries({ queryKey: ['discovery-dashboard'] });
      closeAction();
    },
  });

  const registerMutation = useMutation({
    mutationFn: ({ id, domain, teamName }: { id: string; domain: string; teamName: string }) =>
      serviceCatalogApi.registerFromDiscovery(id, { domain, teamName }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['discovered-services'] });
      queryClient.invalidateQueries({ queryKey: ['discovery-dashboard'] });
      closeAction();
    },
  });

  const ignoreMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      serviceCatalogApi.ignoreDiscoveredService(id, { reason }),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['discovered-services'] });
      queryClient.invalidateQueries({ queryKey: ['discovery-dashboard'] });
      closeAction();
    },
  });

  const runDiscoveryMutation = useMutation({
    mutationFn: () => {
      const now = new Date();
      const from = new Date(now.getTime() - 24 * 60 * 60 * 1000);
      return serviceCatalogApi.runServiceDiscovery({
        environment: envFilter || 'production',
        from: from.toISOString(),
        until: now.toISOString(),
      });
    },
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['discovered-services'] });
      queryClient.invalidateQueries({ queryKey: ['discovery-dashboard'] });
    },
  });

  const closeAction = () => {
    setSelectedService(null);
    setActionType(null);
  };

  const statusColors: Record<DiscoveryStatus, string> = {
    Pending: 'bg-warning/10 text-warning border-warning/20',
    Matched: 'bg-accent/10 text-accent border-accent/20',
    Registered: 'bg-mint/10 text-mint border-mint/20',
    Ignored: 'bg-muted/10 text-muted border-muted/20',
  };

  const statusIcons: Record<DiscoveryStatus, typeof CheckCircle> = {
    Pending: Clock,
    Matched: ArrowRight,
    Registered: CheckCircle,
    Ignored: XCircle,
  };

  return (
    <div className="flex flex-col gap-6 p-6 animate-fade-in">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-xl font-semibold text-heading flex items-center gap-2">
            <Radar size={22} className="text-accent" />
            {t('catalog.discovery.title', 'Service Discovery')}
          </h1>
          <p className="text-sm text-muted mt-1">
            {t('catalog.discovery.subtitle', 'Automatically discover services from telemetry and manage catalog registration.')}
          </p>
        </div>
        <button
          onClick={() => runDiscoveryMutation.mutate()}
          disabled={runDiscoveryMutation.isPending}
          className="inline-flex items-center gap-2 px-4 py-2 text-sm font-medium rounded-lg bg-accent text-white hover:bg-accent/90 transition-colors disabled:opacity-50"
        >
          <RefreshCw size={14} className={runDiscoveryMutation.isPending ? 'animate-spin' : ''} />
          {t('catalog.discovery.runDiscovery', 'Run Discovery')}
        </button>
      </div>

      {/* Dashboard Error */}
      {dashError && (
        <PageErrorState message={t('common.errorLoading')} />
      )}

      {/* Dashboard Stats */}
      {!dashLoading && !dashError && dashboard && (
        <div className="grid grid-cols-2 md:grid-cols-3 lg:grid-cols-6 gap-3">
          <StatCard label={t('catalog.discovery.stats.total', 'Total')} value={dashboard.totalDiscovered} />
          <StatCard label={t('catalog.discovery.stats.pending', 'Pending')} value={dashboard.pending} accent="warning" />
          <StatCard label={t('catalog.discovery.stats.matched', 'Matched')} value={dashboard.matched} accent="accent" />
          <StatCard label={t('catalog.discovery.stats.registered', 'Registered')} value={dashboard.registered} accent="mint" />
          <StatCard label={t('catalog.discovery.stats.ignored', 'Ignored')} value={dashboard.ignored} />
          <StatCard label={t('catalog.discovery.stats.newThisWeek', 'New this week')} value={dashboard.newThisWeek} accent="accent" />
        </div>
      )}

      {/* Filters */}
      <div className="flex items-center gap-3 flex-wrap">
        <div className="relative flex-1 min-w-[200px] max-w-sm">
          <Search size={14} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
          <input
            type="text"
            value={searchTerm}
            onChange={(e) => setSearchTerm(e.target.value)}
            placeholder={t('catalog.discovery.searchPlaceholder', 'Search service name...')}
            className="w-full pl-9 pr-3 py-2 text-sm rounded-lg border border-edge bg-panel text-body placeholder:text-muted/50 focus:outline-none focus:ring-1 focus:ring-accent"
          />
        </div>
        <div className="flex items-center gap-1">
          <Filter size={14} className="text-muted" />
          <select
            value={statusFilter}
            onChange={(e) => setStatusFilter(e.target.value)}
            className="text-sm rounded-lg border border-edge bg-panel text-body px-3 py-2 focus:outline-none focus:ring-1 focus:ring-accent"
          >
            <option value="">{t('catalog.discovery.filters.allStatuses', 'All statuses')}</option>
            <option value="Pending">{t('catalog.discovery.filters.pending', 'Pending')}</option>
            <option value="Matched">{t('catalog.discovery.filters.matched', 'Matched')}</option>
            <option value="Registered">{t('catalog.discovery.filters.registered', 'Registered')}</option>
            <option value="Ignored">{t('catalog.discovery.filters.ignored', 'Ignored')}</option>
          </select>
        </div>
        <input
          type="text"
          value={envFilter}
          onChange={(e) => setEnvFilter(e.target.value)}
          placeholder={t('catalog.discovery.filters.environment', 'Environment')}
          className="text-sm rounded-lg border border-edge bg-panel text-body px-3 py-2 w-40 focus:outline-none focus:ring-1 focus:ring-accent"
        />
      </div>

      {/* Services Table */}
      <div className="border border-edge rounded-lg overflow-hidden bg-panel">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-edge bg-elevated/50">
              <th className="text-left px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                {t('catalog.discovery.table.service', 'Service')}
              </th>
              <th className="text-left px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                {t('catalog.discovery.table.environment', 'Environment')}
              </th>
              <th className="text-right px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                {t('catalog.discovery.table.traces', 'Traces')}
              </th>
              <th className="text-right px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                {t('catalog.discovery.table.endpoints', 'Endpoints')}
              </th>
              <th className="text-left px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                {t('catalog.discovery.table.lastSeen', 'Last Seen')}
              </th>
              <th className="text-left px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                {t('catalog.discovery.table.status', 'Status')}
              </th>
              <th className="text-right px-4 py-3 text-xs font-medium text-muted uppercase tracking-wider">
                {t('catalog.discovery.table.actions', 'Actions')}
              </th>
            </tr>
          </thead>
          <tbody className="divide-y divide-edge">
            {servicesError && (
              <tr>
                <td colSpan={7}>
                  <PageErrorState message={t('common.errorLoading')} />
                </td>
              </tr>
            )}
            {!servicesError && servicesLoading && (
              <tr>
                <td colSpan={7} className="text-center py-12 text-muted">
                  <RefreshCw size={16} className="animate-spin inline mr-2" />
                  {t('common.loading', 'Loading...')}
                </td>
              </tr>
            )}
            {!servicesError && !servicesLoading && services?.items.length === 0 && (
              <tr>
                <td colSpan={7} className="text-center py-12 text-muted">
                  <Radar size={24} className="inline mr-2 text-muted/30" />
                  {t('catalog.discovery.noServices', 'No discovered services found.')}
                </td>
              </tr>
            )}
            {services?.items.map((svc) => {
              const StatusIcon = statusIcons[svc.status];
              return (
                <tr key={svc.id} className="hover:bg-elevated/30 transition-colors">
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-2">
                      <Server size={14} className="text-muted flex-shrink-0" />
                      <div>
                        <span className="font-medium text-heading">{svc.serviceName}</span>
                        {svc.serviceNamespace && (
                          <span className="text-[10px] text-muted ml-1">({svc.serviceNamespace})</span>
                        )}
                      </div>
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <span className="text-xs px-2 py-0.5 rounded bg-elevated border border-edge">
                      {svc.environment}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right font-mono text-xs">{svc.traceCount.toLocaleString()}</td>
                  <td className="px-4 py-3 text-right font-mono text-xs">{svc.endpointCount}</td>
                  <td className="px-4 py-3 text-xs text-muted">
                    {new Date(svc.lastSeenAt).toLocaleDateString()}
                  </td>
                  <td className="px-4 py-3">
                    <span className={`inline-flex items-center gap-1 text-[11px] px-2 py-0.5 rounded border ${statusColors[svc.status]}`}>
                      <StatusIcon size={10} />
                      {t(`catalog.discovery.statuses.${svc.status}`, svc.status)}
                    </span>
                  </td>
                  <td className="px-4 py-3 text-right">
                    {svc.status === 'Pending' && (
                      <div className="flex items-center justify-end gap-1">
                        <button
                          onClick={() => { setSelectedService(svc); setActionType('match'); }}
                          className="text-[11px] px-2 py-1 rounded text-accent hover:bg-accent/10 transition-colors"
                          title={t('catalog.discovery.actions.match', 'Match')}
                        >
                          {t('catalog.discovery.actions.match', 'Match')}
                        </button>
                        <button
                          onClick={() => { setSelectedService(svc); setActionType('register'); }}
                          className="text-[11px] px-2 py-1 rounded text-mint hover:bg-mint/10 transition-colors"
                          title={t('catalog.discovery.actions.register', 'Register')}
                        >
                          {t('catalog.discovery.actions.register', 'Register')}
                        </button>
                        <button
                          onClick={() => { setSelectedService(svc); setActionType('ignore'); }}
                          className="text-[11px] px-2 py-1 rounded text-muted hover:bg-muted/10 transition-colors"
                          title={t('catalog.discovery.actions.ignore', 'Ignore')}
                        >
                          {t('catalog.discovery.actions.ignore', 'Ignore')}
                        </button>
                      </div>
                    )}
                    {svc.status !== 'Pending' && (
                      <button
                        onClick={() => { setSelectedService(svc); setActionType(null); }}
                        className="text-[11px] px-2 py-1 rounded text-muted hover:bg-elevated/50 transition-colors"
                      >
                        <Eye size={12} />
                      </button>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>

      {/* Recent Runs */}
      {dashboard && dashboard.recentRuns.length > 0 && (
        <div className="border border-edge rounded-lg bg-panel p-4">
          <h3 className="text-sm font-semibold text-heading mb-3">
            {t('catalog.discovery.recentRuns', 'Recent Discovery Runs')}
          </h3>
          <div className="space-y-2">
            {dashboard.recentRuns.map((run) => (
              <div key={run.runId} className="flex items-center justify-between text-xs text-muted py-1 border-b border-edge last:border-0">
                <div className="flex items-center gap-2">
                  <span className={`w-1.5 h-1.5 rounded-full ${run.status === 'Completed' ? 'bg-mint' : run.status === 'Failed' ? 'bg-danger' : 'bg-warning'}`} />
                  <span>{run.environment}</span>
                  <span className="text-muted/50">·</span>
                  <span>{new Date(run.startedAt).toLocaleString()}</span>
                </div>
                <div className="flex items-center gap-3">
                  <span>{run.servicesFound} {t('catalog.discovery.found', 'found')}</span>
                  <span className="text-accent">{run.newServicesFound} {t('catalog.discovery.new', 'new')}</span>
                  <span className={run.status === 'Completed' ? 'text-mint' : 'text-danger'}>{run.status}</span>
                </div>
              </div>
            ))}
          </div>
        </div>
      )}

      {/* Action Modal */}
      {selectedService && actionType && (
        <ActionModal
          service={selectedService}
          actionType={actionType}
          onClose={closeAction}
          onMatch={(serviceAssetId) =>
            matchMutation.mutate({ id: selectedService.id, serviceAssetId })
          }
          onRegister={(domain, teamName) =>
            registerMutation.mutate({ id: selectedService.id, domain, teamName })
          }
          onIgnore={(reason) =>
            ignoreMutation.mutate({ id: selectedService.id, reason })
          }
          isLoading={matchMutation.isPending || registerMutation.isPending || ignoreMutation.isPending}
        />
      )}
    </div>
  );
}

// ── Stat Card ────────────────────────────────────────────────────────

function StatCard({ label, value, accent }: { label: string; value: number; accent?: string }) {
  const colorClass = accent === 'warning' ? 'text-warning' : accent === 'accent' ? 'text-accent' : accent === 'mint' ? 'text-mint' : 'text-heading';
  return (
    <div className="border border-edge rounded-lg bg-panel p-3 text-center">
      <div className={`text-2xl font-bold ${colorClass}`}>{value}</div>
      <div className="text-[10px] text-muted uppercase tracking-wider mt-0.5">{label}</div>
    </div>
  );
}

// ── Action Modal ─────────────────────────────────────────────────────

function ActionModal({
  service,
  actionType,
  onClose,
  onMatch,
  onRegister,
  onIgnore,
  isLoading,
}: {
  service: DiscoveredServiceItem;
  actionType: 'match' | 'register' | 'ignore';
  onClose: () => void;
  onMatch: (serviceAssetId: string) => void;
  onRegister: (domain: string, teamName: string) => void;
  onIgnore: (reason: string) => void;
  isLoading: boolean;
}) {
  const { t } = useTranslation();
  const [serviceAssetId, setServiceAssetId] = useState('');
  const [domain, setDomain] = useState('');
  const [teamName, setTeamName] = useState('');
  const [reason, setReason] = useState('');

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center bg-black/50" onClick={onClose}>
      <div className="bg-panel border border-edge rounded-xl shadow-lg w-full max-w-md p-6" onClick={(e) => e.stopPropagation()}>
        <h3 className="text-base font-semibold text-heading mb-1">
          {actionType === 'match' && t('catalog.discovery.modal.matchTitle', 'Match to Existing Service')}
          {actionType === 'register' && t('catalog.discovery.modal.registerTitle', 'Register as New Service')}
          {actionType === 'ignore' && t('catalog.discovery.modal.ignoreTitle', 'Ignore Discovered Service')}
        </h3>
        <p className="text-xs text-muted mb-4">
          {t('catalog.discovery.modal.service', 'Service')}: <strong>{service.serviceName}</strong> ({service.environment})
        </p>

        {actionType === 'match' && (
          <div className="space-y-3">
            <label className="block text-xs font-medium text-heading">
              {t('catalog.discovery.modal.serviceAssetId', 'Service Asset ID')}
            </label>
            <input
              type="text"
              value={serviceAssetId}
              onChange={(e) => setServiceAssetId(e.target.value)}
              placeholder={t('catalog.discovery.modal.serviceAssetIdPlaceholder', 'Select or enter service ID...')}
              className="w-full px-3 py-2 text-sm rounded-lg border border-edge bg-elevated text-body focus:outline-none focus:ring-1 focus:ring-accent"
            />
          </div>
        )}

        {actionType === 'register' && (
          <div className="space-y-3">
            <div>
              <label className="block text-xs font-medium text-heading mb-1">
                {t('catalog.discovery.modal.domain', 'Domain')}
              </label>
              <input
                type="text"
                value={domain}
                onChange={(e) => setDomain(e.target.value)}
                placeholder={t('catalog.discovery.modal.domainPlaceholder', 'e.g. Payments')}
                className="w-full px-3 py-2 text-sm rounded-lg border border-edge bg-elevated text-body focus:outline-none focus:ring-1 focus:ring-accent"
              />
            </div>
            <div>
              <label className="block text-xs font-medium text-heading mb-1">
                {t('catalog.discovery.modal.teamName', 'Team')}
              </label>
              <input
                type="text"
                value={teamName}
                onChange={(e) => setTeamName(e.target.value)}
                placeholder={t('catalog.discovery.modal.teamNamePlaceholder', 'e.g. Platform Engineering')}
                className="w-full px-3 py-2 text-sm rounded-lg border border-edge bg-elevated text-body focus:outline-none focus:ring-1 focus:ring-accent"
              />
            </div>
          </div>
        )}

        {actionType === 'ignore' && (
          <div className="space-y-3">
            <label className="block text-xs font-medium text-heading">
              {t('catalog.discovery.modal.reason', 'Reason')}
            </label>
            <textarea
              value={reason}
              onChange={(e) => setReason(e.target.value)}
              placeholder={t('catalog.discovery.modal.reasonPlaceholder', 'e.g. Internal tooling, not a business service')}
              className="w-full px-3 py-2 text-sm rounded-lg border border-edge bg-elevated text-body focus:outline-none focus:ring-1 focus:ring-accent resize-none h-20"
            />
          </div>
        )}

        <div className="flex justify-end gap-2 mt-5">
          <button
            onClick={onClose}
            className="px-4 py-2 text-sm rounded-lg border border-edge text-muted hover:text-heading transition-colors"
          >
            {t('common.cancel', 'Cancel')}
          </button>
          <button
            disabled={isLoading}
            onClick={() => {
              if (actionType === 'match') onMatch(serviceAssetId);
              if (actionType === 'register') onRegister(domain, teamName);
              if (actionType === 'ignore') onIgnore(reason);
            }}
            className="px-4 py-2 text-sm rounded-lg bg-accent text-white hover:bg-accent/90 transition-colors disabled:opacity-50"
          >
            {isLoading ? t('common.loading', 'Loading...') : t('common.confirm', 'Confirm')}
          </button>
        </div>
      </div>
    </div>
  );
}
