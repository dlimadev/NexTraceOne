import { useTranslation } from 'react-i18next';
import {
  TrendingUp,
  TrendingDown,
  Minus,
  CheckCircle,
  Clock,
  Users,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { StatCard } from '../../../components/StatCard';
import { PageContainer } from '../../../components/shell';

/**
 * Página de value tracking — marcos de valor.
 *
 * Mostra progressão dos utilizadores em atingir marcos de valor do produto.
 * Responde: quanto tempo até o primeiro valor? Quais milestones são mais atingidos?
 * Qual a progressão de valor por persona?
 *
 * @see docs/PRODUCT-VISION.md — marcos de valor do produto
 */

/* ── Dados de demonstração (MVP) ── */

const mockMilestones = [
  { type: 'FirstSearchSuccess', completionRate: 94.2, avgTimeMinutes: 3.2, usersReached: 221, trend: 'Stable' as const },
  { type: 'FirstServiceLookup', completionRate: 88.5, avgTimeMinutes: 5.8, usersReached: 208, trend: 'Improving' as const },
  { type: 'FirstContractView', completionRate: 82.1, avgTimeMinutes: 8.4, usersReached: 193, trend: 'Stable' as const },
  { type: 'FirstSourceOfTruthUsed', completionRate: 86.2, avgTimeMinutes: 7.2, usersReached: 202, trend: 'Improving' as const },
  { type: 'FirstExecutiveOverviewConsumed', completionRate: 78.6, avgTimeMinutes: 6.1, usersReached: 184, trend: 'Improving' as const },
  { type: 'FirstAiUsefulInteraction', completionRate: 72.4, avgTimeMinutes: 12.3, usersReached: 170, trend: 'Improving' as const },
  { type: 'FirstIncidentInvestigation', completionRate: 58.1, avgTimeMinutes: 28.6, usersReached: 136, trend: 'Stable' as const },
  { type: 'FirstContractDraftCreated', completionRate: 52.3, avgTimeMinutes: 42.5, usersReached: 123, trend: 'Improving' as const },
  { type: 'FirstReliabilityViewed', completionRate: 48.7, avgTimeMinutes: 22.8, usersReached: 114, trend: 'Declining' as const },
  { type: 'FirstReportGenerated', completionRate: 45.2, avgTimeMinutes: 48.0, usersReached: 106, trend: 'Stable' as const },
  { type: 'FirstMitigationCompleted', completionRate: 42.3, avgTimeMinutes: 95.4, usersReached: 99, trend: 'Stable' as const },
  { type: 'FirstRunbookConsulted', completionRate: 38.4, avgTimeMinutes: 35.2, usersReached: 90, trend: 'Declining' as const },
  { type: 'FirstContractPublished', completionRate: 34.8, avgTimeMinutes: 168.0, usersReached: 82, trend: 'Improving' as const },
  { type: 'FirstAutomationCreated', completionRate: 28.1, avgTimeMinutes: 180.0, usersReached: 66, trend: 'Improving' as const },
  { type: 'FirstEvidenceExported', completionRate: 18.6, avgTimeMinutes: 210.0, usersReached: 44, trend: 'Stable' as const },
];

function trendIcon(trend: 'Improving' | 'Stable' | 'Declining') {
  switch (trend) {
    case 'Improving': return <TrendingUp size={14} className="text-emerald-400" />;
    case 'Declining': return <TrendingDown size={14} className="text-red-400" />;
    default: return <Minus size={14} className="text-zinc-400" />;
  }
}

function completionColor(rate: number): string {
  if (rate >= 75) return 'bg-emerald-500';
  if (rate >= 50) return 'bg-accent';
  if (rate >= 30) return 'bg-amber-500';
  return 'bg-red-500';
}

function formatTime(minutes: number): string {
  if (minutes < 60) return `${minutes.toFixed(0)}m`;
  const hours = Math.floor(minutes / 60);
  const mins = Math.round(minutes % 60);
  return mins > 0 ? `${hours}h ${mins}m` : `${hours}h`;
}

export function ValueTrackingPage() {
  const { t } = useTranslation();

  const avgCompletionRate = mockMilestones.reduce((sum, m) => sum + m.completionRate, 0) / mockMilestones.length;

  return (
    <PageContainer>
      {/* Header */}
      <div className="mb-8">
        <h1 className="text-2xl font-bold text-white">{t('analytics.value.title')}</h1>
        <p className="text-zinc-400 mt-1">{t('analytics.value.subtitle')}</p>
      </div>

      {/* Summary cards */}
      <div className="grid grid-cols-2 md:grid-cols-4 gap-4 mb-8">
        <StatCard
          title={t('analytics.timeToFirstValue')}
          value="18.5 min"
          icon={<Clock size={20} />}
          color="text-accent"
          trend={{ direction: 'down', label: t('analytics.trendImproving') }}
        />
        <StatCard
          title={t('analytics.timeToCoreValue')}
          value="2h 22m"
          icon={<CheckCircle size={20} />}
          color="text-emerald-400"
          trend={{ direction: 'down', label: t('analytics.trendImproving') }}
        />
        <StatCard
          title={t('analytics.value.avgCompletion')}
          value={`${avgCompletionRate.toFixed(1)}%`}
          icon={<TrendingUp size={20} />}
          color="text-blue-400"
        />
        <StatCard
          title={t('analytics.value.totalMilestones')}
          value={mockMilestones.length}
          icon={<Users size={20} />}
          color="text-amber-400"
        />
      </div>

      {/* Milestones list */}
      <Card>
        <CardHeader>
          <span className="font-semibold text-white">{t('analytics.value.milestoneProgress')}</span>
        </CardHeader>
        <CardBody>
          <div className="space-y-4">
            {mockMilestones.map((m) => (
              <div key={m.type} className="flex flex-col md:flex-row md:items-center gap-3">
                {/* Milestone name & trend */}
                <div className="md:w-72 flex items-center gap-2">
                  <CheckCircle size={16} className={m.completionRate >= 50 ? 'text-emerald-400' : 'text-zinc-600'} />
                  <span className="text-sm text-white">{t(`analytics.milestone.${m.type}`)}</span>
                  {trendIcon(m.trend)}
                </div>

                {/* Progress bar */}
                <div className="flex-1 flex items-center gap-3">
                  <div className="flex-1 h-2 rounded-full bg-zinc-800 overflow-hidden">
                    <div
                      className={`h-full rounded-full ${completionColor(m.completionRate)} transition-all`}
                      style={{ width: `${m.completionRate}%` }}
                    />
                  </div>
                  <span className="text-sm text-white font-medium w-14 text-right">{m.completionRate}%</span>
                </div>

                {/* Stats */}
                <div className="flex items-center gap-4 text-sm md:w-48">
                  <span className="text-zinc-400 flex items-center gap-1">
                    <Clock size={12} />
                    {formatTime(m.avgTimeMinutes)}
                  </span>
                  <span className="text-zinc-400 flex items-center gap-1">
                    <Users size={12} />
                    {m.usersReached}
                  </span>
                </div>
              </div>
            ))}
          </div>
        </CardBody>
      </Card>
    </PageContainer>
  );
}
