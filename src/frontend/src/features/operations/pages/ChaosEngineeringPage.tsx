import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation } from '@tanstack/react-query';
import { Zap, AlertTriangle, ChevronDown, ChevronRight, ShieldCheck } from 'lucide-react';
import { EmptyState } from '../../../components/EmptyState';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button, TextField, TextArea, Select } from '../../../shared/ui';
import client from '../../../api/client';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

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

const useListChaosExperiments = () => {
  const { activeEnvironmentId } = useEnvironment();
  return useQuery({
    queryKey: ['chaos-experiments', activeEnvironmentId],
    queryFn: () =>
      client
        .get<ListChaosExperimentsResponse>('/runtime/chaos/experiments')
        .then((r) => r.data),
  });
};

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
            <span className="text-sm font-medium text-heading">{result.serviceName}</span>
            <span className="text-xs text-muted">— {result.experimentType}</span>
          </div>
          <span className="text-xs text-faded">{result.estimatedDurationSeconds}s / {result.targetPercentage}%</span>
        </div>
      </CardHeader>
      <CardBody className="space-y-3">
        {/* Steps */}
        <div>
          <Button
            type="button"
            variant="ghost"
            size="sm"
            icon={stepsOpen ? <ChevronDown size={14} /> : <ChevronRight size={14} />}
            onClick={() => setStepsOpen((v) => !v)}
          >
            {t('chaosEngineering.steps')} ({result.steps.length})
          </Button>
          {stepsOpen && (
            <ol className="mt-2 space-y-1 pl-5 list-decimal">
              {result.steps.map((step, i) => (
                <li key={i} className="text-xs text-muted">{step}</li>
              ))}
            </ol>
          )}
        </div>

        {/* Safety Checks */}
        <div>
          <p className="mb-1 flex items-center gap-1 text-sm font-medium text-body">
            <ShieldCheck size={14} className="text-warning" />
            {t('chaosEngineering.safetyChecks')}
          </p>
          <ul className="space-y-1">
            {result.safetyChecks.map((check, i) => (
              <li key={i} className="flex items-center gap-2 rounded bg-warning-muted px-2 py-1">
                <AlertTriangle size={12} className="shrink-0 text-warning" />
                <span className="text-xs text-warning">{check}</span>
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
          <span className="text-xs text-faded">
            {new Date(experiment.createdAt).toLocaleString()}
          </span>
        </div>
        <p className="mt-2 text-sm font-medium text-heading">{experiment.serviceName}</p>
        <p className="text-xs text-muted">{experiment.experimentType} — {experiment.environment}</p>
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
                <TextField
                  label={t('chaosEngineering.serviceName')}
                  type="text"
                  value={form.serviceName}
                  onChange={(e) => setForm((f) => ({ ...f, serviceName: e.target.value }))}
                  required
                />
                <Select
                  label={t('chaosEngineering.environment')}
                  value={form.environment}
                  onChange={(e) => setForm((f) => ({ ...f, environment: e.target.value }))}
                  options={ENVIRONMENTS.map((env) => ({ value: env, label: env }))}
                />
                <Select
                  label={t('chaosEngineering.experimentType')}
                  value={form.experimentType}
                  onChange={(e) => setForm((f) => ({ ...f, experimentType: e.target.value }))}
                  options={EXPERIMENT_TYPES.map((type) => ({
                    value: type,
                    label: t(`chaosEngineering.${type.replace(/-([a-z])/g, (_, c: string) => c.toUpperCase())}` as never, {
                      defaultValue: type,
                    }),
                  }))}
                />
                <TextField
                  label={t('chaosEngineering.duration')}
                  type="number"
                  value={form.durationSeconds}
                  min={10}
                  max={3600}
                  onChange={(e) => setForm((f) => ({ ...f, durationSeconds: Number(e.target.value) }))}
                />
                <TextField
                  label={t('chaosEngineering.targetPercentage')}
                  type="number"
                  value={form.targetPercentage}
                  min={1}
                  max={100}
                  onChange={(e) => setForm((f) => ({ ...f, targetPercentage: Number(e.target.value) }))}
                />
                <TextArea
                  label={t('chaosEngineering.description')}
                  value={form.description}
                  rows={2}
                  onChange={(e) => setForm((f) => ({ ...f, description: e.target.value }))}
                />
              </div>

              {createMutation.isError && (
                <p className="text-sm text-critical">{t('chaosEngineering.createError')}</p>
              )}

              <div className="flex justify-end">
                <Button
                  type="submit"
                  variant="primary"
                  loading={createMutation.isPending}
                >
                  {t('chaosEngineering.submit')}
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
          <EmptyState
            icon={<Zap size={20} />}
            title={t('chaosEngineering.noExperiments', 'No experiments found')}
            description={t('chaosEngineering.noExperimentsHint', 'Create an experiment above to get started.')}
            size="compact"
          />
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
