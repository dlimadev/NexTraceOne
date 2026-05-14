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
  CheckCircle,
  XCircle,
  AlertCircle,
} from 'lucide-react';
import { saasApi, type AgentRegistrationDto, type AgentStatus } from '../api/saasApi';

const STATUS_ICON: Record<AgentStatus, React.ReactNode> = {
  Active: <CheckCircle size={14} className="text-green-500" />,
  Inactive: <XCircle size={14} className="text-slate-400" />,
  Decommissioned: <AlertCircle size={14} className="text-red-400" />,
};

const STATUS_COLOR: Record<AgentStatus, string> = {
  Active: 'bg-green-100 text-green-700',
  Inactive: 'bg-slate-100 text-slate-600',
  Decommissioned: 'bg-red-100 text-red-600',
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
    <tr className="hover:bg-slate-50 transition-colors">
      <td className="px-4 py-3">
        <div className="flex items-center gap-2">
          <Server size={15} className="text-slate-400 shrink-0" />
          <div>
            <div className="text-sm font-medium text-slate-800">{agent.hostname}</div>
            <div className="text-xs text-slate-400 font-mono">{agent.hostUnitId.slice(0, 8)}…</div>
          </div>
        </div>
      </td>
      <td className="px-4 py-3">
        <span className={`inline-flex items-center gap-1.5 text-xs px-2 py-1 rounded-full font-medium ${STATUS_COLOR[agent.status]}`}>
          {STATUS_ICON[agent.status]}
          {agent.status}
        </span>
      </td>
      <td className="px-4 py-3 text-sm text-slate-700">
        <div className="flex items-center gap-1.5">
          <Cpu size={13} className="text-slate-400" />
          {agent.cpuCores}c
        </div>
      </td>
      <td className="px-4 py-3 text-sm text-slate-700">
        <div className="flex items-center gap-1.5">
          <MemoryStick size={13} className="text-slate-400" />
          {agent.ramGb}GB
        </div>
      </td>
      <td className="px-4 py-3">
        <span className="inline-flex items-center gap-1 text-sm font-semibold text-violet-700 bg-violet-50 px-2 py-0.5 rounded-full">
          <Activity size={12} />
          {agent.hostUnits.toFixed(1)} HU
        </span>
      </td>
      <td className="px-4 py-3">
        <div className={`flex items-center gap-1.5 text-sm ${isStale ? 'text-amber-600' : 'text-slate-600'}`}>
          <Clock size={13} />
          {formatHeartbeat(agent.lastHeartbeatAt)}
        </div>
      </td>
      <td className="px-4 py-3 text-xs text-slate-400">{agent.agentVersion}</td>
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
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
          <p className="text-sm text-slate-500 mt-1">{t('subtitle')}</p>
        </div>
        <button
          onClick={() => refetch()}
          disabled={isFetching}
          className="flex items-center gap-2 text-sm text-slate-600 hover:text-slate-800 border border-slate-200 rounded-lg px-3 py-2 transition-colors"
        >
          <RefreshCw size={14} className={isFetching ? 'animate-spin' : ''} />
          {t('refresh')}
        </button>
      </div>

      {isError && (
        <div className="bg-red-50 border border-red-200 text-red-700 rounded-lg p-4 text-sm">
          {t('loadError')}
        </div>
      )}

      {/* Summary */}
      {data && (
        <div className="grid grid-cols-1 md:grid-cols-3 gap-4">
          <div className="bg-white border border-slate-200 rounded-xl p-5">
            <div className="text-sm text-slate-500 mb-1">{t('totalAgents')}</div>
            <div className="text-2xl font-bold text-slate-900">{agents.length}</div>
          </div>
          <div className="bg-white border border-slate-200 rounded-xl p-5">
            <div className="text-sm text-slate-500 mb-1">{t('activeAgents')}</div>
            <div className="text-2xl font-bold text-green-700">{data.activeCount}</div>
          </div>
          <div className="bg-white border border-slate-200 rounded-xl p-5">
            <div className="text-sm text-slate-500 mb-1">{t('totalHostUnits')}</div>
            <div className="text-2xl font-bold text-violet-700">{data.totalHostUnits.toFixed(1)} HU</div>
          </div>
        </div>
      )}

      {/* Table */}
      <div className="bg-white border border-slate-200 rounded-xl overflow-hidden">
        {isLoading ? (
          <div className="p-8 text-center text-slate-400 text-sm">{t('loading')}</div>
        ) : agents.length === 0 ? (
          <div className="p-12 text-center">
            <Server size={40} className="mx-auto text-slate-300 mb-3" />
            <p className="text-slate-500 text-sm">{t('noAgents')}</p>
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full">
              <thead className="bg-slate-50 border-b border-slate-200">
                <tr>
                  {['hostname', 'status', 'cpu', 'ram', 'hostUnits', 'lastHeartbeat', 'version'].map((col) => (
                    <th key={col} className="px-4 py-3 text-left text-xs font-medium text-slate-500 uppercase tracking-wider">
                      {t(`col.${col}`)}
                    </th>
                  ))}
                </tr>
              </thead>
              <tbody className="divide-y divide-slate-100">
                {agents.map((agent) => (
                  <AgentRow key={agent.id} agent={agent} now={now} />
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}
