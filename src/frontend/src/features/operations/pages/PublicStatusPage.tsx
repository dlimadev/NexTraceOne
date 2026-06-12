import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { AlertTriangle, CheckCircle2, Clock } from 'lucide-react';
import client from '../../../api/client';

interface PublicStatusIncident {
  reference: string;
  title: string;
  severity: string;
  status: string;
  serviceName: string;
  createdAt: string;
}

interface PublicStatusResponse {
  overallStatus: 'Operational' | 'DegradedPerformance' | 'PartialOutage' | 'MajorOutage';
  generatedAt: string;
  activeIncidents: PublicStatusIncident[];
}

const STATUS_STYLES: Record<PublicStatusResponse['overallStatus'], { bg: string; text: string }> = {
  Operational: { bg: 'bg-success/10 border-success/30', text: 'text-success' },
  DegradedPerformance: { bg: 'bg-warning/10 border-warning/30', text: 'text-warning' },
  PartialOutage: { bg: 'bg-warning/10 border-warning/30', text: 'text-warning' },
  MajorOutage: { bg: 'bg-critical/10 border-critical/30', text: 'text-critical' },
};

/**
 * Status page pública — acessível sem autenticação via /status/:tenantId.
 * Mostra o status geral do tenant e os incidentes abertos com dados mínimos.
 */
export function PublicStatusPage() {
  const { t } = useTranslation();
  const { tenantId } = useParams<{ tenantId: string }>();

  const { data, isLoading, isError } = useQuery({
    queryKey: ['public-status', tenantId],
    queryFn: () =>
      client.get<PublicStatusResponse>(`/status/public/${tenantId}`).then((r) => r.data),
    enabled: Boolean(tenantId),
    refetchInterval: 60_000,
  });

  const overall = data?.overallStatus ?? 'Operational';
  const style = STATUS_STYLES[overall];

  return (
    <div className="min-h-screen bg-page py-12 px-4">
      <div className="max-w-3xl mx-auto space-y-6">
        <div className="text-center mb-8">
          <h1 className="text-2xl font-bold text-heading">NexTraceOne</h1>
          <p className="text-sm text-muted">{t('publicStatus.subtitle', 'Service status')}</p>
        </div>

        {isLoading && (
          <div className="text-center text-sm text-muted py-16">
            {t('common.loading', 'Loading…')}
          </div>
        )}

        {isError && (
          <div className="text-center text-sm text-critical py-16">
            {t('publicStatus.loadError', 'Unable to load status. Please try again later.')}
          </div>
        )}

        {data && (
          <>
            <div className={`flex items-center gap-3 rounded-xl border px-6 py-5 ${style.bg}`}>
              {overall === 'Operational' ? (
                <CheckCircle2 size={24} className={style.text} />
              ) : (
                <AlertTriangle size={24} className={style.text} />
              )}
              <div>
                <div className={`text-lg font-semibold ${style.text}`}>
                  {t(`publicStatus.overall.${overall}`, overall)}
                </div>
                <div className="text-xs text-muted">
                  {t('publicStatus.updatedAt', 'Updated at {{time}}', {
                    time: new Date(data.generatedAt).toLocaleTimeString(),
                  })}
                </div>
              </div>
            </div>

            <div className="rounded-xl border border-edge bg-card">
              <div className="px-6 py-4 border-b border-edge">
                <h2 className="text-sm font-semibold text-heading">
                  {t('publicStatus.activeIncidents', 'Active incidents')}
                </h2>
              </div>
              {data.activeIncidents.length === 0 ? (
                <div className="px-6 py-10 text-center text-sm text-muted">
                  {t('publicStatus.noIncidents', 'No active incidents. All systems operational.')}
                </div>
              ) : (
                <ul className="divide-y divide-edge">
                  {data.activeIncidents.map((incident) => (
                    <li key={incident.reference} className="px-6 py-4 flex items-start gap-3">
                      <AlertTriangle
                        size={16}
                        className={incident.severity === 'Critical' ? 'text-critical mt-0.5' : 'text-warning mt-0.5'}
                      />
                      <div className="min-w-0 flex-1">
                        <div className="flex items-center gap-2 text-xs text-muted">
                          <span className="font-mono">{incident.reference}</span>
                          <span>{incident.severity}</span>
                          <span>•</span>
                          <span>{incident.serviceName}</span>
                        </div>
                        <p className="text-sm text-body truncate">{incident.title}</p>
                      </div>
                      <span className="flex items-center gap-1 text-xs text-muted shrink-0">
                        <Clock size={12} />
                        {new Date(incident.createdAt).toLocaleString()}
                      </span>
                    </li>
                  ))}
                </ul>
              )}
            </div>
          </>
        )}
      </div>
    </div>
  );
}
