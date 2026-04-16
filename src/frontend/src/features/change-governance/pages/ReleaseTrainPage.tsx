import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation } from '@tanstack/react-query';
import {
  Train,
  Plus,
  Trash2,
  AlertTriangle,
  CheckCircle2,
  Clock,
  ShieldAlert,
  Users,
  TrendingUp,
  Zap,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Button } from '../../../components/Button';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { EmptyState } from '../../../components/EmptyState';
import { changeIntelligenceApi } from '../api/changeIntelligence';
import type { ReleaseTrainEvaluationResponse, TrainReleaseItem } from '../api/changeIntelligence';

const INPUT_CLS =
  'w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors';

function readinessBadge(
  readiness: string,
): 'success' | 'warning' | 'danger' | 'default' {
  if (readiness === 'Ready') return 'success';
  if (readiness === 'PartiallyReady') return 'warning';
  if (readiness === 'NotReady') return 'danger';
  return 'default';
}

function readinessIcon(readiness: string) {
  if (readiness === 'Ready')
    return <CheckCircle2 size={16} className="text-success" />;
  if (readiness === 'PartiallyReady')
    return <AlertTriangle size={16} className="text-warning" />;
  return <AlertTriangle size={16} className="text-critical" />;
}

function scoreBadge(score: number | null): 'success' | 'warning' | 'danger' | 'default' {
  if (score === null) return 'default';
  if (score < 0.4) return 'success';
  if (score < 0.7) return 'warning';
  return 'danger';
}

function TrainReleaseRow({ item, t }: { item: TrainReleaseItem; t: (k: string) => string }) {
  return (
    <tr className="border-t border-edge hover:bg-surface/50 transition-colors">
      <td className="px-4 py-3">
        <div className="font-medium text-heading text-sm">{item.serviceName}</div>
        <div className="text-xs text-muted font-mono">{item.version}</div>
      </td>
      <td className="px-4 py-3">
        <Badge variant="default">{item.environment}</Badge>
      </td>
      <td className="px-4 py-3">
        <Badge variant={item.status === 'Succeeded' ? 'success' : item.status === 'Failed' || item.status === 'RolledBack' ? 'danger' : 'info'}>
          {item.status}
        </Badge>
      </td>
      <td className="px-4 py-3">
        <Badge variant={scoreBadge(item.riskScore)}>
          {item.riskScore !== null ? item.riskScore.toFixed(2) : t('releaseTrain.scoreNA')}
        </Badge>
      </td>
      <td className="px-4 py-3 text-sm text-muted">{item.totalAffectedConsumers}</td>
      <td className="px-4 py-3">
        {item.isHighRisk && (
          <ShieldAlert size={16} className="text-critical" />
        )}
      </td>
    </tr>
  );
}

/**
 * Página de Release Train — avaliação coordenada de múltiplas releases entre serviços.
 * Compõe um visão agregada de risk score, blast radius e readiness signal.
 * Gap 1: Release Train / multi-service coordinated release (4.10).
 */
export function ReleaseTrainPage() {
  const { t } = useTranslation();

  const [trainName, setTrainName] = useState('');
  const [releaseIdInput, setReleaseIdInput] = useState('');
  const [releaseIds, setReleaseIds] = useState<string[]>([]);
  const [result, setResult] = useState<ReleaseTrainEvaluationResponse | null>(null);
  const [inputError, setInputError] = useState<string | null>(null);

  const { mutate: evaluate, isPending } = useMutation({
    mutationFn: () =>
      changeIntelligenceApi.evaluateReleaseTrain({ trainName, releaseIds }),
    onSuccess: (data) => setResult(data),
  });

  function addReleaseId() {
    const id = releaseIdInput.trim();
    if (!id) return;
    const uuidPattern =
      /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i;
    if (!uuidPattern.test(id)) {
      setInputError(t('releaseTrain.invalidUuid'));
      return;
    }
    if (releaseIds.includes(id)) {
      setInputError(t('releaseTrain.duplicateId'));
      return;
    }
    setInputError(null);
    setReleaseIds((prev) => [...prev, id]);
    setReleaseIdInput('');
  }

  function removeReleaseId(id: string) {
    setReleaseIds((prev) => prev.filter((r) => r !== id));
  }

  function handleEvaluate() {
    if (!trainName.trim()) {
      setInputError(t('releaseTrain.nameRequired'));
      return;
    }
    if (releaseIds.length < 2) {
      setInputError(t('releaseTrain.minReleases'));
      return;
    }
    setInputError(null);
    setResult(null);
    evaluate();
  }

  return (
    <PageContainer>
      <PageHeader
        title={t('releaseTrain.title')}
        subtitle={t('releaseTrain.subtitle')}
      />

      {/* Composer card */}
      <Card className="mb-6">
        <CardHeader>
          <h3 className="text-sm font-semibold text-heading">{t('releaseTrain.composerTitle')}</h3>
        </CardHeader>
        <CardBody>
          <div className="space-y-4">
            {/* Train name */}
            <div>
              <label className="block text-xs font-medium text-muted mb-1">
                {t('releaseTrain.nameLabel')}
              </label>
              <input
                className={INPUT_CLS}
                placeholder={t('releaseTrain.namePlaceholder')}
                value={trainName}
                onChange={(e) => setTrainName(e.target.value)}
              />
            </div>

            {/* Release ID input */}
            <div>
              <label className="block text-xs font-medium text-muted mb-1">
                {t('releaseTrain.addReleaseLabel')}
              </label>
              <div className="flex gap-2">
                <input
                  className={INPUT_CLS}
                  placeholder={t('releaseTrain.addReleasePlaceholder')}
                  value={releaseIdInput}
                  onChange={(e) => setReleaseIdInput(e.target.value)}
                  onKeyDown={(e) => {
                    if (e.key === 'Enter') addReleaseId();
                  }}
                />
                <Button variant="secondary" onClick={addReleaseId}>
                  <Plus size={16} />
                  {t('releaseTrain.addButton')}
                </Button>
              </div>
              {inputError && (
                <p className="text-xs text-critical mt-1">{inputError}</p>
              )}
            </div>

            {/* Release ID list */}
            {releaseIds.length > 0 && (
              <div className="space-y-2">
                <p className="text-xs font-medium text-muted">
                  {t('releaseTrain.releasesAdded', { count: releaseIds.length })}
                </p>
                {releaseIds.map((id) => (
                  <div
                    key={id}
                    className="flex items-center justify-between bg-surface rounded-md px-3 py-2"
                  >
                    <span className="font-mono text-xs text-heading">{id}</span>
                    <button
                      onClick={() => removeReleaseId(id)}
                      className="text-muted hover:text-critical transition-colors"
                      aria-label={t('releaseTrain.removeRelease')}
                    >
                      <Trash2 size={14} />
                    </button>
                  </div>
                ))}
              </div>
            )}

            <Button
              variant="primary"
              onClick={handleEvaluate}
              disabled={isPending || releaseIds.length < 2 || !trainName.trim()}
            >
              <Zap size={16} />
              {isPending ? t('common.loading') : t('releaseTrain.evaluateButton')}
            </Button>
          </div>
        </CardBody>
      </Card>

      {/* Result */}
      {result && (
        <>
          {/* Summary */}
          <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4 mb-6">
            <Card>
              <CardBody>
                <div className="flex items-center gap-3">
                  {readinessIcon(result.readiness)}
                  <div>
                    <p className="text-xs text-muted">{t('releaseTrain.readiness')}</p>
                    <Badge variant={readinessBadge(result.readiness)}>{result.readiness}</Badge>
                  </div>
                </div>
              </CardBody>
            </Card>
            <Card>
              <CardBody>
                <div className="flex items-center gap-3">
                  <TrendingUp size={16} className="text-accent" />
                  <div>
                    <p className="text-xs text-muted">{t('releaseTrain.aggregateScore')}</p>
                    <p className="text-lg font-bold text-heading">
                      {result.aggregateRiskScore !== null
                        ? result.aggregateRiskScore.toFixed(3)
                        : t('releaseTrain.scoreNA')}
                    </p>
                  </div>
                </div>
              </CardBody>
            </Card>
            <Card>
              <CardBody>
                <div className="flex items-center gap-3">
                  <Users size={16} className="text-accent" />
                  <div>
                    <p className="text-xs text-muted">{t('releaseTrain.combinedBlastRadius')}</p>
                    <p className="text-lg font-bold text-heading">
                      {result.combinedAffectedConsumers}
                    </p>
                  </div>
                </div>
              </CardBody>
            </Card>
            <Card>
              <CardBody>
                <div className="flex items-center gap-3">
                  <Clock size={16} className="text-accent" />
                  <div>
                    <p className="text-xs text-muted">{t('releaseTrain.services')}</p>
                    <p className="text-lg font-bold text-heading">
                      {result.foundCount}/{result.requestedCount}
                    </p>
                  </div>
                </div>
              </CardBody>
            </Card>
          </div>

          {/* Blockers */}
          {result.blockingServices.length > 0 && (
            <Card className="mb-6 border border-critical/30">
              <CardBody>
                <div className="flex items-start gap-2">
                  <AlertTriangle size={16} className="text-critical mt-0.5 shrink-0" />
                  <div>
                    <p className="text-sm font-semibold text-critical mb-1">
                      {t('releaseTrain.blockersTitle')}
                    </p>
                    <div className="flex flex-wrap gap-2">
                      {result.blockingServices.map((svc) => (
                        <Badge key={svc} variant="danger">{svc}</Badge>
                      ))}
                    </div>
                  </div>
                </div>
              </CardBody>
            </Card>
          )}

          {/* Not found */}
          {result.notFoundIds.length > 0 && (
            <Card className="mb-6 border border-warning/30">
              <CardBody>
                <div className="flex items-start gap-2">
                  <AlertTriangle size={16} className="text-warning mt-0.5 shrink-0" />
                  <div>
                    <p className="text-sm font-semibold text-warning mb-1">
                      {t('releaseTrain.notFoundTitle')}
                    </p>
                    <div className="space-y-1">
                      {result.notFoundIds.map((id) => (
                        <p key={id} className="text-xs font-mono text-muted">{id}</p>
                      ))}
                    </div>
                  </div>
                </div>
              </CardBody>
            </Card>
          )}

          {/* Releases table */}
          <Card>
            <CardHeader>
              <h3 className="text-sm font-semibold text-heading">{t('releaseTrain.releasesTableTitle')}</h3>
            </CardHeader>
            <CardBody>
              {result.releases.length === 0 ? (
                <EmptyState title={t('releaseTrain.noReleases')} />
              ) : (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="text-left text-xs font-medium text-muted uppercase tracking-wider">
                        <th className="px-4 py-2">{t('releaseTrain.colService')}</th>
                        <th className="px-4 py-2">{t('releaseTrain.colEnvironment')}</th>
                        <th className="px-4 py-2">{t('releaseTrain.colStatus')}</th>
                        <th className="px-4 py-2">{t('releaseTrain.colScore')}</th>
                        <th className="px-4 py-2">{t('releaseTrain.colBlastRadius')}</th>
                        <th className="px-4 py-2">{t('releaseTrain.colHighRisk')}</th>
                      </tr>
                    </thead>
                    <tbody>
                      {result.releases.map((item) => (
                        <TrainReleaseRow key={item.releaseId} item={item} t={t} />
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </CardBody>
          </Card>
        </>
      )}

      {!result && !isPending && releaseIds.length === 0 && (
        <EmptyState
          icon={<Train size={32} />}
          title={t('releaseTrain.emptyTitle')}
          description={t('releaseTrain.emptyMessage')}
        />
      )}
    </PageContainer>
  );
}
