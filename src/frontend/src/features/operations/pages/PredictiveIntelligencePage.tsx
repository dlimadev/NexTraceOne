import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation } from '@tanstack/react-query';
import { BrainCircuit } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
import { Tabs } from '../../../components/Tabs';
import { TextField } from '../../../components/TextField';
import { TextArea } from '../../../components/TextArea';
import { Select } from '../../../components/Select';
import { Checkbox } from '../../../components/Checkbox';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────

interface ServiceFailurePredictionRequest {
  serviceId: string;
  serviceName: string;
  environment: string;
  predictionHorizon: string;
  errorRatePercent: number;
  incidentCountLast30Days: number;
  changeFrequencyScore: number;
  additionalContext?: string;
}

interface ServiceFailurePredictionResponse {
  predictionId: string;
  serviceId: string;
  serviceName: string;
  failureProbabilityPercent: number;
  riskLevel: string;
  predictionHorizon: string;
  causalFactors: string[];
  recommendedAction: string;
  computedAt: string;
}

interface ChangeRiskRequest {
  serviceId: string;
  environment: string;
  changeType: string;
  priorIncidentRate: number;
  blastRadius: number;
  hasTestEvidence: boolean;
  isBusinessHours: boolean;
}

interface ChangeRiskResponse {
  changeId: string;
  serviceId: string;
  riskScore: number;
  riskLevel: string;
  riskFactors: string[];
  recommendations: string[];
  assessedAt: string;
}

// ── Constants ──────────────────────────────────────────────────────────────

const ENVIRONMENTS = ['Production', 'Staging', 'Development'];
const PREDICTION_HORIZONS = ['24h', '48h', '7d'];
const CHANGE_TYPES = ['Minor', 'Major', 'Breaking', 'Hotfix', 'Configuration'];
const DEMO_CHANGE_ID = '00000000-0000-0000-0000-000000000001';

const RISK_VARIANT: Record<string, 'success' | 'warning' | 'danger' | 'secondary'> = {
  Critical: 'danger',
  High: 'danger',
  Medium: 'warning',
  Low: 'success',
};

// ── Sub-components ─────────────────────────────────────────────────────────

function ServiceFailurePredictionForm() {
  const { t } = useTranslation();
  const [form, setForm] = useState<ServiceFailurePredictionRequest>({
    serviceId: '',
    serviceName: '',
    environment: 'Production',
    predictionHorizon: '24h',
    errorRatePercent: 0,
    incidentCountLast30Days: 0,
    changeFrequencyScore: 0,
    additionalContext: '',
  });
  const [result, setResult] = useState<ServiceFailurePredictionResponse | null>(null);

  const mutation = useMutation({
    mutationFn: (data: ServiceFailurePredictionRequest) =>
      client
        .post<ServiceFailurePredictionResponse>('/predictive/service-failure', data)
        .then((r) => r.data),
    onSuccess: (data) => setResult(data),
  });

  return (
    <div className="space-y-4">
      <Card>
        <CardBody>
          <form
            onSubmit={(e) => {
              e.preventDefault();
              mutation.mutate(form);
            }}
            className="space-y-4"
          >
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <TextField
                size="sm"
                label={t('predictiveIntelligence.serviceId')}
                value={form.serviceId}
                onChange={(e) => setForm((f) => ({ ...f, serviceId: e.target.value }))}
                required
              />
              <TextField
                size="sm"
                label={t('predictiveIntelligence.serviceName')}
                value={form.serviceName}
                onChange={(e) => setForm((f) => ({ ...f, serviceName: e.target.value }))}
                required
              />
              <Select
                size="sm"
                label={t('predictiveIntelligence.environment')}
                value={form.environment}
                onChange={(e) => setForm((f) => ({ ...f, environment: e.target.value }))}
                options={ENVIRONMENTS.map((env) => ({ value: env, label: env }))}
              />
              <Select
                size="sm"
                label={t('predictiveIntelligence.predictionHorizon')}
                value={form.predictionHorizon}
                onChange={(e) => setForm((f) => ({ ...f, predictionHorizon: e.target.value }))}
                options={PREDICTION_HORIZONS.map((h) => ({ value: h, label: h }))}
              />
              <TextField
                size="sm"
                type="number"
                label={t('predictiveIntelligence.errorRatePercent')}
                value={form.errorRatePercent}
                min={0}
                max={100}
                onChange={(e) => setForm((f) => ({ ...f, errorRatePercent: Number(e.target.value) }))}
              />
              <TextField
                size="sm"
                type="number"
                label={t('predictiveIntelligence.incidentCount')}
                value={form.incidentCountLast30Days}
                min={0}
                onChange={(e) => setForm((f) => ({ ...f, incidentCountLast30Days: Number(e.target.value) }))}
              />
              <TextField
                size="sm"
                type="number"
                label={t('predictiveIntelligence.changeFrequencyScore')}
                value={form.changeFrequencyScore}
                min={0}
                max={10}
                step={0.1}
                onChange={(e) => setForm((f) => ({ ...f, changeFrequencyScore: Number(e.target.value) }))}
              />
              <TextArea
                label={t('predictiveIntelligence.additionalContext')}
                value={form.additionalContext}
                rows={2}
                onChange={(e) => setForm((f) => ({ ...f, additionalContext: e.target.value }))}
              />
            </div>

            {mutation.isError && (
              <p className="text-sm text-critical">{t('predictiveIntelligence.error')}</p>
            )}

            <div className="flex justify-end">
              <Button type="submit" disabled={mutation.isPending} loading={mutation.isPending}>
                {mutation.isPending ? t('predictiveIntelligence.loading') : t('predictiveIntelligence.submit')}
              </Button>
            </div>
          </form>
        </CardBody>
      </Card>

      {result && (
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between flex-wrap gap-2">
              <span className="text-sm font-medium text-heading">{result.serviceName}</span>
              <Badge variant={RISK_VARIANT[result.riskLevel] ?? 'secondary'}>
                {t(`predictiveIntelligence.${result.riskLevel.toLowerCase()}` as never, {
                  defaultValue: result.riskLevel,
                })}
              </Badge>
            </div>
          </CardHeader>
          <CardBody className="space-y-4">
            <div className="flex items-end gap-2">
              <span className="text-5xl font-bold text-heading">
                {result.failureProbabilityPercent}
                <span className="text-2xl">%</span>
              </span>
              <span className="text-sm text-muted mb-1">
                {t('predictiveIntelligence.failureProbability')}
              </span>
            </div>

            <div>
              <p className="mb-2 text-xs font-medium text-body">
                {t('predictiveIntelligence.causalFactors')}
              </p>
              <div className="flex flex-wrap gap-2">
                {result.causalFactors.map((factor, i) => (
                  <span
                    key={i}
                    className="rounded-full bg-accent/10 px-2 py-0.5 text-xs text-accent"
                  >
                    {factor}
                  </span>
                ))}
              </div>
            </div>

            <div>
              <p className="mb-1 text-xs font-medium text-body">
                {t('predictiveIntelligence.recommendedAction')}
              </p>
              <p className="text-sm text-body">{result.recommendedAction}</p>
            </div>

            <p className="text-xs text-faded">
              {t('predictiveIntelligence.computedAt')}: {new Date(result.computedAt).toLocaleString()}
            </p>
          </CardBody>
        </Card>
      )}

      {!result && !mutation.isPending && (
        <p className="text-sm text-muted">{t('predictiveIntelligence.noResult')}</p>
      )}
    </div>
  );
}

function ChangeRiskAssessmentForm() {
  const { t } = useTranslation();
  const [form, setForm] = useState<ChangeRiskRequest>({
    serviceId: '',
    environment: 'Production',
    changeType: 'Minor',
    priorIncidentRate: 0,
    blastRadius: 0,
    hasTestEvidence: false,
    isBusinessHours: true,
  });
  const [result, setResult] = useState<ChangeRiskResponse | null>(null);

  const mutation = useMutation({
    mutationFn: (data: ChangeRiskRequest) =>
      client
        .get<ChangeRiskResponse>(`/predictive/change-risk/${DEMO_CHANGE_ID}`, {
          params: {
            serviceId: data.serviceId,
            environment: data.environment,
            changeType: data.changeType,
            priorIncidentRate: data.priorIncidentRate,
            blastRadius: data.blastRadius,
            hasTestEvidence: data.hasTestEvidence,
            isBusinessHours: data.isBusinessHours,
          },
        })
        .then((r) => r.data),
    onSuccess: (data) => setResult(data),
  });

  return (
    <div className="space-y-4">
      <Card>
        <CardBody>
          <form
            onSubmit={(e) => {
              e.preventDefault();
              mutation.mutate(form);
            }}
            className="space-y-4"
          >
            <div className="grid grid-cols-1 gap-4 sm:grid-cols-2">
              <TextField
                size="sm"
                label={t('predictiveIntelligence.serviceId')}
                value={form.serviceId}
                onChange={(e) => setForm((f) => ({ ...f, serviceId: e.target.value }))}
                required
              />
              <Select
                size="sm"
                label={t('predictiveIntelligence.environment')}
                value={form.environment}
                onChange={(e) => setForm((f) => ({ ...f, environment: e.target.value }))}
                options={ENVIRONMENTS.map((env) => ({ value: env, label: env }))}
              />
              <Select
                size="sm"
                label={t('predictiveIntelligence.changeType')}
                value={form.changeType}
                onChange={(e) => setForm((f) => ({ ...f, changeType: e.target.value }))}
                options={CHANGE_TYPES.map((ct) => ({ value: ct, label: ct }))}
              />
              <TextField
                size="sm"
                type="number"
                label={t('predictiveIntelligence.priorIncidentRate')}
                value={form.priorIncidentRate}
                min={0}
                step={0.01}
                onChange={(e) => setForm((f) => ({ ...f, priorIncidentRate: Number(e.target.value) }))}
              />
              <TextField
                size="sm"
                type="number"
                label={t('predictiveIntelligence.blastRadius')}
                value={form.blastRadius}
                min={0}
                max={100}
                onChange={(e) => setForm((f) => ({ ...f, blastRadius: Number(e.target.value) }))}
              />
            </div>

            <div className="flex flex-col gap-3">
              <Checkbox
                label={t('predictiveIntelligence.hasTestEvidence')}
                checked={form.hasTestEvidence}
                onChange={(e) => setForm((f) => ({ ...f, hasTestEvidence: e.target.checked }))}
              />
              <Checkbox
                label={t('predictiveIntelligence.isBusinessHours')}
                checked={form.isBusinessHours}
                onChange={(e) => setForm((f) => ({ ...f, isBusinessHours: e.target.checked }))}
              />
            </div>

            {mutation.isError && (
              <p className="text-sm text-critical">{t('predictiveIntelligence.error')}</p>
            )}

            <div className="flex justify-end">
              <Button type="submit" disabled={mutation.isPending} loading={mutation.isPending}>
                {mutation.isPending ? t('predictiveIntelligence.loading') : t('predictiveIntelligence.submit')}
              </Button>
            </div>
          </form>
        </CardBody>
      </Card>

      {result && (
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between flex-wrap gap-2">
              <span className="text-sm font-medium text-heading">{result.serviceId}</span>
              <Badge variant={RISK_VARIANT[result.riskLevel] ?? 'secondary'}>
                {t(`predictiveIntelligence.${result.riskLevel.toLowerCase()}` as never, {
                  defaultValue: result.riskLevel,
                })}
              </Badge>
            </div>
          </CardHeader>
          <CardBody className="space-y-4">
            <div className="flex items-end gap-2">
              <span className="text-5xl font-bold text-heading">{result.riskScore}</span>
              <span className="text-sm text-muted mb-1">
                {t('predictiveIntelligence.riskScore')}
              </span>
            </div>

            {result.riskFactors.length > 0 && (
              <div>
                <p className="mb-2 text-xs font-medium text-body">
                  {t('predictiveIntelligence.riskFactors')}
                </p>
                <ul className="space-y-1">
                  {result.riskFactors.map((factor, i) => (
                    <li key={i} className="flex items-center gap-2 text-sm text-body">
                      <span className="h-1.5 w-1.5 rounded-full bg-warning shrink-0" />
                      {factor}
                    </li>
                  ))}
                </ul>
              </div>
            )}

            {result.recommendations.length > 0 && (
              <div>
                <p className="mb-2 text-xs font-medium text-body">
                  {t('predictiveIntelligence.recommendations')}
                </p>
                <ul className="space-y-1">
                  {result.recommendations.map((rec, i) => (
                    <li key={i} className="flex items-center gap-2 text-sm text-body">
                      <span className="h-1.5 w-1.5 rounded-full bg-accent shrink-0" />
                      {rec}
                    </li>
                  ))}
                </ul>
              </div>
            )}

            <p className="text-xs text-faded">
              {t('predictiveIntelligence.assessedAt')}: {new Date(result.assessedAt).toLocaleString()}
            </p>
          </CardBody>
        </Card>
      )}

      {!result && !mutation.isPending && (
        <p className="text-sm text-muted">{t('predictiveIntelligence.noResult')}</p>
      )}
    </div>
  );
}

// ── Page ───────────────────────────────────────────────────────────────────

type ActiveTab = 'serviceFailure' | 'changeRisk';

export function PredictiveIntelligencePage() {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState<ActiveTab>('serviceFailure');

  return (
    <PageContainer>
      <PageHeader
        title={t('predictiveIntelligence.title')}
        subtitle={t('predictiveIntelligence.subtitle')}
        icon={<BrainCircuit size={24} />}
      />

      <PageSection title={t('predictiveIntelligence.analysisPanel')}>
        <Tabs
          className="mb-4"
          items={[
            { id: 'serviceFailure', label: t('predictiveIntelligence.serviceFailurePrediction') },
            { id: 'changeRisk', label: t('predictiveIntelligence.changeRiskAssessment') },
          ]}
          activeId={activeTab}
          onChange={(id) => setActiveTab(id as ActiveTab)}
        />

        {activeTab === 'serviceFailure' && <ServiceFailurePredictionForm />}
        {activeTab === 'changeRisk' && <ChangeRiskAssessmentForm />}
      </PageSection>
    </PageContainer>
  );
}
