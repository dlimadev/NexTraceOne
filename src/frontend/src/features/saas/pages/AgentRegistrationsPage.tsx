import { useMemo, useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  Server,
  RefreshCw,
  Activity,
  Cpu,
  MemoryStick,
  Clock,
  CheckCircle2,
  XCircle,
  AlertCircle,
} from 'lucide-react';
import { saasApi, type AgentRegistrationDto, type AgentStatus } from '../api/saasApi';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { Card, CardBody } from '../../../components/Card';
import { EmptyState } from '../../../components/EmptyState';

const STATUS_ICON: Record<AgentStatus, React.ReactNode> = {
  Active: <CheckCircle2 size={14} className="text-success" />,
  Inactive: <XCircle size={14} className="text-faded" />,
  Decommissioned: <AlertCircle size={14} className="text-critical/60" />,
};

const STATUS_COLOR: Record<AgentStatus, string> = {
  Active: 'bg-success/10 text-success',
  Inactive: 'bg-elevated text-muted',
  Decommissioned: 'bg-critical/10 text-critical',
};

function formatHeartbeat(lastHeartbeatAt: string | null): string {
  if (!lastHeartbeatAt) return '—';
  const diff = Date.now() - new Date(lastHeartbeatAt).getTime();
  const minutes = Math.floor(diff / 60_000);
  if (minutes < 1) return '< 1 min';
  if (minutes < 60) return `${minutes} min`;
  const hours = Math.floor(minutes / 60);
  if (hours < 24) return `${hours}h`;
  return `${Math.floor(hours / 24)}d`;
}

function AgentRow({ agent, now }: { agent: AgentRegistrationDto; now: number }) {
  const heartbeatAge = useMemo(() => agent.lastHeartbeatAt
    ? now - new Date(agent.lastHeartbeatAt).getTime()
    : Infinity, [agent.lastHeartbeatAt, now]);
  const isStale = heartbeatAge > 5 * 60_000;

  return (
    <tr className="hover:bg-elevated transition-colors">
      <td className="px-4 py-3">
        <div className="flex items-center gap-2">
          <Server size={15} className="text-faded shrink-0" />
          <div>
            <div className="text-sm font-medium text-heading">{agent.hostname}</div>
            <div className="text-xs text-faded font-mono">{agent.hostUnitId.slice(0, 8)}…</div>
          </div>
        </div>
      </td>
      <td className="px-4 py-3">
        <span className={`inline-flex items-center gap-1.5 text-xs px-2 py-1 rounded-full font-medium ${STATUS_COLOR[agent.status]}`}>
          {STATUS_ICON[agent.status]}
          {agent.status}
        </span>
      </td>
      <td className="px-4 py-3 text-sm text-body">
        <div className="flex items-center gap-1.5">
          <Cpu size={13} className="text-faded" />
          {agent.cpuCores}c
        </div>
      </td>
      <td className="px-4 py-3 text-sm text-body">
        <div className="flex items-center gap-1.5">
          <MemoryStick size={13} className="text-faded" />
          {agent.ramGb}GB
        </div>
      </td>
      <td className="px-4 py-3">
        <span className="inline-flex items-center gap-1 text-sm font-semibold text-accent bg-accent/10 px-2 py-0.5 rounded-full">
          <Activity size={12} />
          {agent.hostUnits.toFixed(1)} HU
        </span>
      </td>
      <td className="px-4 py-3">
        <div className={`flex items-center gap-1.5 text-sm ${isStale ? 'text-warning' : 'text-muted'}`}>
          <Clock size={13} />
          {formatHeartbeat(agent.lastHeartbeatAt)}
        </div>
      </td>
      <td className="px-4 py-3 text-xs text-faded">{agent.agentVersion}</td>
    </tr>
  );
}

export function AgentRegistrationsPage() {
  const { t } = useTranslation('agentRegistrations');
  const [now, setNow] = useState(() => Date.now()); // Inicialização lazy é aceitável

  // Atualizar timestamp a cada minuto
  useEffect(() => {
    const interval = setInterval(() => setNow(Date.now()), 60_000);
    return () => clearInterval(interval);
  }, []);

  const { data, isLoading, isError, refetch, isFetching } = useQuery({
    queryKey: ['saas-agents'],
    queryFn: saasApi.listAgents,
    refetchInterval: 30_000,
  });

  const agents = data?.items ?? [];

  return (
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          icon={<Server size={20} />}
          actions={
            <Button
              variant="ghost"
              onClick={() => refetch()}
              disabled={isFetching}
              className="flex items-center gap-2"
              size="sm"
            >
              <RefreshCw size={14} className={isFetching ? 'animate-spin' : ''} />
              {t('refresh')}
            </Button>
          }
        />

        {isError && (
          <div className="bg-critical/10 border border-critical/20 text-critical rounded-lg p-4 text-sm">
            {t('loadError')}
          </div>
        )}

        {/* Summary */}
        {data && (
          <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
            <Card>
              <CardBody>
                <div className="text-sm text-muted mb-1">{t('totalAgents')}</div>
                <div className="text-2xl font-bold text-heading">{agents.length}</div>
              </CardBody>
            </Card>
            <Card>
              <CardBody>
                <div className="text-sm text-muted mb-1">{t('activeAgents')}</div>
                <div className="text-2xl font-bold text-success">{data.activeCount}</div>
              </CardBody>
            </Card>
            <Card>
              <CardBody>
                <div className="text-sm text-muted mb-1">{t('totalHostUnits')}</div>
                <div className="text-2xl font-bold text-accent">{data.totalHostUnits.toFixed(1)} HU</div>
              </CardBody>
            </Card>
          </div>
        )}

        {/* Table */}
        <div className="bg-card border border-edge rounded-md overflow-hidden">
          {isLoading ? (
            <div className="p-8 text-center text-faded text-sm">{t('loading')}</div>
          ) : agents.length === 0 && !isLoading ? (
            <EmptyState
              icon={<Server size={24} />}
              title={t('noAgents', 'No agents registered')}
              description={t('noAgentsDescription', 'No agent registrations found. Agents will appear here once they connect.')}
            />
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full">
                <thead className="bg-elevated border-b border-edge">
                  <tr>
                    {['hostname', 'status', 'cpu', 'ram', 'hostUnits', 'lastHeartbeat', 'version'].map((col) => (
                      <th key={col} className="px-4 py-3 text-left text-xs font-medium text-muted uppercase tracking-wider">
                        {t(`col.${col}`)}
                      </th>
                    ))}
                  </tr>
                </thead>
                <tbody className="divide-y divide-edge/50">
                  {agents.map((agent) => (
                    <AgentRow key={agent.id} agent={agent} now={now} />
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>
    </PageContainer>
  );
}
