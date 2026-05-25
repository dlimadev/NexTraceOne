/**
 * ServiceIncidentsTab — Histórico e incidentes ativos do serviço.
 *
 * Exibe incidentes recentes que afetaram este serviço, com acesso rápido
 * ao explorador de incidentes já filtrado.
 *
 * @pillar Service 360° — Contexto operacional do serviço
 */
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { AlertTriangle, ExternalLink, Clock, CheckCircle, XCircle } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Link } from 'react-router-dom';
import { incidentsApi, type IncidentListItem } from '../../operations/api/incidents';

interface Props {
  serviceId: string;
}

function severityBadge(severity: string) {
  switch (severity?.toLowerCase()) {
    case 'critical': case 'p1': return <Badge variant="danger" size="sm">P1 Critical</Badge>;
    case 'high': case 'p2': return <Badge variant="warning" size="sm">P2 High</Badge>;
    case 'medium': case 'p3': return <Badge variant="info" size="sm">P3 Medium</Badge>;
    default: return <Badge variant="default" size="sm">{severity}</Badge>;
  }
}

function statusIcon(status: string) {
  switch (status?.toLowerCase()) {
    case 'resolved': return <CheckCircle size={13} className="text-success" />;
    case 'open': case 'active': return <AlertTriangle size={13} className="text-critical" />;
    default: return <Clock size={13} className="text-warning" />;
  }
}

function formatRelativeTime(isoDate: string): string {
  const diff = Date.now() - new Date(isoDate).getTime();
  const hours = Math.floor(diff / 3_600_000);
  if (hours < 1) return `${Math.floor(diff / 60_000)}m ago`;
  if (hours < 24) return `${hours}h ago`;
  return `${Math.floor(hours / 24)}d ago`;
}

export function ServiceIncidentsTab({ serviceId }: Props) {
  const { t } = useTranslation();

  const { data, isLoading } = useQuery({
    queryKey: ['service-incidents', serviceId],
    queryFn: () => incidentsApi.listIncidentsByService(serviceId, 1, 10),
    staleTime: 30_000,
  });

  const incidents: IncidentListItem[] = data?.items ?? [];
  const openCount = incidents.filter(i => i.status?.toLowerCase() !== 'resolved').length;

  return (
    <div className="flex flex-col gap-4">
      {/* ── Header banner ── */}
      {openCount > 0 && (
        <div className="flex items-center gap-2 px-4 py-3 rounded-lg bg-critical/10 border border-critical/30 text-critical text-sm font-medium">
          <AlertTriangle size={15} />
          {t('serviceDetail.incidents.openBanner', {
            count: openCount,
            defaultValue: `${openCount} open incident(s) affecting this service`,
          })}
        </div>
      )}

      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <AlertTriangle size={16} className="text-accent" />
              <h3 className="text-sm font-semibold text-heading">
                {t('serviceDetail.incidents.title', 'Recent Incidents')}
              </h3>
              {incidents.length > 0 && (
                <span className="text-xs text-muted">({incidents.length})</span>
              )}
            </div>
            <Link
              to={`/operations/incidents?serviceId=${serviceId}`}
              className="flex items-center gap-1 text-xs text-accent hover:underline"
            >
              <ExternalLink size={11} />
              {t('serviceDetail.incidents.viewAll', 'View All')}
            </Link>
          </div>
        </CardHeader>
        <CardBody className="p-0">
          {isLoading ? (
            <div className="py-8 text-center">
              <AlertTriangle size={20} className="text-muted animate-pulse mx-auto" />
            </div>
          ) : incidents.length === 0 ? (
            <div className="py-10 text-center px-4">
              <CheckCircle size={20} className="text-success mx-auto mb-2" />
              <p className="text-sm text-muted">
                {t('serviceDetail.incidents.noIncidents', 'No recent incidents. Great job! 🎉')}
              </p>
            </div>
          ) : (
            <table className="w-full text-sm">
              <thead className="border-b border-edge">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase tracking-wider">
                    {t('serviceDetail.incidents.incident', 'Incident')}
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase tracking-wider">
                    {t('serviceDetail.incidents.severity', 'Severity')}
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase tracking-wider">
                    {t('serviceDetail.incidents.status', 'Status')}
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase tracking-wider">
                    {t('serviceDetail.incidents.opened', 'Opened')}
                  </th>
                  <th className="px-4 py-3" />
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {incidents.map((incident) => (
                  <tr key={incident.incidentId} className="hover:bg-elevated/50 transition-colors">
                    <td className="px-4 py-3">
                      <div className="flex items-center gap-1.5">
                        {statusIcon(incident.status)}
                        <span className="font-medium text-heading truncate max-w-xs">
                          {incident.title}
                        </span>
                      </div>
                    </td>
                    <td className="px-4 py-3">
                      {severityBadge(incident.severity)}
                    </td>
                    <td className="px-4 py-3">
                      <span className="text-xs text-muted capitalize">{incident.status}</span>
                    </td>
                    <td className="px-4 py-3 text-xs text-muted tabular-nums">
                      {incident.createdAt ? formatRelativeTime(incident.createdAt) : '—'}
                    </td>
                    <td className="px-4 py-3 text-right">
                      <Link
                        to={`/operations/incidents/${incident.incidentId}`}
                        className="text-xs text-accent hover:underline"
                      >
                        {t('common.view', 'View')}
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </CardBody>
      </Card>

      {/* ── Links rápidos ── */}
      <div className="flex gap-3 flex-wrap">
        <Link
          to={`/operations/incidents?serviceId=${serviceId}`}
          className="flex items-center gap-1.5 text-xs text-accent border border-accent/30 rounded px-3 py-1.5 hover:bg-accent/5 transition-colors"
        >
          <AlertTriangle size={12} />
          {t('serviceDetail.incidents.allIncidents', 'All Incidents')}
        </Link>
        <Link
          to={`/operations/post-incident?serviceId=${serviceId}`}
          className="flex items-center gap-1.5 text-xs text-accent border border-accent/30 rounded px-3 py-1.5 hover:bg-accent/5 transition-colors"
        >
          <ExternalLink size={12} />
          {t('serviceDetail.incidents.postMortems', 'Post-Mortems')}
        </Link>
        <Link
          to={`/operations/runbooks?serviceId=${serviceId}`}
          className="flex items-center gap-1.5 text-xs text-accent border border-accent/30 rounded px-3 py-1.5 hover:bg-accent/5 transition-colors"
        >
          <ExternalLink size={12} />
          {t('serviceDetail.incidents.runbooks', 'Runbooks')}
        </Link>
      </div>
    </div>
  );
}
