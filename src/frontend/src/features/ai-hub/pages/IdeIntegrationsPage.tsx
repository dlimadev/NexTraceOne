import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Monitor, Code, Search, Shield, ShieldCheck, Plug,
  Clock, FileText, AlertTriangle, Zap, Activity,
} from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Loader } from '../../../components/Loader';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { Button } from '../../../components/Button';
import { aiGovernanceApi } from '../api';

interface IdeQuerySession {
  sessionId: string;
  userId: string;
  ideClient: string;
  ideClientVersion: string;
  queryType: string;
  queryText: string;
  status: string;
  modelUsed: string;
  tokensUsed: number;
  responseTimeMs: number | null;
  submittedAt: string;
}

interface IdeClient {
  id: string;
  userId: string;
  userDisplayName: string;
  clientType: string;
  clientVersion: string;
  deviceIdentifier: string;
  lastAccessAt: string;
  isActive: boolean;
}

interface IdeCapabilityPolicy {
  id: string;
  clientType: string;
  persona: string | null;
  allowedCommands: string;
  allowedContextScopes: string;
  allowContractGeneration: boolean;
  allowIncidentTroubleshooting: boolean;
  allowExternalAI: boolean;
  maxTokensPerRequest: number;
  isActive: boolean;
}

type ClientFilter = 'all' | 'VsCode' | 'VisualStudio';

function timeAgo(dateStr: string): string {
  if (!dateStr) return '—';
  const diff = Date.now() - new Date(dateStr).getTime();
  const mins = Math.floor(diff / 60000);
  if (mins < 60) return `${mins}m`;
  const hrs = Math.floor(mins / 60);
  if (hrs < 24) return `${hrs}h`;
  return `${Math.floor(hrs / 24)}d`;
}

/**
 * Página de IDE Integrations — gestão administrativa de integrações com IDE.
 * Mostra clientes registados, políticas de capacidade e estado geral.
 * Parte do módulo AI Hub do NexTraceOne.
 */
export function IdeIntegrationsPage() {
  const { t } = useTranslation();
  const [clientFilter, setClientFilter] = useState<ClientFilter>('all');
  const [search, setSearch] = useState('');

  const {
    data: summary,
    isLoading: isLoadingSummary,
    isError: isSummaryError,
    refetch: refetchSummary,
  } = useQuery({
    queryKey: ['ai-governance', 'ide', 'summary'],
    queryFn: () => aiGovernanceApi.getIdeSummary(),
    staleTime: 30_000,
  });

  const {
    data: clientsData,
    isLoading: isLoadingClients,
    isError: isClientsError,
    refetch: refetchClients,
  } = useQuery({
    queryKey: ['ai-governance', 'ide', 'clients'],
    queryFn: () => aiGovernanceApi.listIdeClients({ pageSize: 200 }),
    staleTime: 30_000,
  });

  const {
    data: policiesData,
    isLoading: isLoadingPolicies,
    isError: isPoliciesError,
    refetch: refetchPolicies,
  } = useQuery({
    queryKey: ['ai-governance', 'ide', 'policies'],
    queryFn: () => aiGovernanceApi.listIdeCapabilityPolicies({ pageSize: 200 }),
    staleTime: 30_000,
  });

  const { data: vsCodeCapabilities } = useQuery({
    queryKey: ['ai-governance', 'ide', 'capabilities', 'VsCode'],
    queryFn: () => aiGovernanceApi.getIdeCapabilities({ clientType: 'VsCode', persona: null }),
    staleTime: 60_000,
  });

  const { data: vsCapabilities } = useQuery({
    queryKey: ['ai-governance', 'ide', 'capabilities', 'VisualStudio'],
    queryFn: () => aiGovernanceApi.getIdeCapabilities({ clientType: 'VisualStudio', persona: null }),
    staleTime: 60_000,
  });

  const { data: querySessionsData } = useQuery({
    queryKey: ['ai-governance', 'ide', 'query-sessions'],
    queryFn: () => aiGovernanceApi.listIdeQuerySessions(),
    staleTime: 30_000,
  });

  const querySessions: IdeQuerySession[] = useMemo(() => {
    const items = (querySessionsData?.items ?? []) as Array<{
      sessionId: string;
      userId: string;
      ideClient: string;
      ideClientVersion: string;
      queryType: string;
      queryText: string;
      status: string;
      modelUsed: string;
      tokensUsed: number;
      responseTimeMs?: number | null;
      submittedAt: string;
    }>;
    return items.map((s) => ({
      sessionId: s.sessionId,
      userId: s.userId,
      ideClient: s.ideClient,
      ideClientVersion: s.ideClientVersion,
      queryType: s.queryType,
      queryText: s.queryText,
      status: s.status,
      modelUsed: s.modelUsed,
      tokensUsed: s.tokensUsed,
      responseTimeMs: s.responseTimeMs ?? null,
      submittedAt: s.submittedAt,
    }));
  }, [querySessionsData]);

  const clients: IdeClient[] = useMemo(() => {
    const items = (clientsData?.items ?? []) as Array<{
      registrationId: string;
      userId: string;
      userDisplayName: string;
      clientType: string;
      clientVersion?: string | null;
      deviceIdentifier?: string | null;
      lastAccessAt?: string | null;
      isActive: boolean;
    }>;

    return items.map((c) => ({
      id: c.registrationId,
      userId: c.userId,
      userDisplayName: c.userDisplayName,
      clientType: c.clientType,
      clientVersion: c.clientVersion ?? '—',
      deviceIdentifier: c.deviceIdentifier ?? '—',
      lastAccessAt: c.lastAccessAt ?? '',
      isActive: c.isActive,
    }));
  }, [clientsData]);

  const capabilityPolicies: IdeCapabilityPolicy[] = useMemo(() => {
    const items = (policiesData?.items ?? []) as Array<{
      policyId: string;
      clientType: string;
      persona?: string | null;
      allowedCommands: string;
      allowedContextScopes: string;
      allowContractGeneration: boolean;
      allowIncidentTroubleshooting: boolean;
      allowExternalAI: boolean;
      maxTokensPerRequest: number;
      isActive: boolean;
    }>;

    return items.map((p) => ({
      id: p.policyId,
      clientType: p.clientType,
      persona: p.persona ?? null,
      allowedCommands: p.allowedCommands,
      allowedContextScopes: p.allowedContextScopes,
      allowContractGeneration: p.allowContractGeneration,
      allowIncidentTroubleshooting: p.allowIncidentTroubleshooting,
      allowExternalAI: p.allowExternalAI,
      maxTokensPerRequest: p.maxTokensPerRequest,
      isActive: p.isActive,
    }));
  }, [policiesData]);

  const filteredClients = clients.filter((c) => {
    if (clientFilter !== 'all' && c.clientType !== clientFilter) return false;
    if (search && !c.userDisplayName.toLowerCase().includes(search.toLowerCase()) && !c.userId.toLowerCase().includes(search.toLowerCase())) return false;
    return true;
  });

  const totalActive = summary?.totalActiveClients ?? clients.filter((c) => c.isActive).length;
  const vsCodeCount = clients.filter((c) => c.clientType === 'VsCode' && c.isActive).length;
  const vsCount = clients.filter((c) => c.clientType === 'VisualStudio' && c.isActive).length;
  const activePolicies = summary?.activePolicies ?? capabilityPolicies.filter((p) => p.isActive).length;

  const filters: { key: ClientFilter; label: string }[] = [
    { key: 'all', label: t('aiHub.ideFilterAll') },
    { key: 'VsCode', label: t('aiHub.ideFilterVsCode') },
    { key: 'VisualStudio', label: t('aiHub.ideFilterVisualStudio') },
  ];

  const clientIcon = (type: string) =>
    type === 'VsCode' ? <Code size={14} /> : <Monitor size={14} />;

  const isLoading = isLoadingSummary || isLoadingClients || isLoadingPolicies;
  const isError = isSummaryError || isClientsError || isPoliciesError;

  return (
    <PageContainer>
      <PageHeader
        title={t('aiHub.ideTitle')}
        subtitle={t('aiHub.ideSubtitle')}
      />

      {/* Stats */}
      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
        <StatCard title={t('aiHub.ideTotalClients')} value={totalActive} icon={<Plug size={20} />} color="text-accent" />
        <StatCard title={t('aiHub.ideVsCodeClients')} value={vsCodeCount} icon={<Code size={20} />} color="text-info" />
        <StatCard title={t('aiHub.ideVisualStudioClients')} value={vsCount} icon={<Monitor size={20} />} color="text-info" />
        <StatCard title={t('aiHub.ideActivePolicies')} value={activePolicies} icon={<ShieldCheck size={20} />} color="text-success" />
      </div>

      {isLoading && (
        <Card className="mb-8">
          <CardBody className="flex justify-center py-16">
            <Loader size="lg" />
          </CardBody>
        </Card>
      )}

      {isError && (
        <PageErrorState
          action={(
            <Button
              variant="secondary"
              size="sm"
              onClick={() => {
                refetchSummary();
                refetchClients();
                refetchPolicies();
              }}
            >
              {t('common.retry', 'Retry')}
            </Button>
          )}
        />
      )}

      {/* Client Types Overview */}
      {!isLoading && !isError && (
      <div className="mb-8">
        <h2 className="text-lg font-semibold text-heading mb-3">{t('aiHub.ideSupportedClients')}</h2>
        <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
          {/* VS Code */}
          <Card>
            <CardBody>
              <div className="flex items-center gap-3 mb-3">
                <div className="w-10 h-10 rounded-lg bg-accent/10 flex items-center justify-center">
                  <Code size={20} className="text-accent" />
                </div>
                <div>
                  <h3 className="text-sm font-semibold text-heading">{t('aiHub.ideVsCodeTitle')}</h3>
                  <p className="text-xs text-muted">{t('aiHub.ideVsCodeDescription')}</p>
                </div>
                <Badge variant="success" className="ml-auto">{t('aiHub.ideStatusReady')}</Badge>
              </div>
              <div className="flex flex-wrap gap-2 mb-2">
                <Badge variant="info">{t('aiHub.ideActiveClientsLabel')}: {vsCodeCount}</Badge>
                <Badge variant={summary?.clientTypes?.some((ct: { clientType: string; hasCapabilityPolicy: boolean }) => ct.clientType === 'VsCode' && ct.hasCapabilityPolicy) ? 'success' : 'default'}>
                  <Shield size={10} className="mr-1 inline" />{t('aiHub.idePolicyConfigured')}
                </Badge>
              </div>
              <div className="mt-2 flex flex-wrap gap-1">
                {(vsCodeCapabilities?.allowedCommands ?? ['Chat', 'ServiceLookup', 'ContractLookup', 'ContractGenerate', 'IncidentLookup']).slice(0, 5).map((cmd: string) => (
                  <span key={cmd} className="text-[10px] px-1.5 py-0.5 rounded bg-elevated text-muted">{cmd}</span>
                ))}
                {(vsCodeCapabilities?.allowedCommands?.length ?? 0) > 5 && (
                  <span className="text-[10px] text-faded">+{(vsCodeCapabilities.allowedCommands.length - 5)}</span>
                )}
              </div>
            </CardBody>
          </Card>

          {/* Visual Studio */}
          <Card>
            <CardBody>
              <div className="flex items-center gap-3 mb-3">
                <div className="w-10 h-10 rounded-lg bg-accent/10 flex items-center justify-center">
                  <Monitor size={20} className="text-accent" />
                </div>
                <div>
                  <h3 className="text-sm font-semibold text-heading">{t('aiHub.ideVisualStudioTitle')}</h3>
                  <p className="text-xs text-muted">{t('aiHub.ideVisualStudioDescription')}</p>
                </div>
                <Badge variant="success" className="ml-auto">{t('aiHub.ideStatusReady')}</Badge>
              </div>
              <div className="flex flex-wrap gap-2 mb-2">
                <Badge variant="info">{t('aiHub.ideActiveClientsLabel')}: {vsCount}</Badge>
                <Badge variant={summary?.clientTypes?.some((ct: { clientType: string; hasCapabilityPolicy: boolean }) => ct.clientType === 'VisualStudio' && ct.hasCapabilityPolicy) ? 'success' : 'default'}>
                  <Shield size={10} className="mr-1 inline" />{t('aiHub.idePolicyConfigured')}
                </Badge>
              </div>
              <div className="mt-2 flex flex-wrap gap-1">
                {(vsCapabilities?.allowedCommands ?? ['Chat', 'ServiceLookup', 'ContractLookup', 'ContractGenerate', 'IncidentLookup']).slice(0, 5).map((cmd: string) => (
                  <span key={cmd} className="text-[10px] px-1.5 py-0.5 rounded bg-elevated text-muted">{cmd}</span>
                ))}
                {(vsCapabilities?.allowedCommands?.length ?? 0) > 5 && (
                  <span className="text-[10px] text-faded">+{(vsCapabilities.allowedCommands.length - 5)}</span>
                )}
              </div>
            </CardBody>
          </Card>
        </div>
      </div>
      )}

      {/* Registered Clients */}
      {!isLoading && !isError && (
      <div className="mb-8">
        <h2 className="text-lg font-semibold text-heading mb-3">{t('aiHub.ideRegisteredClients')}</h2>
        <div className="flex flex-wrap items-center gap-3 mb-4">
          <div className="relative flex-1 min-w-[200px] max-w-xs">
            <Search size={16} className="absolute left-3 top-1/2 -translate-y-1/2 text-muted" />
            <input
              type="text"
              placeholder={t('aiHub.ideSearchClients')}
              value={search}
              onChange={(e) => setSearch(e.target.value)}
              className="w-full pl-9 pr-3 py-2 rounded-md bg-surface border border-edge text-body text-sm placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent"
            />
          </div>
          <div className="flex gap-1.5">
            {filters.map((f) => (
              <button
                key={f.key}
                onClick={() => setClientFilter(f.key)}
                className={`px-3 py-1.5 rounded-md text-xs font-medium transition-colors ${clientFilter === f.key ? 'bg-accent text-heading' : 'bg-elevated text-muted hover:text-body'}`}
              >
                {f.label}
              </button>
            ))}
          </div>
        </div>

        <div className="space-y-2">
          {filteredClients.map((c) => (
            <Card key={c.id}>
              <CardBody>
                <div className="flex items-center justify-between gap-4">
                  <div className="flex items-center gap-3 min-w-0">
                    <div className="w-8 h-8 rounded-full bg-accent/10 flex items-center justify-center shrink-0">
                      {clientIcon(c.clientType)}
                    </div>
                    <div className="min-w-0">
                      <div className="flex items-center gap-2">
                        <span className="text-sm font-medium text-heading truncate">{c.userDisplayName}</span>
                        <Badge variant={c.isActive ? 'success' : 'default'}>{c.isActive ? t('aiHub.statusActive') : t('aiHub.ideStatusRevoked')}</Badge>
                      </div>
                      <div className="flex items-center gap-2 text-xs text-muted">
                        <span>{c.clientType}</span>
                        <span>·</span>
                        <span>v{c.clientVersion}</span>
                        <span>·</span>
                        <Clock size={10} className="inline" />
                        <span>{timeAgo(c.lastAccessAt)}</span>
                      </div>
                    </div>
                  </div>
                  <div className="flex items-center gap-2">
                    <span className="text-xs text-muted">{c.deviceIdentifier}</span>
                  </div>
                </div>
              </CardBody>
            </Card>
          ))}
          {filteredClients.length === 0 && (
            <EmptyState title={t('aiHub.ideNoClientsFound')} size="compact" />
          )}
        </div>
      </div>
      )}

      {/* Capability Policies */}
      {!isLoading && !isError && (
        <>
          <div className="mb-8">
            <h2 className="text-lg font-semibold text-heading mb-3">{t('aiHub.ideCapabilityPolicies')}</h2>
            <div className="space-y-3">
              {capabilityPolicies.map((p) => (
                <Card key={p.id}>
                  <CardBody>
                    <div className="flex items-start justify-between gap-4">
                      <div className="min-w-0 flex-1">
                        <div className="flex items-center gap-2 mb-1">
                          <span className="text-sm font-semibold text-heading">{p.clientType}</span>
                          {p.persona && <Badge variant="info">{p.persona}</Badge>}
                          {!p.persona && <Badge variant="default">{t('aiHub.ideAllPersonas')}</Badge>}
                          <Badge variant={p.isActive ? 'success' : 'default'}>{p.isActive ? t('aiHub.statusActive') : t('aiHub.ideStatusInactive')}</Badge>
                        </div>
                        <div className="flex flex-wrap gap-2 mb-2">
                          {p.allowContractGeneration && (
                            <Badge variant="info"><FileText size={10} className="mr-1 inline" />{t('aiHub.ideContractGen')}</Badge>
                          )}
                          {p.allowIncidentTroubleshooting && (
                            <Badge variant="info"><AlertTriangle size={10} className="mr-1 inline" />{t('aiHub.ideTroubleshooting')}</Badge>
                          )}
                          {p.allowExternalAI ? (
                            <Badge variant="warning"><Zap size={10} className="mr-1 inline" />{t('aiHub.ideExternalAiAllowed')}</Badge>
                          ) : (
                            <Badge variant="default"><Shield size={10} className="mr-1 inline" />{t('aiHub.ideInternalOnly')}</Badge>
                          )}
                          <span className="text-xs text-muted">{t('aiHub.ideMaxTokens')}: {p.maxTokensPerRequest.toLocaleString()}</span>
                        </div>
                        <div className="flex flex-wrap gap-1">
                          {p.allowedCommands.split(',').slice(0, 5).map((cmd) => (
                            <span key={cmd} className="text-[10px] px-1.5 py-0.5 rounded bg-elevated text-muted">{cmd}</span>
                          ))}
                          {p.allowedCommands.split(',').length > 5 && (
                            <span className="text-[10px] text-faded">+{p.allowedCommands.split(',').length - 5}</span>
                          )}
                        </div>
                      </div>
                    </div>
                  </CardBody>
                </Card>
              ))}
            </div>
          </div>

          {/* Query Sessions Audit */}
          <div className="mb-8">
            <h2 className="text-lg font-semibold text-heading mb-3">
              <Activity size={18} className="inline mr-2 text-accent" />
              {t('aiHub.ideQuerySessions')}
            </h2>
            <p className="text-xs text-muted mb-4">{t('aiHub.ideQuerySessionsSubtitle')}</p>
            {querySessions.length === 0 ? (
              <EmptyState icon={<FileText size={32} />} title="Nenhuma sessão" description={t('aiHub.ideQuerySessionsNoData')} />
            ) : (
              <Card>
                <CardBody className="p-0 overflow-x-auto">
                  <table className="w-full text-xs">
                    <thead>
                      <tr className="border-b border-edge text-left">
                        <th className="px-4 py-2 text-muted font-medium">{t('aiHub.ideQueryUser')}</th>
                        <th className="px-4 py-2 text-muted font-medium">{t('aiHub.ideQueryClient')}</th>
                        <th className="px-4 py-2 text-muted font-medium">{t('aiHub.ideQueryType')}</th>
                        <th className="px-4 py-2 text-muted font-medium">{t('aiHub.ideQueryModel')}</th>
                        <th className="px-4 py-2 text-muted font-medium">{t('aiHub.ideQueryTokens')}</th>
                        <th className="px-4 py-2 text-muted font-medium">{t('aiHub.ideQueryResponseTime')}</th>
                        <th className="px-4 py-2 text-muted font-medium">Status</th>
                        <th className="px-4 py-2 text-muted font-medium">{t('aiHub.ideQueryTime')}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {querySessions.map((s) => (
                        <tr key={s.sessionId} className="border-b border-edge last:border-0 hover:bg-elevated/40">
                          <td className="px-4 py-2 text-heading">{s.userId}</td>
                          <td className="px-4 py-2 text-muted">
                            {s.ideClient === 'vscode' ? <Code size={12} className="inline mr-1" /> : <Monitor size={12} className="inline mr-1" />}
                            {s.ideClient}
                          </td>
                          <td className="px-4 py-2 text-muted">{s.queryType}</td>
                          <td className="px-4 py-2 text-muted">{s.modelUsed}</td>
                          <td className="px-4 py-2 text-muted">{s.tokensUsed > 0 ? s.tokensUsed.toLocaleString() : '—'}</td>
                          <td className="px-4 py-2 text-muted">
                            {s.responseTimeMs != null ? `${s.responseTimeMs}ms` : '—'}
                          </td>
                          <td className="px-4 py-2">
                            {s.status === 'Responded' && <Badge variant="success">{t('aiHub.ideQueryStatusResponded')}</Badge>}
                            {s.status === 'Processing' && <Badge variant="info">{t('aiHub.ideQueryStatusProcessing')}</Badge>}
                            {s.status === 'Blocked' && <Badge variant="warning">{t('aiHub.ideQueryStatusBlocked')}</Badge>}
                            {s.status === 'Failed' && <Badge variant="danger">{t('aiHub.ideQueryStatusFailed')}</Badge>}
                          </td>
                          <td className="px-4 py-2 text-muted">
                            <Clock size={10} className="inline mr-1" />{timeAgo(s.submittedAt)}
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </CardBody>
              </Card>
            )}
          </div>

          {/* Governance Notice */}
          <div className="p-4 rounded-lg bg-elevated border border-edge">
            <div className="flex items-start gap-3">
              <ShieldCheck size={18} className="text-accent mt-0.5 shrink-0" />
              <p className="text-xs text-muted">{t('aiHub.ideGovernanceNotice')}</p>
            </div>
          </div>
        </>
      )}
    </PageContainer>
  );
}
