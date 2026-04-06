import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation } from '@tanstack/react-query';
import { Zap, AlertTriangle, ChevronDown, ChevronRight, ShieldCheck } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import client from '../../../api/client';

interface CreateChaosExperimentRequest {
  serviceName: string;
  environment: string;
  experimentType: string;
  description?: string;
  durationSeconds: number;
  targetPercentage: number;
}

interface CreateChaosExperimentResponse {
  experimentId: string;
  serviceName: string;
  environment: string;
  experimentType: string;
  steps: string[];
  riskLevel: string;
  estimatedDurationSeconds: number;
  targetPercentage: number;
  safetyChecks: string[];
  createdAt: string;
}

interface ChaosExperimentSummary {
  experimentId: string;
  serviceName: string;
  environment: string;
  experimentType: string;
  riskLevel: string;
  status: string;
  createdAt: string;
}

interface ListChaosExperimentsResponse {
  items: ChaosExperimentSummary[];
  totalCount: number;
}

const ENVIRONMENTS = ['Development', 'Staging', 'Production'];
const EXPERIMENT_TYPES = [
  'latency-injection',
  'error-injection',
  'cpu-stress',
  'memory-stress',
  'pod-kill',
  'network-partition',
];

const RISK_VARIANT: Record<string, 'success' | 'warning' | 'danger' | 'secondary'> = {
  Low: 'success',
  Medium: 'warning',
  High: 'danger',
};

const STATUS_VARIANT: Record<string, 'success' | 'warning' | 'danger' | 'secondary'> = {
  Completed: 'success',
  Running: 'warning',
  Planned: 'secondary',
  Cancelled: 'secondary',
};

const useListChaosExperiments = () =>
  useQuery({
    queryKey: ['chaos-experiments'],
    queryFn: () =>
      client
        .get<ListChaosExperimentsResponse>('/runtime/chaos/experiments')
        .then((r) => r.data),
  });

const initialForm: CreateChaosExperimentRequest = {
  serviceName: '',
  environment: 'Development',
  experimentType: 'latency-injection',
  description: '',
  durationSeconds: 60,
  targetPercentage: 10,
};

function ExperimentResultCard({ result }: { result: CreateChaosExperimentResponse }) {
  const { t } = useTranslation();
  const [stepsOpen, setStepsOpen] = useState(false);

  return (
    <Card>
      <CardHeader>
        <div className="flex items-center justify-between flex-wrap gap-2">
          <div className="flex items-center gap-2">
            <Badge variant={RISK_VARIANT[result.riskLevel] ?? 'secondary'}>
              {t(`chaosEngineering.${result.riskLevel.toLowerCase()}` as never, { defaultValue: result.riskLevel })}
            </Badge>
            <span className="text-sm font-medium text-gray-900 dark:text-white">{result.serviceName}</span>
            <span className="text-xs text-gray-500 dark:text-gray-400">— {result.experimentType}</span>
          </div>
          <span className="text-xs text-gray-400">{result.estimatedDurationSeconds}s / {result.targetPercentage}%</span>
        </div>
      </CardHeader>
      <CardBody className="space-y-3">
        {/* Steps */}
        <div>
          <button
            type="button"
            onClick={() => setStepsOpen((v) => !v)}
            className="flex items-center gap-1 text-sm font-medium text-gray-700 dark:text-gray-300 hover:text-blue-600 dark:hover:text-blue-400"
          >
            {stepsOpen ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
            {t('chaosEngineering.steps')} ({result.steps.length})
          </button>
          {stepsOpen && (
            <ol className="mt-2 space-y-1 pl-5 list-decimal">
              {result.steps.map((step, i) => (
                <li key={i} className="text-xs text-gray-600 dark:text-gray-400">{step}</li>
              ))}
            </ol>
          )}
        </div>

        {/* Safety Checks */}
        <div>
          <p className="mb-1 flex items-center gap-1 text-sm font-medium text-gray-700 dark:text-gray-300">
            <ShieldCheck size={14} className="text-amber-500" />
            {t('chaosEngineering.safetyChecks')}
          </p>
          <ul className="space-y-1">
            {result.safetyChecks.map((check, i) => (
              <li key={i} className="flex items-center gap-2 rounded bg-amber-50 dark:bg-amber-900/20 px-2 py-1">
                <AlertTriangle size={12} className="shrink-0 text-amber-500" />
                <span className="text-xs text-amber-700 dark:text-amber-300">{check}</span>
              </li>
            ))}
          </ul>
        </div>
      </CardBody>
    </Card>
  );
}

function ExperimentSummaryCard({ experiment }: { experiment: ChaosExperimentSummary }) {
  const { t } = useTranslation();

  return (
    <Card>
      <CardBody>
        <div className="flex items-center justify-between gap-2 flex-wrap">
          <div className="flex items-center gap-2">
            <Badge variant={RISK_VARIANT[experiment.riskLevel] ?? 'secondary'}>
              {t(`chaosEngineering.${experiment.riskLevel.toLowerCase()}` as never, { defaultValue: experiment.riskLevel })}
            </Badge>
            <Badge variant={STATUS_VARIANT[experiment.status] ?? 'secondary'}>
              {t(`chaosEngineering.${experiment.status.toLowerCase()}` as never, { defaultValue: experiment.status })}
            </Badge>
          </div>
          <span className="text-xs text-gray-400">
            {new Date(experiment.createdAt).toLocaleString()}
          </span>
        </div>
        <p className="mt-2 text-sm font-medium text-gray-900 dark:text-white">{experiment.serviceName}</p>
        <p className="text-xs text-gray-500 dark:text-gray-400">{experiment.experimentType} — {experiment.environment}</p>
      </CardBody>
    </Card>
  );
}

export function ChaosEngineeringPage() {
  const { t } = useTranslation();
  const [form, setForm] = useState<CreateChaosExperimentRequest>(initialForm);
  const [lastResult, setLastResult] = useState<CreateChaosExperimentResponse | null>(null);

  const { data: experimentsData, isLoading: isListLoading } = useListChaosExperiments();

  const createMutation = useMutation({
    mutationFn: (data: CreateChaosExperimentRequest) =>
      client.post<CreateChaosExperimentResponse>('/runtime/chaos/experiments', data).then((r) => r.data),
    onSuccess: (data) => {
      setLastResult(data);
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
        title={t('chaosEngineering.title')}
        subtitle={t('chaosEngineering.subtitle')}
        icon={<Zap size={24} />}
      />

      <PageSection title={t('chaosEngineering.createExperiment')}>
        <Card>
          <CardBody>
            <form onSubmit={handleSubmit} className="space-y-4">
              <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
                <div>
                  <label className={labelClass}>{t('chaosEngineering.serviceName')}</label>
                  <input
                    type="text"
                    className={inputClass}
                    value={form.serviceName}
                    onChange={(e) => setForm((f) => ({ ...f, serviceName: e.target.value }))}
                    required
                  />
                </div>
                <div>
                  <label className={labelClass}>{t('chaosEngineering.environment')}</label>
                  <select
                    className={inputClass}
                    value={form.environment}
                    onChange={(e) => setForm((f) => ({ ...f, environment: e.target.value }))}
                  >
                    {ENVIRONMENTS.map((env) => (
                      <option key={env} value={env}>{env}</option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className={labelClass}>{t('chaosEngineering.experimentType')}</label>
                  <select
                    className={inputClass}
                    value={form.experimentType}
                    onChange={(e) => setForm((f) => ({ ...f, experimentType: e.target.value }))}
                  >
                    {EXPERIMENT_TYPES.map((type) => (
                      <option key={type} value={type}>
                        {t(`chaosEngineering.${type.replace(/-([a-z])/g, (_, c: string) => c.toUpperCase())}` as never, {
                          defaultValue: type,
                        })}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className={labelClass}>{t('chaosEngineering.duration')}</label>
                  <input
                    type="number"
                    className={inputClass}
                    value={form.durationSeconds}
                    min={10}
                    max={3600}
                    onChange={(e) => setForm((f) => ({ ...f, durationSeconds: Number(e.target.value) }))}
                  />
                </div>
                <div>
                  <label className={labelClass}>{t('chaosEngineering.targetPercentage')}</label>
                  <input
                    type="number"
                    className={inputClass}
                    value={form.targetPercentage}
                    min={1}
                    max={100}
                    onChange={(e) => setForm((f) => ({ ...f, targetPercentage: Number(e.target.value) }))}
                  />
                </div>
                <div>
                  <label className={labelClass}>{t('chaosEngineering.description')}</label>
                  <textarea
                    className={inputClass}
                    value={form.description}
                    rows={2}
                    onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
                  />
                </div>
              </div>

              {createMutation.isError && (
                <p className="text-sm text-red-600 dark:text-red-400">{t('chaosEngineering.createError')}</p>
              )}

              <div className="flex justify-end">
                <Button type="submit" disabled={createMutation.isPending}>
                  {createMutation.isPending ? '...' : t('chaosEngineering.submit')}
                </Button>
              </div>
            </form>
          </CardBody>
        </Card>
      </PageSection>

      {lastResult && (
        <PageSection title={t('chaosEngineering.createSuccess')}>
          <ExperimentResultCard result={lastResult} />
        </PageSection>
      )}

      <PageSection title={t('chaosEngineering.recentExperiments')}>
        {isListLoading ? (
          <PageLoadingState message="..." />
        ) : !experimentsData?.items?.length ? (
          <p className="text-sm text-gray-500 dark:text-gray-400">{t('chaosEngineering.noExperiments')}</p>
        ) : (
          <div className="space-y-3">
            {experimentsData.items.map((experiment) => (
              <ExperimentSummaryCard key={experiment.experimentId} experiment={experiment} />
            ))}
          </div>
        )}
      </PageSection>
    </PageContainer>
  );
}
