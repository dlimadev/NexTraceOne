import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation } from '@tanstack/react-query';
import { Activity } from 'lucide-react';
import { EmptyState } from '../../../components/EmptyState';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button, TextField, Select, SearchInput, TextArea } from '../../../shared/ui';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

interface SubmitScoreRequest {
  teamId: string;
  teamName: string;
  serviceId?: string;
  period: string;
  cycleTimeHours: number;
  deploymentFrequencyPerWeek: number;
  cognitiveLoadScore: number;
  toilPercentage: number;
  notes?: string;
}

interface ScoreResponse {
  scoreId: string;
  teamId: string;
  teamName: string;
  serviceId?: string;
  period: string;
  cycleTimeHours: number;
  deploymentFrequencyPerWeek: number;
  cognitiveLoadScore: number;
  toilPercentage: number;
  overallScore: number;
  scoreLevel: string;
  notes?: string;
  computedAt: string;
}

interface ListScoresResponse {
  items: ScoreResponse[];
}

// ── Constants ──────────────────────────────────────────────────────────────

const PERIODS = ['2026-Q1', '2026-Q2', '2026-Q3', '2026-Q4'];

const SCORE_LEVEL_VARIANT: Record<string, 'success' | 'warning' | 'danger' | 'secondary'> = {
  Excellent: 'success',
  Good: 'secondary',
  Fair: 'warning',
  Poor: 'danger',
};

const SCORE_LEVEL_COLOR: Record<string, string> = {
  Excellent: 'text-success',
  Good: 'text-accent',
  Fair: 'text-warning',
  Poor: 'text-critical',
};

// ── Sub-components ─────────────────────────────────────────────────────────

function ScoreResultCard({ score }: { score: ScoreResponse }) {
  const { t } = useTranslation();

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between flex-wrap gap-2">
          <span className="text-sm font-medium text-heading">{score.teamName}</span>
          <Badge variant={SCORE_LEVEL_VARIANT[score.scoreLevel] ?? 'secondary'}>
            {t(`developerExperienceScore.${score.scoreLevel.toLowerCase()}` as never, {
              defaultValue: score.scoreLevel,
            })}
          </Badge>
        </div>
      </CardHeader>
      <CardBody className="space-y-4">
        <div className="flex items-end gap-2">
          <span className={`text-5xl font-bold ${SCORE_LEVEL_COLOR[score.scoreLevel] ?? 'text-heading'}`}>
            {score.overallScore}
          </span>
          <span className="text-sm text-muted mb-1">
            / 100 — {t('developerExperienceScore.overallScore')}
          </span>
        </div>

        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <div className="rounded bg-elevated border border-edge p-3 text-center">
            <p className="text-lg font-semibold text-heading">{score.cycleTimeHours}h</p>
            <p className="text-xs text-muted">{t('developerExperienceScore.cycleTime')}</p>
          </div>
          <div className="rounded bg-elevated border border-edge p-3 text-center">
            <p className="text-lg font-semibold text-heading">{score.deploymentFrequencyPerWeek}</p>
            <p className="text-xs text-muted">{t('developerExperienceScore.deploysPerWeek')}</p>
          </div>
          <div className="rounded bg-elevated border border-edge p-3 text-center">
            <p className="text-lg font-semibold text-heading">{score.cognitiveLoadScore}</p>
            <p className="text-xs text-muted">{t('developerExperienceScore.cognitiveLoad')}</p>
          </div>
          <div className="rounded bg-elevated border border-edge p-3 text-center">
            <p className="text-lg font-semibold text-heading">{score.toilPercentage}%</p>
            <p className="text-xs text-muted">{t('developerExperienceScore.toil')}</p>
          </div>
        </div>

        <p className="text-xs text-muted">
          {t('developerExperienceScore.computedAt')}: {new Date(score.computedAt).toLocaleString()}
        </p>
      </CardBody>
    </Card>
  );
}

// ── Page ───────────────────────────────────────────────────────────────────

const initialForm: SubmitScoreRequest = {
  teamId: '',
  teamName: '',
  serviceId: '',
  period: '2026-Q1',
  cycleTimeHours: 0,
  deploymentFrequencyPerWeek: 0,
  cognitiveLoadScore: 0,
  toilPercentage: 0,
  notes: '',
};

export function DeveloperExperienceScorePage() {
  const { t } = useTranslation();
  const [form, setForm] = useState<SubmitScoreRequest>(initialForm);
  const [filterTeamId, setFilterTeamId] = useState('');
  const [lastResult, setLastResult] = useState<ScoreResponse | null>(null);

  const { data: scoresData, isLoading: isListLoading, isError, refetch } = useQuery({
    queryKey: ['developer-experience-scores', filterTeamId],
    queryFn: () =>
      client
        .get<ListScoresResponse>('/developer-experience/scores', {
          params: { teamId: filterTeamId || undefined, page: 1, pageSize: 20 },
        })
        .then((r) => r.data),
  });

  const createMutation = useMutation({
    mutationFn: (data: SubmitScoreRequest) =>
      client
        .post<ScoreResponse>('/developer-experience/scores', data)
        .then((r) => r.data),
    onSuccess: (data) => {
      setLastResult(data);
      void refetch();
    },
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    createMutation.mutate(form);
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('developerExperienceScore.title')}
        subtitle={t('developerExperienceScore.subtitle')}
        icon={<Activity size={24} />}
      />

      <PageSection title={t('developerExperienceScore.submitScore')}>
        <Card>
          <CardBody>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <TextField
                  label={t('developerExperienceScore.teamId')}
                  value={form.teamId}
                  onChange={(e) => setForm((f) => ({ ...f, teamId: e.target.value }))}
                  required
                />
                <TextField
                  label={t('developerExperienceScore.teamName')}
                  value={form.teamName}
                  onChange={(e) => setForm((f) => ({ ...f, teamName: e.target.value }))}
                  required
                />
                <Select
                  label={t('developerExperienceScore.period')}
                  options={PERIODS.map((p) => ({ value: p, label: p }))}
                  value={form.period}
                  onChange={(e) => setForm((f) => ({ ...f, period: e.target.value }))}
                />
                <TextField
                  label={t('developerExperienceScore.cycleTimeHours')}
                  type="number"
                  value={form.cycleTimeHours}
                  min={0}
                  step={0.5}
                  onChange={(e) => setForm((f) => ({ ...f, cycleTimeHours: Number(e.target.value) }))}
                />
                <TextField
                  label={t('developerExperienceScore.deploymentFrequency')}
                  type="number"
                  value={form.deploymentFrequencyPerWeek}
                  min={0}
                  step={0.1}
                  onChange={(e) => setForm((f) => ({ ...f, deploymentFrequencyPerWeek: Number(e.target.value) }))}
                />
                <TextField
                  label={t('developerExperienceScore.cognitiveLoadScore')}
                  type="number"
                  value={form.cognitiveLoadScore}
                  min={0}
                  max={10}
                  step={0.1}
                  onChange={(e) => setForm((f) => ({ ...f, cognitiveLoadScore: Number(e.target.value) }))}
                />
                <TextField
                  label={t('developerExperienceScore.toilPercentage')}
                  type="number"
                  value={form.toilPercentage}
                  min={0}
                  max={100}
                  onChange={(e) => setForm((f) => ({ ...f, toilPercentage: Number(e.target.value) }))}
                />
                <TextArea
                  label={t('developerExperienceScore.notes')}
                  value={form.notes}
                  rows={2}
                  onChange={(e) => setForm((f) => ({ ...f, notes: e.target.value }))}
                />
              </div>

              {createMutation.isError && (
                <p className="text-sm text-critical">{t('developerExperienceScore.createError')}</p>
              )}

              <div className="flex justify-end">
                <Button type="submit" loading={createMutation.isPending}>
                  {createMutation.isPending ? t('developerExperienceScore.loading') : t('developerExperienceScore.submit')}
                </Button>
              </div>
            </form>
          </CardBody>
        </Card>
      </PageSection>

      {lastResult && (
        <PageSection title={t('developerExperienceScore.createSuccess')}>
          <ScoreResultCard score={lastResult} />
        </PageSection>
      )}

      <PageSection title={t('developerExperienceScore.recentScores')}>
        <div className="mb-3 flex gap-2">
          <SearchInput
            placeholder={t('developerExperienceScore.teamId')}
            value={filterTeamId}
            onChange={(e) => setFilterTeamId(e.target.value)}
            aria-label={t('developerExperienceScore.teamId')}
          />
        </div>

        {isError ? (
          <PageErrorState />
        ) : isListLoading ? (
          <PageLoadingState message="..." />
        ) : !scoresData?.items?.length ? (
          <EmptyState
            icon={<Activity size={20} />}
            title={t('developerExperienceScore.noScores', 'No scores found')}
            description={t('developerExperienceScore.noScoresHint', 'Scores will appear here once teams are evaluated.')}
            size="compact"
          />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-edge text-left">
                  <th className="pb-2 pr-4 text-xs font-medium text-muted">
                    {t('developerExperienceScore.teamName')}
                  </th>
                  <th className="pb-2 pr-4 text-xs font-medium text-muted">
                    {t('developerExperienceScore.period')}
                  </th>
                  <th className="pb-2 pr-4 text-xs font-medium text-muted">
                    {t('developerExperienceScore.overallScore')}
                  </th>
                  <th className="pb-2 pr-4 text-xs font-medium text-muted">
                    {t('developerExperienceScore.scoreLevel')}
                  </th>
                  <th className="pb-2 text-xs font-medium text-muted">
                    {t('developerExperienceScore.computedAt')}
                  </th>
                </tr>
              </thead>
              <tbody>
                {scoresData.items.map((score) => (
                  <tr
                    key={score.scoreId}
                    className="border-b border-edge hover:bg-elevated/50 transition-colors"
                  >
                    <td className="py-2 pr-4 font-medium text-heading">{score.teamName}</td>
                    <td className="py-2 pr-4 text-muted">{score.period}</td>
                    <td className="py-2 pr-4 font-semibold text-heading">{score.overallScore}</td>
                    <td className="py-2 pr-4">
                      <Badge variant={SCORE_LEVEL_VARIANT[score.scoreLevel] ?? 'secondary'}>
                        {t(`developerExperienceScore.${score.scoreLevel.toLowerCase()}` as never, {
                          defaultValue: score.scoreLevel,
                        })}
                      </Badge>
                    </td>
                    <td className="py-2 text-xs text-muted">
                      {new Date(score.computedAt).toLocaleString()}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </PageSection>
    </PageContainer>
  );
}
