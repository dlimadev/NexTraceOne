import { useTranslation } from 'react-i18next';
import { useQuery } from '@tanstack/react-query';
import { BarChart2, CheckCircle, XCircle, AlertTriangle, Info } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import { EmptyState } from '../../../../components/EmptyState';
import { LoadingState, ErrorState } from '../../shared/components/StateIndicators';
import { contractsApi } from '../../api/contracts';
import type { ContractScorecard } from '../../types/domain';

interface ScorecardSectionProps {
  contractVersionId: string;
  className?: string;
}

/** Formata um score de 0.0-1.0 como percentagem inteira. */
function toPercent(score: number): number {
  return Math.round(score * 100);
}

/** Cor da barra / badge baseada no score (0-1). */
function scoreColor(score: number): string {
  if (score >= 0.8) return 'text-mint';
  if (score >= 0.6) return 'text-cyan';
  if (score >= 0.4) return 'text-warning';
  return 'text-danger';
}

function scoreBg(score: number): string {
  if (score >= 0.8) return 'bg-mint';
  if (score >= 0.6) return 'bg-cyan';
  if (score >= 0.4) return 'bg-warning';
  return 'bg-danger';
}

function scoreBgLight(score: number): string {
  if (score >= 0.8) return 'bg-mint/10';
  if (score >= 0.6) return 'bg-cyan/10';
  if (score >= 0.4) return 'bg-warning/10';
  return 'bg-danger/10';
}

/** Ícone de status por threshold. */
function ScoreIcon({ score }: { score: number }) {
  if (score >= 0.8) return <CheckCircle size={14} className="text-mint flex-shrink-0" />;
  if (score >= 0.6) return <Info size={14} className="text-cyan flex-shrink-0" />;
  if (score >= 0.4) return <AlertTriangle size={14} className="text-warning flex-shrink-0" />;
  return <XCircle size={14} className="text-danger flex-shrink-0" />;
}

interface ScoreDimensionProps {
  label: string;
  score: number;
  justification: string;
}

/** Linha de dimensão de score com barra de progresso. */
function ScoreDimension({ label, score, justification }: ScoreDimensionProps) {
  const pct = toPercent(score);
  return (
    <div className="space-y-1.5">
      <div className="flex items-center justify-between gap-2">
        <div className="flex items-center gap-2 min-w-0">
          <ScoreIcon score={score} />
          <span className="text-xs font-medium text-heading truncate">{label}</span>
        </div>
        <span className={`text-xs font-bold tabular-nums flex-shrink-0 ${scoreColor(score)}`}>
          {pct}%
        </span>
      </div>
      <div className="h-1.5 bg-elevated rounded-full overflow-hidden">
        <div
          className={`h-full rounded-full transition-all duration-500 ${scoreBg(score)}`}
          style={{ width: `${pct}%` }}
        />
      </div>
      {justification && (
        <p className="text-[11px] text-muted leading-relaxed">{justification}</p>
      )}
    </div>
  );
}

interface OverallGaugeProps {
  score: number;
}

/** Gauge circular simplificado para o score geral. */
function OverallGauge({ score }: OverallGaugeProps) {
  const pct = toPercent(score);
  const radius = 40;
  const circumference = 2 * Math.PI * radius;
  const offset = circumference - (pct / 100) * circumference;

  return (
    <div className="flex flex-col items-center gap-2">
      <div className="relative w-28 h-28">
        <svg className="w-full h-full -rotate-90" viewBox="0 0 100 100">
          <circle
            cx="50" cy="50" r={radius}
            fill="none"
            strokeWidth="10"
            className="stroke-elevated"
          />
          <circle
            cx="50" cy="50" r={radius}
            fill="none"
            strokeWidth="10"
            strokeLinecap="round"
            strokeDasharray={circumference}
            strokeDashoffset={offset}
            className={`transition-all duration-700 ${
              score >= 0.8 ? 'stroke-mint' :
              score >= 0.6 ? 'stroke-cyan' :
              score >= 0.4 ? 'stroke-warning' :
              'stroke-danger'
            }`}
          />
        </svg>
        <div className="absolute inset-0 flex flex-col items-center justify-center">
          <span className={`text-2xl font-bold tabular-nums ${scoreColor(score)}`}>{pct}</span>
          <span className="text-[10px] text-muted">/ 100</span>
        </div>
      </div>
    </div>
  );
}

interface MetricBadgeProps {
  label: string;
  value: string | number;
  active?: boolean;
}

function MetricBadge({ label, value, active }: MetricBadgeProps) {
  return (
    <div className={`rounded-lg px-3 py-2 border text-center ${active ? 'bg-mint/10 border-mint/25' : 'bg-elevated border-edge'}`}>
      <div className={`text-sm font-bold tabular-nums ${active ? 'text-mint' : 'text-heading'}`}>{value}</div>
      <div className="text-[10px] text-muted mt-0.5">{label}</div>
    </div>
  );
}

/**
 * Secção de scorecard técnico de uma versão de contrato.
 * Consome GET /api/v1/contracts/{contractVersionId}/scorecard.
 * Mostra score geral + 4 dimensões + métricas estruturais.
 */
export function ScorecardSection({ contractVersionId, className = '' }: ScorecardSectionProps) {
  const { t } = useTranslation();

  const { data, isLoading, isError, refetch } = useQuery({
    queryKey: ['contract-scorecard', contractVersionId],
    queryFn: () => contractsApi.getScorecard(contractVersionId),
    enabled: !!contractVersionId,
  });

  if (isLoading) return <LoadingState />;
  if (isError) return <ErrorState onRetry={() => refetch()} />;

  if (!data) {
    return (
      <EmptyState
        icon={<BarChart2 size={24} className="text-muted" />}
        title={t('contracts.scorecard.empty.title', 'Scorecard not available')}
        description={t('contracts.scorecard.empty.description', 'No scorecard data could be generated for this contract version.')}
      />
    );
  }

  const scorecard: ContractScorecard = data;

  return (
    <div className={`space-y-6 ${className}`}>
      {/* ── Overall Score ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center gap-2">
            <BarChart2 size={14} className="text-accent" />
            <h3 className="text-sm font-semibold text-heading">
              {t('contracts.scorecard.overall.title', 'Overall Score')}
            </h3>
          </div>
        </CardHeader>
        <CardBody>
          <div className="flex flex-col md:flex-row items-center gap-8">
            <OverallGauge score={scorecard.overallScore} />
            <div className={`flex-1 rounded-xl p-4 ${scoreBgLight(scorecard.overallScore)}`}>
              <p className="text-xs text-body leading-relaxed">
                {scorecard.overallScore >= 0.8
                  ? t('contracts.scorecard.overall.high', 'This contract has a high quality score. It is well-documented, structurally complete, and follows best practices.')
                  : scorecard.overallScore >= 0.6
                  ? t('contracts.scorecard.overall.medium', 'This contract meets baseline quality standards but has opportunities for improvement.')
                  : t('contracts.scorecard.overall.low', 'This contract has quality gaps that should be addressed before promotion to production.')}
              </p>
              {/* Structural metrics */}
              <div className="grid grid-cols-3 gap-3 mt-4">
                <MetricBadge
                  label={t('contracts.scorecard.metrics.operations', 'Operations')}
                  value={scorecard.operationCount}
                />
                <MetricBadge
                  label={t('contracts.scorecard.metrics.schemas', 'Schemas')}
                  value={scorecard.schemaCount}
                />
                <MetricBadge
                  label={t('contracts.scorecard.metrics.security', 'Security')}
                  value={scorecard.hasSecurityDefinitions ? t('common.yes', 'Yes') : t('common.no', 'No')}
                  active={scorecard.hasSecurityDefinitions}
                />
                <MetricBadge
                  label={t('contracts.scorecard.metrics.examples', 'Examples')}
                  value={scorecard.hasExamples ? t('common.yes', 'Yes') : t('common.no', 'No')}
                  active={scorecard.hasExamples}
                />
                <MetricBadge
                  label={t('contracts.scorecard.metrics.descriptions', 'Descriptions')}
                  value={scorecard.hasDescriptions ? t('common.yes', 'Yes') : t('common.no', 'No')}
                  active={scorecard.hasDescriptions}
                />
              </div>
            </div>
          </div>
        </CardBody>
      </Card>

      {/* ── Score Dimensions ── */}
      <Card>
        <CardHeader>
          <h3 className="text-sm font-semibold text-heading">
            {t('contracts.scorecard.dimensions.title', 'Score Dimensions')}
          </h3>
        </CardHeader>
        <CardBody>
          <div className="space-y-5">
            <ScoreDimension
              label={t('contracts.scorecard.dimensions.quality', 'Quality')}
              score={scorecard.qualityScore}
              justification={scorecard.qualityJustification}
            />
            <ScoreDimension
              label={t('contracts.scorecard.dimensions.completeness', 'Completeness')}
              score={scorecard.completenessScore}
              justification={scorecard.completenessJustification}
            />
            <ScoreDimension
              label={t('contracts.scorecard.dimensions.compatibility', 'Compatibility')}
              score={scorecard.compatibilityScore}
              justification={scorecard.compatibilityJustification}
            />
            <ScoreDimension
              label={t('contracts.scorecard.dimensions.risk', 'Risk')}
              score={1 - scorecard.riskScore}
              justification={scorecard.riskJustification}
            />
          </div>
        </CardBody>
      </Card>
    </div>
  );
}
