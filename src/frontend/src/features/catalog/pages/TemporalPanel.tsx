import { useTranslation } from 'react-i18next';
import { Clock, RefreshCw } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button, Select } from '../../../shared/ui';
import type { GraphSnapshotSummary, TemporalDiffResult } from '../../../types';

export interface TemporalPanelProps {
  snapshots: GraphSnapshotSummary[];
  selectedFrom: string;
  selectedTo: string;
  diffResult: TemporalDiffResult | null;
  diffLoading: boolean;
  onSelectFrom: (id: string) => void;
  onSelectTo: (id: string) => void;
  onCreateSnapshot: () => void;
  createSnapshotPending: boolean;
}

/** Painel de comparação temporal entre snapshots do grafo. */
export function TemporalPanel({
  snapshots, selectedFrom, selectedTo, diffResult, diffLoading,
  onSelectFrom, onSelectTo, onCreateSnapshot, createSnapshotPending,
}: TemporalPanelProps) {
  const { t } = useTranslation();

  const snapshotOptions = snapshots.map((s) => ({
    value: s.snapshotId,
    label: `${s.label} — ${new Date(s.capturedAt).toLocaleDateString()} (${s.nodeCount} ${t('serviceCatalog.temporal.nodes')}, ${s.edgeCount} ${t('serviceCatalog.temporal.edges')})`,
  }));

  return (
    <div className="space-y-4">
      {/* ── Controles de snapshot ────────────────────────────── */}
      <Card>
        <CardBody>
          <div className="flex items-end gap-4">
            <Select
              className="flex-1"
              label={t('serviceCatalog.temporal.fromSnapshot')}
              value={selectedFrom}
              onChange={(e) => onSelectFrom(e.target.value)}
              options={snapshotOptions}
              placeholder={t('serviceCatalog.temporal.selectSnapshot')}
              size="sm"
            />
            <Select
              className="flex-1"
              label={t('serviceCatalog.temporal.toSnapshot')}
              value={selectedTo}
              onChange={(e) => onSelectTo(e.target.value)}
              options={snapshotOptions}
              placeholder={t('serviceCatalog.temporal.selectSnapshot')}
              size="sm"
            />
            <Button
              variant="secondary"
              onClick={onCreateSnapshot}
              loading={createSnapshotPending}
            >
              <Clock size={16} /> {t('serviceCatalog.temporal.createSnapshot')}
            </Button>
          </div>
        </CardBody>
      </Card>

      {/* ── Estado sem snapshots ─────────────────────────────── */}
      {snapshots.length === 0 && (
        <Card>
          <CardBody className="py-12 text-center">
            <Clock size={40} className="mx-auto text-muted mb-4" />
            <p className="text-muted">{t('serviceCatalog.temporal.noSnapshots')}</p>
            <p className="text-xs text-muted mt-2">{t('serviceCatalog.temporal.createFirst')}</p>
          </CardBody>
        </Card>
      )}

      {/* ── Lista de snapshots ──────────────────────────────── */}
      {snapshots.length > 0 && (
        <Card>
          <CardHeader>
            <h3 className="font-semibold text-heading text-sm">{t('serviceCatalog.temporal.snapshotHistory')}</h3>
          </CardHeader>
          <CardBody className="p-0">
            <table className="min-w-full text-sm">
              <thead>
                <tr className="border-b border-edge bg-panel text-left">
                  <th className="px-4 py-2 font-medium text-muted">{t('serviceCatalog.temporal.label')}</th>
                  <th className="px-4 py-2 font-medium text-muted">{t('serviceCatalog.temporal.capturedAt')}</th>
                  <th className="px-4 py-2 font-medium text-muted">{t('serviceCatalog.temporal.nodes')}</th>
                  <th className="px-4 py-2 font-medium text-muted">{t('serviceCatalog.temporal.edges')}</th>
                  <th className="px-4 py-2 font-medium text-muted">{t('serviceCatalog.temporal.createdBy')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-edge">
                {snapshots.map((s) => (
                  <tr key={s.snapshotId} className="hover:bg-hover transition-colors">
                    <td className="px-4 py-2 text-body font-medium">{s.label}</td>
                    <td className="px-4 py-2 text-body">{new Date(s.capturedAt).toLocaleString()}</td>
                    <td className="px-4 py-2 text-body">{s.nodeCount}</td>
                    <td className="px-4 py-2 text-body">{s.edgeCount}</td>
                    <td className="px-4 py-2 text-xs text-muted">{s.createdBy}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </CardBody>
        </Card>
      )}

      {/* ── Carregando diff ─────────────────────────────────── */}
      {diffLoading && (
        <div className="flex items-center justify-center py-8">
          <RefreshCw size={20} className="animate-spin text-muted" />
          <span className="ml-2 text-muted">{t('serviceCatalog.temporal.calculatingDiff')}</span>
        </div>
      )}

      {/* ── Resultado do diff temporal ──────────────────────── */}
      {diffResult && (
        <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
          <Card>
            <CardBody className="text-center">
              <p className="text-2xl font-bold text-success">+{diffResult.addedNodesCount}</p>
              <p className="text-xs text-muted">{t('serviceCatalog.temporal.addedNodes')}</p>
            </CardBody>
          </Card>
          <Card>
            <CardBody className="text-center">
              <p className="text-2xl font-bold text-critical">-{diffResult.removedNodesCount}</p>
              <p className="text-xs text-muted">{t('serviceCatalog.temporal.removedNodes')}</p>
            </CardBody>
          </Card>
          <Card>
            <CardBody className="text-center">
              <p className="text-2xl font-bold text-success">+{diffResult.addedEdgesCount}</p>
              <p className="text-xs text-muted">{t('serviceCatalog.temporal.addedEdges')}</p>
            </CardBody>
          </Card>
          <Card>
            <CardBody className="text-center">
              <p className="text-2xl font-bold text-critical">-{diffResult.removedEdgesCount}</p>
              <p className="text-xs text-muted">{t('serviceCatalog.temporal.removedEdges')}</p>
            </CardBody>
          </Card>
        </div>
      )}
    </div>
  );
}
