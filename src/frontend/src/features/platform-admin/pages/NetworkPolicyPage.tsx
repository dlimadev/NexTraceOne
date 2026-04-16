import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  ShieldCheck,
  ShieldAlert,
  ShieldOff,
  CheckCircle,
  XCircle,
  Globe,
  AlertTriangle,
} from 'lucide-react';
import { platformAdminApi, type NetworkIsolationMode } from '../api/platformAdmin';

export function NetworkPolicyPage() {
  const { t } = useTranslation('networkPolicy');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['network-policy'],
    queryFn: platformAdminApi.getNetworkPolicy,
  });

  return (
    <div className="p-6 space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div>
          <h1 className="text-2xl font-semibold text-slate-900">{t('title')}</h1>
          <p className="mt-1 text-sm text-slate-500">{t('subtitle')}</p>
        </div>
        <button
          onClick={() => refetch()}
          className="px-4 py-2 text-sm bg-indigo-600 text-white rounded-lg hover:bg-indigo-700 transition-colors"
        >
          {t('refresh')}
        </button>
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

      {data && (
        <>
          {/* Mode Banner */}
          <ModeBanner mode={data.mode} t={t} />

          {/* Stats */}
          <div className="grid grid-cols-3 gap-4">
            <StatCard
              label={t('currentMode')}
              value={data.mode}
              color={data.mode === 'AirGap' ? 'red' : data.mode === 'Restricted' ? 'amber' : 'slate'}
            />
            <StatCard
              label={t('activeCalls')}
              value={String(data.activeCalls)}
              color={data.activeCalls > 0 && data.mode === 'AirGap' ? 'red' : 'emerald'}
            />
            <StatCard
              label={t('blockedCalls')}
              value={String(data.blockedCalls)}
              color={data.blockedCalls > 0 ? 'amber' : 'slate'}
            />
          </div>

          {/* External Calls Table */}
          <section>
            <h2 className="text-base font-medium text-slate-800 mb-3">{t('externalCallsTitle')}</h2>
            <div className="bg-white border border-slate-200 rounded-lg overflow-hidden">
              <table className="w-full text-sm">
                <thead className="bg-slate-50 border-b border-slate-200">
                  <tr>
                    <th className="text-left px-4 py-3 font-medium text-slate-600">{t('colCall')}</th>
                    <th className="text-left px-4 py-3 font-medium text-slate-600">{t('colDescription')}</th>
                    <th className="text-left px-4 py-3 font-medium text-slate-600">{t('colEnvVar')}</th>
                    <th className="text-center px-4 py-3 font-medium text-slate-600">{t('colConfigured')}</th>
                    <th className="text-center px-4 py-3 font-medium text-slate-600">{t('colBlocked')}</th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-slate-100">
                  {data.calls.map((call) => (
                    <tr key={call.key} className={call.blocked ? 'bg-slate-50 opacity-70' : ''}>
                      <td className="px-4 py-3 font-mono text-xs text-slate-700">{call.key}</td>
                      <td className="px-4 py-3 text-xs text-slate-600">{call.description}</td>
                      <td className="px-4 py-3 font-mono text-xs text-slate-400">{call.envVar}</td>
                      <td className="px-4 py-3 text-center">
                        {call.configured ? (
                          <CheckCircle size={15} className="text-emerald-500 mx-auto" />
                        ) : (
                          <XCircle size={15} className="text-slate-300 mx-auto" />
                        )}
                      </td>
                      <td className="px-4 py-3 text-center">
                        {call.blocked ? (
                          <ShieldAlert size={15} className="text-red-400 mx-auto" />
                        ) : (
                          <Globe size={15} className="text-slate-400 mx-auto" />
                        )}
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          </section>

          {/* Config hint */}
          <div className="bg-slate-50 border border-slate-200 rounded-lg p-4 text-xs text-slate-500 space-y-1">
            <p className="font-medium text-slate-600">{t('configHintTitle')}</p>
            <p>{t('configHintBody')}</p>
            <code className="block bg-white border border-slate-200 rounded px-3 py-2 font-mono text-xs mt-2">
              Platform__NetworkIsolation__Mode=AirGap
            </code>
          </div>

          <p className="text-xs text-slate-400">
            {t('auditedAt')}: {new Date(data.auditedAt).toLocaleString()}
          </p>
        </>
      )}
    </div>
  );
}

function ModeBanner({ mode, t }: { mode: NetworkIsolationMode; t: (key: string) => string }) {
  const config = {
    AirGap: {
      bg: 'bg-red-50 border-red-200',
      text: 'text-red-800',
      icon: <ShieldAlert size={20} className="text-red-500" />,
      key: 'modeAirGap',
    },
    Restricted: {
      bg: 'bg-amber-50 border-amber-200',
      text: 'text-amber-800',
      icon: <ShieldCheck size={20} className="text-amber-500" />,
      key: 'modeRestricted',
    },
    Off: {
      bg: 'bg-slate-50 border-slate-200',
      text: 'text-slate-700',
      icon: <ShieldOff size={20} className="text-slate-400" />,
      key: 'modeOff',
    },
  }[mode];

  return (
    <div className={`flex items-start gap-3 p-4 border rounded-lg ${config.bg}`}>
      {config.icon}
      <div>
        <p className={`font-semibold ${config.text}`}>{t(`${config.key}Title`)}</p>
        <p className={`text-sm mt-0.5 ${config.text}`}>{t(`${config.key}Desc`)}</p>
      </div>
      {mode === 'AirGap' && (
        <span className="ml-auto flex items-center gap-1 text-xs text-red-700 bg-red-100 px-2 py-1 rounded-full font-medium">
          <AlertTriangle size={11} />
          {t('activeLabel')}
        </span>
      )}
    </div>
  );
}

function StatCard({
  label,
  value,
  color,
}: {
  label: string;
  value: string;
  color: 'red' | 'amber' | 'emerald' | 'slate';
}) {
  const colors = {
    red: 'text-red-600',
    amber: 'text-amber-600',
    emerald: 'text-emerald-600',
    slate: 'text-slate-700',
  };
  return (
    <div className="bg-white border border-slate-200 rounded-lg p-4">
      <p className="text-xs text-slate-500 uppercase tracking-wide">{label}</p>
      <p className={`text-2xl font-bold mt-1 ${colors[color]}`}>{value}</p>
    </div>
  );
}
