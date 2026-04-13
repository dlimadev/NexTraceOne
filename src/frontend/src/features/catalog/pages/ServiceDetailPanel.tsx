import { useTranslation } from 'react-i18next';
import { X } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import type { AssetGraph, NodeHealthResult } from '../../../types';

/** Variantes de badge para status de saúde dos nós. */
const healthBadgeVariant = (status: string): 'success' | 'warning' | 'danger' | 'default' => {
  switch (status.toLowerCase()) {
    case 'healthy': return 'success';
    case 'degraded': return 'warning';
    case 'unhealthy': return 'danger';
    default: return 'default';
  }
};

export interface ServiceDetailPanelProps {
  graph: AssetGraph;
  nodeId: string;
  healthData: NodeHealthResult | null;
  onClose: () => void;
}

/** Painel lateral de detalhe que mostra contexto operacional de um serviço ou API selecionado. */
export function ServiceDetailPanel({
  graph,
  nodeId,
  healthData,
  onClose,
}: ServiceDetailPanelProps) {
  const { t } = useTranslation();

  const service = graph.services.find((s) => s.serviceAssetId === nodeId);
  const api = graph.apis.find((a) => a.apiAssetId === nodeId);
  const nodeName = service?.name ?? api?.name ?? nodeId;
  const nodeHealth = healthData?.items?.find((h) => h.nodeId === nodeId);
  const healthStatus = nodeHealth?.status ?? 'Unknown';

  const consumerCount = api?.consumers?.length ?? 0;
  const dependencyCount = service
    ? graph.apis.filter((a) => a.consumers?.some((c) => c.consumerName === nodeId)).length
    : 0;

  return (
    <div className="absolute top-0 right-0 w-80 z-10 animate-fade-in">
      <Card className="shadow-lg border-edge">
        <CardHeader className="flex items-center justify-between">
          <h3 className="font-semibold text-heading text-sm">{t('serviceCatalog.detail.title')}</h3>
          <button onClick={onClose} className="text-muted hover:text-body transition-colors" aria-label={t('serviceCatalog.detail.close')}>
            <X size={16} />
          </button>
        </CardHeader>
        <CardBody className="space-y-4">
          {/* Nome e saúde */}
          <div>
            <p className="font-medium text-heading">{nodeName}</p>
            <div className="flex items-center gap-2 mt-1">
              <Badge variant={healthBadgeVariant(healthStatus)}>
                {t(`serviceCatalog.overview.${healthStatus.toLowerCase()}`)}
              </Badge>
              {nodeHealth && <span className="text-xs text-muted">{t('serviceCatalog.overview.healthScore')}: {nodeHealth.score.toFixed(2)}</span>}
            </div>
          </div>

          {/* Metadados do serviço */}
          {service && (
            <div className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-muted">{t('serviceCatalog.detail.domain')}</span>
                <span className="text-heading">{service.domain}</span>
              </div>
              <div className="flex justify-between">
                <span className="text-muted">{t('serviceCatalog.detail.team')}</span>
                <span className="text-heading">{service.teamName}</span>
              </div>
            </div>
          )}

          {/* Metadados da API */}
          {api && (
            <div className="space-y-2 text-sm">
              <div className="flex justify-between">
                <span className="text-muted">{t('serviceCatalog.detail.version')}</span>
                <span className="text-heading">v{api.version}</span>
              </div>
            </div>
          )}

          {/* Contadores */}
          <div className="grid grid-cols-2 gap-3">
            <div className="rounded-md bg-elevated p-3 text-center">
              <p className="text-lg font-bold text-heading">{consumerCount}</p>
              <p className="text-xs text-muted">{t('serviceCatalog.detail.consumerCount')}</p>
            </div>
            <div className="rounded-md bg-elevated p-3 text-center">
              <p className="text-lg font-bold text-heading">{dependencyCount}</p>
              <p className="text-xs text-muted">{t('serviceCatalog.detail.dependencyCount')}</p>
            </div>
          </div>

          {/* Proveniência dos dados */}
          <div>
            <p className="text-xs font-medium text-heading mb-2">{t('serviceCatalog.detail.dataProvenance')}</p>
            <div className="space-y-1 text-xs text-muted">
              <div className="flex justify-between">
                <span>{t('serviceCatalog.overview.provenance')}</span>
                <Badge variant="default">{t('serviceCatalog.overview.catalogImport')}</Badge>
              </div>
              <div className="flex justify-between">
                <span>{t('serviceCatalog.overview.confidence')}</span>
                <span>—</span>
              </div>
              <div className="flex justify-between">
                <span>{t('serviceCatalog.overview.freshness')}</span>
                <span>—</span>
              </div>
            </div>
          </div>

          {/* Issues críticas */}
          <div>
            <p className="text-xs font-medium text-heading mb-1">{t('serviceCatalog.detail.criticalIssues')}</p>
            <p className="text-xs text-muted">{t('serviceCatalog.detail.noCriticalIssues')}</p>
          </div>

          <Button variant="secondary" className="w-full" onClick={onClose}>
            {t('serviceCatalog.detail.close')}
          </Button>
        </CardBody>
      </Card>
    </div>
  );
}
