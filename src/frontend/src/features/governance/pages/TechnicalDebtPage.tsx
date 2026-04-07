import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { AlertTriangle } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

interface DebtItemDto {
  debtId: string;
  serviceName: string;
  debtType: string;
  title: string;
  severity: string;
  estimatedEffortDays: number;
  debtScore: number;
}

interface DebtByTypeDto {
  debtType: string;
  count: number;
  totalScore: number;
}

interface TechnicalDebtSummaryResponse {
  totalDebtScore: number;
  debtItems: DebtItemDto[];
  byType: DebtByTypeDto[];
  highestRiskService: string;
  recommendedAction: string;
}

interface RecordDebtRequest {
  serviceName: string;
  debtType: string;
  title: string;
  description: string;
  severity: string;
  estimatedEffortDays: number;
  tags: string;
}

const DEBT_TYPES = [
  'architecture',
  'code-quality',
  'security',
  'dependency',
  'documentation',
  'testing',
  'performance',
  'infrastructure',
] as const;

const SEVERITIES = ['critical', 'high', 'medium', 'low'] as const;

// ── Severity / DebtType styling ────────────────────────────────────────────

const SEVERITY_VARIANT: Record<string, 'danger' | 'warning' | 'secondary' | 'primary'> = {
  critical: 'danger',
  high: 'warning',
  medium: 'secondary',
  low: 'primary',
};

const DEBT_TYPE_VARIANT: Record<string, 'danger' | 'warning' | 'secondary' | 'primary'> = {
  architecture: 'primary',
  'code-quality': 'secondary',
  security: 'danger',
  dependency: 'warning',
  documentation: 'secondary',
  testing: 'secondary',
  performance: 'warning',
  infrastructure: 'primary',
};

// ── Hooks ──────────────────────────────────────────────────────────────────

const useTechnicalDebtSummary = () =>
  useQuery({
    queryKey: ['technical-debt-summary'],
    queryFn: () =>
      client
        .get<TechnicalDebtSummaryResponse>('/governance/technical-debt/summary', {
          params: { topN: 10 },
        })
        .then((r) => r.data),
  });

const useRecordDebt = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: RecordDebtRequest) =>
      client.post('/governance/technical-debt', data).then((r) => r.data),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: ['technical-debt-summary'] });
    },
  });
};

// ── Component ──────────────────────────────────────────────────────────────

export function TechnicalDebtPage() {
  const { t } = useTranslation();

  const { data, isLoading, isError, refetch } = useTechnicalDebtSummary();
  const recordMutation = useRecordDebt();

  const [serviceName, setServiceName] = useState('');
  const [debtType, setDebtType] = useState<string>(DEBT_TYPES[0]);
  const [title, setTitle] = useState('');
  const [description, setDescription] = useState('');
  const [severity, setSeverity] = useState<string>(SEVERITIES[2]);
  const [effortDays, setEffortDays] = useState(1);
  const [formError, setFormError] = useState<string | null>(null);
  const [formSuccess, setFormSuccess] = useState(false);

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    setFormSuccess(false);

    try {
      await recordMutation.mutateAsync({
        serviceName,
        debtType,
        title,
        description,
        severity,
        estimatedEffortDays: effortDays,
        tags: '',
      });
      setFormSuccess(true);
      setServiceName('');
      setTitle('');
      setDescription('');
      setDebtType(DEBT_TYPES[0]);
      setSeverity(SEVERITIES[2]);
      setEffortDays(1);
    } catch {
      setFormError(t('governance.technicalDebt.createError'));
    }
  };

  if (isLoading) return <PageLoadingState message={t('governance.technicalDebt.loading')} />;
  if (isError)
    return <PageErrorState message={t('governance.technicalDebt.error')} onRetry={() => refetch()} />;

  return (
    <PageContainer>
      <PageHeader
        title={t('governance.technicalDebt.title')}
        subtitle={t('governance.technicalDebt.subtitle')}
        icon={<AlertTriangle size={24} />}
      />

      {/* Summary Section */}
      {data && (
        <PageSection title={t('governance.technicalDebt.byType')}>
          <div className="grid grid-cols-1 gap-4 sm:grid-cols-3">
            <Card>
              <CardBody className="text-center py-4">
                <p className="text-3xl font-bold text-red-600 dark:text-red-400">
                  {data.totalDebtScore}
                </p>
                <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
                  {t('governance.technicalDebt.totalScore')}
                </p>
              </CardBody>
            </Card>
            <Card>
              <CardBody className="text-center py-4">
                <p className="text-xl font-semibold text-gray-900 dark:text-white">
                  {data.highestRiskService}
                </p>
                <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
                  {t('governance.technicalDebt.highestRisk')}
                </p>
              </CardBody>
            </Card>
            <Card>
              <CardBody className="py-4">
                <p className="text-xs text-gray-600 dark:text-gray-400 italic">
                  {data.recommendedAction}
                </p>
              </CardBody>
            </Card>
          </div>

          {/* By Type Badges */}
          <div className="mt-4 flex flex-wrap gap-2">
            {data.byType.map((item) => (
              <span
                key={item.debtType}
                className="inline-flex items-center gap-1 rounded-full bg-gray-100 dark:bg-gray-800 px-3 py-1 text-xs text-gray-700 dark:text-gray-300"
              >
                <span className="font-medium">
                  {t(`governance.technicalDebt.${item.debtType.replace('-', '')}`, {
                    defaultValue: item.debtType,
                  })}
                </span>
                <span className="text-gray-400">·</span>
                <span>
                  {item.count} / {item.totalScore} {t('governance.technicalDebt.score')}
                </span>
              </span>
            ))}
          </div>
        </PageSection>
      )}

      {/* Debt Items Table */}
      <PageSection
        title={`${t('governance.technicalDebt.title')} (${data?.debtItems.length ?? 0})`}
      >
        {data?.debtItems.length === 0 ? (
          <EmptyState
            title={t('governance.technicalDebt.empty', 'No technical debt recorded')}
            description={t('governance.technicalDebt.emptyDescription', 'Record technical debt items using the form below to track and prioritize remediation.')}
            size="compact"
          />
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-200 dark:border-gray-700">
                  <th className="py-2 px-3 text-left text-xs text-gray-500">
                    {t('governance.technicalDebt.serviceName')}
                  </th>
                  <th className="py-2 px-3 text-left text-xs text-gray-500">
                    {t('governance.technicalDebt.debtType')}
                  </th>
                  <th className="py-2 px-3 text-left text-xs text-gray-500">
                    {t('governance.technicalDebt.debtTitle')}
                  </th>
                  <th className="py-2 px-3 text-left text-xs text-gray-500">
                    {t('governance.technicalDebt.severity')}
                  </th>
                  <th className="py-2 px-3 text-right text-xs text-gray-500">
                    {t('governance.technicalDebt.effortDays')}
                  </th>
                  <th className="py-2 px-3 text-right text-xs text-gray-500">
                    {t('governance.technicalDebt.score')}
                  </th>
                </tr>
              </thead>
              <tbody>
                {data?.debtItems.map((item) => (
                  <tr
                    key={item.debtId}
                    className="border-b border-gray-100 dark:border-gray-800 hover:bg-gray-50 dark:hover:bg-gray-900/20"
                  >
                    <td className="py-2 px-3 text-gray-700 dark:text-gray-300 font-medium">
                      {item.serviceName}
                    </td>
                    <td className="py-2 px-3">
                      <Badge variant={DEBT_TYPE_VARIANT[item.debtType] ?? 'secondary'}>
                        {item.debtType}
                      </Badge>
                    </td>
                    <td className="py-2 px-3 text-gray-700 dark:text-gray-300">{item.title}</td>
                    <td className="py-2 px-3">
                      <Badge variant={SEVERITY_VARIANT[item.severity] ?? 'secondary'}>
                        {t(`governance.technicalDebt.${item.severity}`)}
                      </Badge>
                    </td>
                    <td className="py-2 px-3 text-right text-gray-700 dark:text-gray-300">
                      {item.estimatedEffortDays}d
                    </td>
                    <td className="py-2 px-3 text-right font-semibold text-gray-900 dark:text-white">
                      {item.debtScore}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </PageSection>

      {/* Record Debt Form */}
      <PageSection title={t('governance.technicalDebt.recordDebt')}>
        <Card>
          <CardHeader>
            <h3 className="text-sm font-semibold text-gray-900 dark:text-white">
              {t('governance.technicalDebt.recordDebt')}
            </h3>
          </CardHeader>
          <CardBody>
            <form onSubmit={handleSubmit} className="space-y-4">
              {/* Service Name + Debt Type */}
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    {t('governance.technicalDebt.serviceName')}
                  </label>
                  <input
                    type="text"
                    value={serviceName}
                    onChange={(e) => setServiceName(e.target.value)}
                    required
                    maxLength={200}
                    className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-3 py-2 text-gray-900 dark:text-white"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    {t('governance.technicalDebt.debtType')}
                  </label>
                  <select
                    value={debtType}
                    onChange={(e) => setDebtType(e.target.value)}
                    className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-3 py-2 text-gray-900 dark:text-white"
                  >
                    {DEBT_TYPES.map((dt) => (
                      <option key={dt} value={dt}>
                        {t(`governance.technicalDebt.${dt.replace('-', '')}`, {
                          defaultValue: dt,
                        })}
                      </option>
                    ))}
                  </select>
                </div>
              </div>

              {/* Title */}
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  {t('governance.technicalDebt.debtTitle')}
                </label>
                <input
                  type="text"
                  value={title}
                  onChange={(e) => setTitle(e.target.value)}
                  required
                  maxLength={200}
                  className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-3 py-2 text-gray-900 dark:text-white"
                />
              </div>

              {/* Description */}
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                  {t('governance.technicalDebt.debtDescription')}
                </label>
                <textarea
                  value={description}
                  onChange={(e) => setDescription(e.target.value)}
                  required
                  maxLength={1000}
                  rows={3}
                  className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-3 py-2 text-gray-900 dark:text-white"
                />
              </div>

              {/* Severity + Effort Days */}
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    {t('governance.technicalDebt.severity')}
                  </label>
                  <select
                    value={severity}
                    onChange={(e) => setSeverity(e.target.value)}
                    className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-3 py-2 text-gray-900 dark:text-white"
                  >
                    {SEVERITIES.map((s) => (
                      <option key={s} value={s}>
                        {t(`governance.technicalDebt.${s}`)}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                    {t('governance.technicalDebt.effortDays')}
                  </label>
                  <input
                    type="number"
                    min={1}
                    max={999}
                    value={effortDays}
                    onChange={(e) => setEffortDays(Number(e.target.value))}
                    required
                    className="w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 text-sm px-3 py-2 text-gray-900 dark:text-white"
                  />
                </div>
              </div>

              {formError && (
                <p className="text-sm text-red-600 dark:text-red-400">{formError}</p>
              )}
              {formSuccess && (
                <p className="text-sm text-green-600 dark:text-green-400">
                  {t('governance.technicalDebt.createSuccess')}
                </p>
              )}

              <Button type="submit" disabled={recordMutation.isPending}>
                {t('governance.technicalDebt.submit')}
              </Button>
            </form>
          </CardBody>
        </Card>
      </PageSection>
    </PageContainer>
  );
}
