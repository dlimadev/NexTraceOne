import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import {
  ShieldCheck,
  ShieldAlert,
  ShieldOff,
  CheckCircle2,
  XCircle,
  Globe,
  AlertTriangle,
  RefreshCw,
} from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { platformAdminApi, type NetworkIsolationMode } from '../api/platformAdmin';

export function NetworkPolicyPage() {
  const { t } = useTranslation('networkPolicy');

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['network-policy'],
    queryFn: platformAdminApi.getNetworkPolicy,
  });

  return (
    <PageContainer>
      <div className="space-y-6">
        <PageHeader
          title={t('title')}
          subtitle={t('subtitle')}
          actions={
            <Button variant="primary" onClick={() => refetch()}>
              <RefreshCw size={14} />
              {t('refresh')}
            </Button>
          }
        />

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

        {data && (
          <>
            {/* Mode Banner */}
            <ModeBanner mode={data.mode} t={t} />

            {/* Stats */}
            <div className="grid grid-cols-3 gap-4">
              <StatCard
                label={t('currentMode')}
                value={data.mode}
                color={data.mode === 'AirGap' ? 'critical' : data.mode === 'Restricted' ? 'warning' : 'body'}
              />
              <StatCard
                label={t('activeCalls')}
                value={String(data.activeCalls)}
                color={data.activeCalls > 0 && data.mode === 'AirGap' ? 'critical' : 'success'}
              />
              <StatCard
                label={t('blockedCalls')}
                value={String(data.blockedCalls)}
                color={data.blockedCalls > 0 ? 'warning' : 'body'}
              />
            </div>

            {/* External Calls Table */}
            <section>
              <h2 className="text-base font-medium text-heading mb-3">{t('externalCallsTitle')}</h2>
              <div className="bg-card border border-edge rounded-lg overflow-hidden">
                <table className="w-full text-sm">
                  <thead className="bg-elevated border-b border-edge">
                    <tr>
                      <th className="text-left px-4 py-3 font-medium text-muted">{t('colCall')}</th>
                      <th className="text-left px-4 py-3 font-medium text-muted">{t('colDescription')}</th>
                      <th className="text-left px-4 py-3 font-medium text-muted">{t('colEnvVar')}</th>
                      <th className="text-center px-4 py-3 font-medium text-muted">{t('colConfigured')}</th>
                      <th className="text-center px-4 py-3 font-medium text-muted">{t('colBlocked')}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-edge/50">
                    {data.calls.map((call) => (
                      <tr key={call.key} className={call.blocked ? 'bg-elevated opacity-70' : 'hover:bg-elevated'}>
                        <td className="px-4 py-3 font-mono text-xs text-body">{call.key}</td>
                        <td className="px-4 py-3 text-xs text-muted">{call.description}</td>
                        <td className="px-4 py-3 font-mono text-xs text-faded">{call.envVar}</td>
                        <td className="px-4 py-3 text-center">
                          {call.configured ? (
                            <CheckCircle2 size={15} className="text-success mx-auto" />
                          ) : (
                            <XCircle size={15} className="text-faded mx-auto" />
                          )}
                        </td>
                        <td className="px-4 py-3 text-center">
                          {call.blocked ? (
                            <ShieldAlert size={15} className="text-critical mx-auto" />
                          ) : (
                            <Globe size={15} className="text-faded mx-auto" />
                          )}
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>

            {/* Config hint */}
            <div className="bg-elevated border border-edge rounded-lg p-4 text-xs text-muted space-y-1">
              <p className="font-medium text-body">{t('configHintTitle')}</p>
              <p>{t('configHintBody')}</p>
              <code className="block bg-card border border-edge rounded px-3 py-2 font-mono text-xs mt-2 text-body">
                Platform__NetworkIsolation__Mode=AirGap
              </code>
            </div>

            <p className="text-xs text-faded">
              {t('auditedAt')}: {new Date(data.auditedAt).toLocaleString()}
            </p>
          </>
        )}
      </div>
    </PageContainer>
  );
}

function ModeBanner({ mode, t }: { mode: NetworkIsolationMode; t: (key: string) => string }) {
  const config = {
    AirGap: {
      bg: 'bg-critical/10 border-critical/20',
      text: 'text-critical',
      icon: <ShieldAlert size={20} className="text-critical" />,
      key: 'modeAirGap',
    },
    Restricted: {
      bg: 'bg-warning/10 border-warning/20',
      text: 'text-warning',
      icon: <ShieldCheck size={20} className="text-warning" />,
      key: 'modeRestricted',
    },
    Off: {
      bg: 'bg-elevated border-edge',
      text: 'text-body',
      icon: <ShieldOff size={20} className="text-muted" />,
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
        <span className="ml-auto flex items-center gap-1 text-xs text-critical bg-critical/10 px-2 py-1 rounded-full font-medium">
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
  color: 'critical' | 'warning' | 'success' | 'body';
}) {
  const colors = {
    critical: 'text-critical',
    warning: 'text-warning',
    success: 'text-success',
    body: 'text-body',
  };
  return (
    <div className="bg-card border border-edge rounded-lg p-4">
      <p className="text-xs text-muted uppercase tracking-wide">{label}</p>
      <p className={`text-2xl font-bold mt-1 ${colors[color]}`}>{value}</p>
    </div>
  );
}
