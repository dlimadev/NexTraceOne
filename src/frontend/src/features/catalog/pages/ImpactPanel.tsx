import { useTranslation } from 'react-i18next';
import { Zap, RefreshCw, AlertTriangle, Shield } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Select } from '../../../shared/ui';
import type { AssetGraph, ImpactPropagationResult } from '../../../types';

/** Mapeamento de confiança para variantes visuais de badge. */
const confidenceVariant = (score: number): 'default' | 'success' | 'warning' | 'danger' | 'info' => {
  if (score >= 0.9) return 'success';
  if (score >= 0.7) return 'info';
  if (score >= 0.4) return 'warning';
  return 'danger';
};

export interface ImpactPanelProps {
  graph: AssetGraph | undefined;
  selectedNodeId: string | null;
  impactResult: ImpactPropagationResult | null;
  impactLoading: boolean;
  impactDepth: number;
  onSelectNode: (id: string) => void;
  onChangeDepth: (depth: number) => void;
}

/** Painel de análise de propagação de impacto (blast radius). */
export function ImpactPanel({ graph, selectedNodeId, impactResult, impactLoading, impactDepth, onSelectNode, onChangeDepth }: ImpactPanelProps) {
  const { t } = useTranslation();

  const nodeOptions = [
    { value: '', label: t('serviceCatalog.impact.selectNodePlaceholder') },
    ...(graph?.apis?.map((api) => ({
      value: api.apiAssetId,
      label: `API: ${api.name} (${api.routePattern})`,
    })) ?? []),
    ...(graph?.services?.map((svc) => ({
      value: svc.serviceAssetId,
      label: `Service: ${svc.name} (${svc.domain})`,
    })) ?? []),
  ];
  const depthOptions = [1, 2, 3, 4, 5].map((d) => ({ value: String(d), label: String(d) }));

  return (
    <div className="space-y-4">
      {/* ── Seletor de nó e profundidade ────────────────────── */}
      <Card>
        <CardBody>
          <div className="flex items-end gap-4">
            <Select
              className="flex-1"
              label={t('serviceCatalog.impact.selectNode')}
              value={selectedNodeId ?? ''}
              onChange={(e) => onSelectNode(e.target.value)}
              options={nodeOptions}
              size="sm"
            />
            <Select
              className="w-32"
              label={t('serviceCatalog.impact.maxDepth')}
              value={impactDepth}
              onChange={(e) => onChangeDepth(Number(e.target.value))}
              options={depthOptions}
              size="sm"
            />
          </div>
        </CardBody>
      </Card>

      {/* ── Estado sem seleção ───────────────────────────────── */}
      {!selectedNodeId && (
        <Card>
          <CardBody className="py-12 text-center">
            <Zap size={40} className="mx-auto text-muted mb-4" />
            <p className="text-muted">{t('serviceCatalog.impact.selectNodeHint')}</p>
          </CardBody>
        </Card>
      )}

      {/* ── Carregando ──────────────────────────────────────── */}
      {selectedNodeId && impactLoading && (
        <div className="flex items-center justify-center py-12">
          <RefreshCw size={20} className="animate-spin text-muted" />
          <span className="ml-2 text-muted">{t('serviceCatalog.impact.calculating')}</span>
        </div>
      )}

      {/* ── Resultado do impacto ────────────────────────────── */}
      {impactResult && (
        <>
          <div className="grid grid-cols-3 gap-4">
            <Card>
              <CardBody className="text-center">
                <p className="text-3xl font-bold text-heading">{impactResult.directCount + impactResult.transitiveCount}</p>
                <p className="text-xs text-muted">{t('serviceCatalog.impact.totalImpacted')}</p>
              </CardBody>
            </Card>
            <Card>
              <CardBody className="text-center">
                <p className="text-3xl font-bold text-warning">{impactResult.directCount}</p>
                <p className="text-xs text-muted">{t('serviceCatalog.impact.directConsumers')}</p>
              </CardBody>
            </Card>
            <Card>
              <CardBody className="text-center">
                <p className="text-3xl font-bold text-critical">{impactResult.transitiveCount}</p>
                <p className="text-xs text-muted">{t('serviceCatalog.impact.transitiveConsumers')}</p>
              </CardBody>
            </Card>
          </div>

          {impactResult.impactedNodes.length > 0 && (
            <Card>
              <CardHeader>
                <h3 className="font-semibold text-heading text-sm">{t('serviceCatalog.impact.impactedNodes')}</h3>
              </CardHeader>
              <CardBody className="p-0">
                <table className="min-w-full text-sm">
                  <thead>
                    <tr className="border-b border-edge bg-panel text-left">
                      <th className="px-4 py-2 font-medium text-muted">{t('serviceCatalog.impact.node')}</th>
                      <th className="px-4 py-2 font-medium text-muted">{t('serviceCatalog.impact.depth')}</th>
                      <th className="px-4 py-2 font-medium text-muted">{t('serviceCatalog.confidence')}</th>
                      <th className="px-4 py-2 font-medium text-muted">{t('serviceCatalog.impact.path')}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-edge">
                    {impactResult.impactedNodes.map((node) => (
                      <tr key={node.nodeId} className="hover:bg-hover transition-colors">
                        <td className="px-4 py-2 text-body font-medium">
                          <div className="flex items-center gap-2">
                            {node.depth === 1 ? (
                              <AlertTriangle size={14} className="text-warning" />
                            ) : (
                              <Shield size={14} className="text-critical" />
                            )}
                            {node.nodeName}
                          </div>
                        </td>
                        <td className="px-4 py-2">
                          <Badge variant={node.depth === 1 ? 'warning' : 'danger'}>{node.depth}</Badge>
                        </td>
                        <td className="px-4 py-2">
                          <Badge variant={confidenceVariant(node.confidenceScore)}>
                            {Math.round(node.confidenceScore * 100)}%
                          </Badge>
                        </td>
                        <td className="px-4 py-2 text-xs text-muted font-mono">{node.impactPath}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </CardBody>
            </Card>
          )}
        </>
      )}
    </div>
  );
}
