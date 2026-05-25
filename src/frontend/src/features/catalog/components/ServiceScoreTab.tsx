/**
 * ServiceScoreTab — Score consolidado de maturidade, SRE e Developer Experience.
 *
 * Centraliza as três dimensões de score do serviço em uma única visualização:
 * - Maturity Score (nível 1–5)
 * - SRE Score (operational excellence)
 * - Developer Experience Score
 *
 * @pillar Service 360° — Visão consolidada de qualidade do serviço
 */
import { useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { Award, Activity, BookMarked, ExternalLink, TrendingUp, TrendingDown, Minus } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { Link } from 'react-router-dom';
import client from '../../../api/client';

interface Props {
  serviceId: string;
}

interface ServiceScoreResponse {
  maturityScore: number;       // 0–100
  maturityLevel: number;       // 1–5
  maturityLabel: string;
  sreScore: number;            // 0–100
  dxScore: number;             // 0–100
  computedAt: string;
  trend: 'up' | 'down' | 'stable';
}

async function fetchServiceScore(serviceId: string): Promise<ServiceScoreResponse> {
  const r = await client.get<ServiceScoreResponse>(`/services/${serviceId}/score`);
  return r.data;
}

function ScoreRing({ score, label, color }: { score: number; label: string; color: string }) {
  const radius = 36;
  const circumference = 2 * Math.PI * radius;
  const offset = circumference - (score / 100) * circumference;

  return (
    <div className="flex flex-col items-center gap-2">
      <div className="relative w-24 h-24">
        <svg className="w-full h-full -rotate-90" viewBox="0 0 96 96">
          <circle cx="48" cy="48" r={radius} fill="none" stroke="var(--c-edge)" strokeWidth="8" />
          <circle
            cx="48"
            cy="48"
            r={radius}
            fill="none"
            stroke={color}
            strokeWidth="8"
            strokeDasharray={circumference}
            strokeDashoffset={offset}
            strokeLinecap="round"
            className="transition-all duration-700"
          />
        </svg>
        <div className="absolute inset-0 flex items-center justify-center">
          <span className="text-xl font-bold text-heading">{score}</span>
        </div>
      </div>
      <span className="text-xs text-muted font-medium">{label}</span>
    </div>
  );
}

function maturityColor(level: number): string {
  if (level >= 4) return '#22c55e';
  if (level >= 3) return '#3b82f6';
  if (level >= 2) return '#f59e0b';
  return '#ef4444';
}

function scoreColor(score: number): string {
  if (score >= 80) return '#22c55e';
  if (score >= 60) return '#3b82f6';
  if (score >= 40) return '#f59e0b';
  return '#ef4444';
}

export function ServiceScoreTab({ serviceId }: Props) {
  const { t } = useTranslation();

  const { data: score, isLoading } = useQuery({
    queryKey: ['service-score', serviceId],
    queryFn: () => fetchServiceScore(serviceId),
    staleTime: 5 * 60_000,
  });

  const TrendIcon = score?.trend === 'up'
    ? TrendingUp
    : score?.trend === 'down'
      ? TrendingDown
      : Minus;

  const trendColor = score?.trend === 'up'
    ? 'text-success'
    : score?.trend === 'down'
      ? 'text-critical'
      : 'text-muted';

  return (
    <div className="flex flex-col gap-6">
      {/* ── Scores visuais ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <Award size={16} className="text-accent" />
              <h3 className="text-sm font-semibold text-heading">
                {t('serviceDetail.score.title', 'Service Score')}
              </h3>
            </div>
            {score && (
              <div className={`flex items-center gap-1 text-xs ${trendColor}`}>
                <TrendIcon size={13} />
                {t(`serviceDetail.score.trend.${score.trend}`, score.trend)}
              </div>
            )}
          </div>
        </CardHeader>
        <CardBody>
          {isLoading ? (
            <div className="flex justify-center gap-8 py-4 animate-pulse">
              {[...Array(3)].map((_, i) => (
                <div key={i} className="w-24 h-28 bg-elevated rounded-lg" />
              ))}
            </div>
          ) : score ? (
            <div className="flex justify-around items-start py-4 flex-wrap gap-6">
              <ScoreRing
                score={score.maturityScore}
                label={t('serviceDetail.score.maturity', 'Maturity')}
                color={maturityColor(score.maturityLevel)}
              />
              <ScoreRing
                score={score.sreScore}
                label={t('serviceDetail.score.sre', 'SRE Score')}
                color={scoreColor(score.sreScore)}
              />
              <ScoreRing
                score={score.dxScore}
                label={t('serviceDetail.score.dx', 'DX Score')}
                color={scoreColor(score.dxScore)}
              />
            </div>
          ) : (
            <div className="py-8 text-center text-sm text-muted">
              {t('serviceDetail.score.noData', 'Score data not available.')}
            </div>
          )}

          {/* ── Nível de maturidade ── */}
          {score && (
            <div className="mt-4 pt-4 border-t border-edge flex items-center justify-between">
              <div>
                <p className="text-xs text-muted">{t('serviceDetail.score.maturityLevel', 'Maturity Level')}</p>
                <p className="text-sm font-semibold text-heading mt-0.5">
                  {t('serviceDetail.score.level', 'Level')} {score.maturityLevel} — {score.maturityLabel}
                </p>
              </div>
              <div className="flex gap-1">
                {[1, 2, 3, 4, 5].map(l => (
                  <div
                    key={l}
                    className={`w-5 h-2 rounded-sm ${l <= score.maturityLevel ? 'bg-accent' : 'bg-edge'}`}
                  />
                ))}
              </div>
            </div>
          )}
        </CardBody>
      </Card>

      {/* ── Checklist de maturidade ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <div className="flex items-center gap-2">
              <TrendingUp size={16} className="text-accent" />
              <h3 className="text-sm font-semibold text-heading">
                {t('serviceDetail.score.improve', 'How to Improve')}
              </h3>
            </div>
            <Link
              to={`/services/maturity?serviceId=${serviceId}`}
              className="flex items-center gap-1 text-xs text-accent hover:underline"
            >
              <ExternalLink size={11} />
              {t('serviceDetail.score.fullScorecard', 'Full Scorecard')}
            </Link>
          </div>
        </CardHeader>
        <CardBody>
          <div className="space-y-2 text-sm text-muted">
            <p>{t('serviceDetail.score.improveTip', 'Review the full scorecard to see specific criteria and next steps for each dimension.')}</p>
          </div>
          <div className="flex gap-3 flex-wrap mt-4">
            <Link
              to={`/services/maturity?serviceId=${serviceId}`}
              className="flex items-center gap-1.5 text-xs text-accent border border-accent/30 rounded px-3 py-1.5 hover:bg-accent/5 transition-colors"
            >
              <Award size={12} />
              {t('serviceDetail.score.maturityScorecard', 'Maturity Scorecard')}
            </Link>
            <Link
              to={`/catalog/developer-experience-score?serviceId=${serviceId}`}
              className="flex items-center gap-1.5 text-xs text-accent border border-accent/30 rounded px-3 py-1.5 hover:bg-accent/5 transition-colors"
            >
              <BookMarked size={12} />
              {t('serviceDetail.score.dxDetail', 'DX Detail')}
            </Link>
            <Link
              to={`/operations/service-maturity-sre?serviceId=${serviceId}`}
              className="flex items-center gap-1.5 text-xs text-accent border border-accent/30 rounded px-3 py-1.5 hover:bg-accent/5 transition-colors"
            >
              <Activity size={12} />
              {t('serviceDetail.score.sreDetail', 'SRE Detail')}
            </Link>
          </div>
        </CardBody>
      </Card>
    </div>
  );
}
