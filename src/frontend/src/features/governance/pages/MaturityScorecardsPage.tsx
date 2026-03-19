import { useState, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { Award, Loader2, AlertTriangle } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import type { MaturityScorecardsResponse, MaturityLevelType } from '../../../types';
import { PageContainer } from '../../../components/shell';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { organizationGovernanceApi } from '../api/organizationGovernance';

type MaturityDimension = 'team' | 'domain';

/** Mapeia MaturityLevelType para variante do Badge. */
const maturityBadgeVariant = (level: MaturityLevelType): 'success' | 'warning' | 'danger' | 'info' | 'default' => {
  switch (level) {
    case 'Optimizing': return 'success';
    case 'Managed': return 'success';
    case 'Defined': return 'info';
    case 'Developing': return 'warning';
    case 'Initial': return 'danger';
    default: return 'default';
  }
};

/** Mapeia score para classe de cor da barra de progresso. */
const scoreBarColor = (score: number, maxScore: number): string => {
  const pct = maxScore === 0 ? 0 : (score / maxScore) * 100;
  if (pct >= 80) return 'bg-emerald-500';
  if (pct >= 60) return 'bg-amber-500';
  if (pct >= 40) return 'bg-orange-500';
  return 'bg-critical';
};

/**
 * Página de Scorecards de Maturidade — avaliação de maturidade por equipa baseada em cobertura real de rollouts.
 * Dados derivados de Teams e Governance Pack rollouts por equipa.
 * Parte do módulo Executive Governance do NexTraceOne.
 */
export function MaturityScorecardsPage() {
  const { t } = useTranslation();
  const [dimension, setDimension] = useState<MaturityDimension>('team');
  const [data, setData] = useState<MaturityScorecardsResponse | null>(null);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);

  const dimensions: MaturityDimension[] = ['team', 'domain'];

  useEffect(() => {
    let cancelled = false;
    setLoading(true);
    setError(null);
    organizationGovernanceApi.getMaturityScorecards(dimension)
      .then((d) => { if (!cancelled) { setData(d); setLoading(false); } })
      .catch((err) => { if (!cancelled) { setError(err.message || t('common.errorLoading')); setLoading(false); } });
    return () => { cancelled = true; };
  }, [dimension, t]);

  return (
    <PageContainer>
      {/* Header */}
      <div className="mb-6">
        <h1 className="text-2xl font-bold text-heading">{t('governance.executive.scorecardsTitle')}</h1>
        <p className="text-muted mt-1">{t('governance.executive.scorecardsSubtitle')}</p>
      </div>

      {/* Dimension Selector */}
      <div className="flex flex-wrap items-center gap-3 mb-6">
        {dimensions.map(dim => (
          <button
            key={dim}
            onClick={() => setDimension(dim)}
            className={`px-3 py-1.5 text-xs rounded-md border transition-colors ${
              dimension === dim
                ? 'bg-accent/10 text-accent border-accent/30'
                : 'bg-surface text-muted border-edge hover:text-body'
            }`}
          >
            {t(`governance.executive.scorecardsDimension${dim.charAt(0).toUpperCase()}${dim.slice(1)}`)}
          </button>
        ))}
      </div>

      {loading && <PageLoadingState />}

      {!loading && (error || !data) && (
        <PageErrorState message={error ?? undefined} />
      )}

      {/* Scorecards */}
      {!loading && data && (
        <div className="space-y-6">
          {data.scorecards.length === 0 && (
            <div className="text-center py-12 text-muted">
              <Award size={32} className="mx-auto mb-2 opacity-50" />
              <p className="text-sm">{t('governance.executive.noScorecards')}</p>
            </div>
          )}
          {data.scorecards.map(sc => (
            <Card key={sc.groupId}>
              <CardHeader>
                <div className="flex items-center gap-3">
                  <Award size={16} className="text-accent" />
                  <span className="text-sm font-semibold text-heading">{sc.groupName}</span>
                  <Badge variant={maturityBadgeVariant(sc.overallMaturity)}>
                    {t(`governance.maturity.${sc.overallMaturity}`)}
                  </Badge>
                </div>
              </CardHeader>
              <CardBody>
                <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
                  {sc.dimensions.map(dim => (
                    <div key={dim.dimension} className="bg-surface/50 rounded-md p-3 border border-edge/50">
                      <div className="flex items-center justify-between mb-1">
                        <p className="text-xs font-medium text-heading">
                          {t(`governance.executive.scorecards${dim.dimension.charAt(0).toUpperCase()}${dim.dimension.slice(1)}`)}
                        </p>
                        <Badge variant={maturityBadgeVariant(dim.level)}>
                          {t(`governance.maturity.${dim.level}`)}
                        </Badge>
                      </div>
                      <div className="flex items-center justify-between mb-1">
                        <span className="text-xs text-muted">
                          {dim.score}/{dim.maxScore}
                        </span>
                      </div>
                      <div className="w-full bg-surface rounded-full h-1.5 mb-2">
                        <div
                          className={`${scoreBarColor(dim.score, dim.maxScore)} rounded-full h-1.5 transition-all`}
                          style={{ width: `${dim.maxScore === 0 ? 0 : (dim.score / dim.maxScore) * 100}%` }}
                        />
                      </div>
                      <p className="text-xs text-muted">{dim.explanation}</p>
                    </div>
                  ))}
                </div>
              </CardBody>
            </Card>
          ))}
        </div>
      )}
    </PageContainer>
  );
}
