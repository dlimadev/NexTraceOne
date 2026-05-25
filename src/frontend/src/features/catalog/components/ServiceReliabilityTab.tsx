/**
 * ServiceReliabilityTab — SLOs e confiabilidade do serviço.
 *
 * Exibe os SLOs registrados para este serviço e link para
 * gerenciamento completo de error budget.
 *
 * @pillar Service 360° — Confiabilidade contextualizada ao serviço
 */
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Shield, ExternalLink, Activity, CheckCircle } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Link } from 'react-router-dom';
import { reliabilityApi, type ServiceSloItem } from '../../operations/api/reliability';

interface Props {
  serviceId: string;
}

function sloActiveBadge(isActive: boolean) {
  return isActive
    ? <Badge variant="success" size="sm"><CheckCircle size={10} className="inline mr-1" />Active</Badge>
    : <Badge variant="default" size="sm">Inactive</Badge>;
}

export function ServiceReliabilityTab({ serviceId }: Props) {
  const { t } = useTranslation();

  const { data, isLoading } = useQuery({
    queryKey: ['service-slos', serviceId],
    queryFn: () => reliabilityApi.listServiceSlos(serviceId),
    staleTime: 60_000,
  });

  const slos: ServiceSloItem[] = data?.items ?? [];
  const activeCount = slos.filter(s => s.isActive).length;

  return (
    <div className="flex flex-col gap-6">
      {/* ── Resumo ── */}
      {slos.length > 0 && (
        <div className="grid grid-cols-3 gap-3">
          <div className="p-4 bg-elevated rounded-lg border border-edge text-center">
            <div className="text-2xl font-bold text-heading">{slos.length}</div>
            <div className="text-xs text-muted mt-0.5">{t('serviceDetail.reliability.total', 'Total SLOs')}</div>
          </div>
          <div className="p-4 bg-elevated rounded-lg border border-edge text-center">
            <div className="text-2xl font-bold text-success">{activeCount}</div>
            <div className="text-xs text-muted mt-0.5">{t('serviceDetail.reliability.active', 'Active')}</div>
          </div>
          <div className="p-4 bg-elevated rounded-lg border border-edge text-center">
            <div className="text-2xl font-bold text-muted">{slos.length - activeCount}</div>
            <div className="text-xs text-muted mt-0.5">{t('serviceDetail.reliability.inactive', 'Inactive')}</div>
          </div>
        </div>
      )}

      {/* ── Lista de SLOs ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Shield size={16} className="text-accent" />
              <h3 className="text-sm font-semibold text-heading">
                {t('serviceDetail.reliability.slos', 'Service SLOs')}
              </h3>
              {slos.length > 0 && (
                <span className="text-xs text-muted">({slos.length})</span>
              )}
            </div>
            <Link
              to={`/operations/reliability?serviceId=${serviceId}`}
              className="flex items-center gap-1 text-xs text-accent hover:underline"
            >
              <ExternalLink size={11} />
              {t('serviceDetail.reliability.manageAll', 'Manage SLOs')}
            </Link>
          </div>
        </CardHeader>
        <CardBody className="p-0">
          {isLoading ? (
            <div className="py-8 text-center">
              <Activity size={20} className="text-muted animate-pulse mx-auto" />
            </div>
          ) : slos.length === 0 ? (
            <div className="py-10 text-center px-4">
              <Shield size={20} className="text-muted mx-auto mb-2" />
              <p className="text-sm text-muted">{t('serviceDetail.reliability.noSlos', 'No SLOs defined for this service.')}</p>
              <Link
                to={`/operations/reliability?serviceId=${serviceId}`}
                className="mt-3 inline-flex items-center gap-1 text-xs text-accent hover:underline"
              >
                <ExternalLink size={11} />
                {t('serviceDetail.reliability.addSlo', 'Add an SLO')}
              </Link>
            </div>
          ) : (
            <table className="w-full text-sm">
              <thead className="border-b border-edge">
                <tr>
                  <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase tracking-wider">
                    {t('serviceDetail.reliability.sloName', 'SLO')}
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase tracking-wider">
                    {t('serviceDetail.reliability.type', 'Type')}
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase tracking-wider">
                    {t('serviceDetail.reliability.target', 'Target')}
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase tracking-wider">
                    {t('serviceDetail.reliability.window', 'Window')}
                  </th>
                  <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase tracking-wider">
                    {t('serviceDetail.reliability.status', 'Status')}
                  </th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {slos.map((slo) => (
                  <tr key={slo.id} className="hover:bg-elevated/50 transition-colors">
                    <td className="px-4 py-3 font-medium text-heading">{slo.name}</td>
                    <td className="px-4 py-3 text-muted text-xs">{slo.type}</td>
                    <td className="px-4 py-3 text-muted tabular-nums">{slo.targetPercent?.toFixed(2)}%</td>
                    <td className="px-4 py-3 text-muted text-xs">{slo.windowDays}d</td>
                    <td className="px-4 py-3">
                      {sloActiveBadge(slo.isActive)}
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
          to={`/operations/reliability?serviceId=${serviceId}`}
          className="flex items-center gap-1.5 text-xs text-accent border border-accent/30 rounded px-3 py-1.5 hover:bg-accent/5 transition-colors"
        >
          <Shield size={12} />
          {t('serviceDetail.reliability.sloManagement', 'SLO Management')}
        </Link>
        <Link
          to={`/operations/slo-burn-rate?serviceId=${serviceId}`}
          className="flex items-center gap-1.5 text-xs text-accent border border-accent/30 rounded px-3 py-1.5 hover:bg-accent/5 transition-colors"
        >
          <Activity size={12} />
          {t('serviceDetail.reliability.burnRate', 'Burn Rate')}
        </Link>
      </div>
    </div>
  );
}
