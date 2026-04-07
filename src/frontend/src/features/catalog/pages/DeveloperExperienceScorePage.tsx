import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation } from '@tanstack/react-query';
import { Activity } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
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
  Excellent: 'text-green-600 dark:text-green-400',
  Good: 'text-blue-600 dark:text-blue-400',
  Fair: 'text-yellow-600 dark:text-yellow-400',
  Poor: 'text-red-600 dark:text-red-400',
};

// ── Sub-components ─────────────────────────────────────────────────────────

function ScoreResultCard({ score }: { score: ScoreResponse }) {
  const { t } = useTranslation();

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between flex-wrap gap-2">
          <span className="text-sm font-medium text-gray-900 dark:text-white">{score.teamName}</span>
          <Badge variant={SCORE_LEVEL_VARIANT[score.scoreLevel] ?? 'secondary'}>
            {t(`developerExperienceScore.${score.scoreLevel.toLowerCase()}` as never, {
              defaultValue: score.scoreLevel,
            })}
          </Badge>
        </div>
      </CardHeader>
      <CardBody className="space-y-4">
        <div className="flex items-end gap-2">
          <span className={`text-5xl font-bold ${SCORE_LEVEL_COLOR[score.scoreLevel] ?? 'text-gray-900 dark:text-white'}`}>
            {score.overallScore}
          </span>
          <span className="text-sm text-gray-500 dark:text-gray-400 mb-1">
            / 100 — {t('developerExperienceScore.overallScore')}
          </span>
        </div>

        <div className="grid grid-cols-2 gap-3 sm:grid-cols-4">
          <div className="rounded bg-gray-50 dark:bg-gray-800 p-3 text-center">
            <p className="text-lg font-semibold text-gray-900 dark:text-white">{score.cycleTimeHours}h</p>
            <p className="text-xs text-gray-500 dark:text-gray-400">{t('developerExperienceScore.cycleTime')}</p>
          </div>
          <div className="rounded bg-gray-50 dark:bg-gray-800 p-3 text-center">
            <p className="text-lg font-semibold text-gray-900 dark:text-white">{score.deploymentFrequencyPerWeek}</p>
            <p className="text-xs text-gray-500 dark:text-gray-400">{t('developerExperienceScore.deploysPerWeek')}</p>
          </div>
          <div className="rounded bg-gray-50 dark:bg-gray-800 p-3 text-center">
            <p className="text-lg font-semibold text-gray-900 dark:text-white">{score.cognitiveLoadScore}</p>
            <p className="text-xs text-gray-500 dark:text-gray-400">{t('developerExperienceScore.cognitiveLoad')}</p>
          </div>
          <div className="rounded bg-gray-50 dark:bg-gray-800 p-3 text-center">
            <p className="text-lg font-semibold text-gray-900 dark:text-white">{score.toilPercentage}%</p>
            <p className="text-xs text-gray-500 dark:text-gray-400">{t('developerExperienceScore.toil')}</p>
          </div>
        </div>

        <p className="text-xs text-gray-400">
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

  const inputClass =
    'w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-400';
  const labelClass = 'mb-1 block text-xs font-medium text-gray-700 dark:text-gray-300';

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
                <div>
                  <label className={labelClass}>{t('developerExperienceScore.teamId')}</label>
                  <input
                    type="text"
                    className={inputClass}
                    value={form.teamId}
                    onChange={(e) => setForm((f) => ({ ...f, teamId: e.target.value }))}
                    required
                  />
                </div>
                <div>
                  <label className={labelClass}>{t('developerExperienceScore.teamName')}</label>
                  <input
                    type="text"
                    className={inputClass}
                    value={form.teamName}
                    onChange={(e) => setForm((f) => ({ ...f, teamName: e.target.value }))}
                    required
                  />
                </div>
                <div>
                  <label className={labelClass}>{t('developerExperienceScore.period')}</label>
                  <select
                    className={inputClass}
                    value={form.period}
                    onChange={(e) => setForm((f) => ({ ...f, period: e.target.value }))}
                  >
                    {PERIODS.map((p) => (
                      <option key={p} value={p}>{p}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className={labelClass}>{t('developerExperienceScore.cycleTimeHours')}</label>
                  <input
                    type="number"
                    className={inputClass}
                    value={form.cycleTimeHours}
                    min={0}
                    step={0.5}
                    onChange={(e) => setForm((f) => ({ ...f, cycleTimeHours: Number(e.target.value) }))}
                  />
                </div>
                <div>
                  <label className={labelClass}>{t('developerExperienceScore.deploymentFrequency')}</label>
                  <input
                    type="number"
                    className={inputClass}
                    value={form.deploymentFrequencyPerWeek}
                    min={0}
                    step={0.1}
                    onChange={(e) => setForm((f) => ({ ...f, deploymentFrequencyPerWeek: Number(e.target.value) }))}
                  />
                </div>
                <div>
                  <label className={labelClass}>{t('developerExperienceScore.cognitiveLoadScore')}</label>
                  <input
                    type="number"
                    className={inputClass}
                    value={form.cognitiveLoadScore}
                    min={0}
                    max={10}
                    step={0.1}
                    onChange={(e) => setForm((f) => ({ ...f, cognitiveLoadScore: Number(e.target.value) }))}
                  />
                </div>
                <div>
                  <label className={labelClass}>{t('developerExperienceScore.toilPercentage')}</label>
                  <input
                    type="number"
                    className={inputClass}
                    value={form.toilPercentage}
                    min={0}
                    max={100}
                    onChange={(e) => setForm((f) => ({ ...f, toilPercentage: Number(e.target.value) }))}
                  />
                </div>
                <div>
                  <label className={labelClass}>{t('developerExperienceScore.notes')}</label>
                  <textarea
                    className={inputClass}
                    value={form.notes}
                    rows={2}
                    onChange={(e) => setForm((f) => ({ ...f, notes: e.target.value }))}
                  />
                </div>
              </div>

              {createMutation.isError && (
                <p className="text-sm text-red-600 dark:text-red-400">{t('developerExperienceScore.createError')}</p>
              )}

              <div className="flex justify-end">
                <Button type="submit" disabled={createMutation.isPending}>
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
          <input
            type="text"
            className="rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-1.5 text-sm text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-400"
            placeholder={t('developerExperienceScore.teamId')}
            value={filterTeamId}
            onChange={(e) => setFilterTeamId(e.target.value)}
          />
        </div>

        {isError ? (
          <PageErrorState />
        ) : isListLoading ? (
          <PageLoadingState message="..." />
        ) : !scoresData?.items?.length ? (
          <p className="text-sm text-gray-500 dark:text-gray-400">{t('developerExperienceScore.noScores')}</p>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-200 dark:border-gray-700 text-left">
                  <th className="pb-2 pr-4 text-xs font-medium text-gray-600 dark:text-gray-400">
                    {t('developerExperienceScore.teamName')}
                  </th>
                  <th className="pb-2 pr-4 text-xs font-medium text-gray-600 dark:text-gray-400">
                    {t('developerExperienceScore.period')}
                  </th>
                  <th className="pb-2 pr-4 text-xs font-medium text-gray-600 dark:text-gray-400">
                    {t('developerExperienceScore.overallScore')}
                  </th>
                  <th className="pb-2 pr-4 text-xs font-medium text-gray-600 dark:text-gray-400">
                    {t('developerExperienceScore.scoreLevel')}
                  </th>
                  <th className="pb-2 text-xs font-medium text-gray-600 dark:text-gray-400">
                    {t('developerExperienceScore.computedAt')}
                  </th>
                </tr>
              </thead>
              <tbody>
                {scoresData.items.map((score) => (
                  <tr
                    key={score.scoreId}
                    className="border-b border-gray-100 dark:border-gray-800 hover:bg-gray-50 dark:hover:bg-gray-800/50"
                  >
                    <td className="py-2 pr-4 font-medium text-gray-900 dark:text-white">{score.teamName}</td>
                    <td className="py-2 pr-4 text-gray-600 dark:text-gray-400">{score.period}</td>
                    <td className="py-2 pr-4 font-semibold text-gray-900 dark:text-white">{score.overallScore}</td>
                    <td className="py-2 pr-4">
                      <Badge variant={SCORE_LEVEL_VARIANT[score.scoreLevel] ?? 'secondary'}>
                        {t(`developerExperienceScore.${score.scoreLevel.toLowerCase()}` as never, {
                          defaultValue: score.scoreLevel,
                        })}
                      </Badge>
                    </td>
                    <td className="py-2 text-xs text-gray-500 dark:text-gray-400">
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
