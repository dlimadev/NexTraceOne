import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation } from '@tanstack/react-query';
import { BrainCircuit, TrendingUp } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Button } from '../../../components/Button';
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

  const inputClass =
    'w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-400';
  const labelClass = 'mb-1 block text-xs font-medium text-gray-700 dark:text-gray-300';

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
              <div>
                <label className={labelClass}>{t('predictiveIntelligence.serviceId')}</label>
                <input
                  type="text"
                  className={inputClass}
                  value={form.serviceId}
                  onChange={(e) => setForm((f) => ({ ...f, serviceId: e.target.value }))}
                  required
                />
              </div>
              <div>
                <label className={labelClass}>{t('predictiveIntelligence.serviceName')}</label>
                <input
                  type="text"
                  className={inputClass}
                  value={form.serviceName}
                  onChange={(e) => setForm((f) => ({ ...f, serviceName: e.target.value }))}
                  required
                />
              </div>
              <div>
                <label className={labelClass}>{t('predictiveIntelligence.environment')}</label>
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
                <label className={labelClass}>{t('predictiveIntelligence.predictionHorizon')}</label>
                <select
                  className={inputClass}
                  value={form.predictionHorizon}
                  onChange={(e) => setForm((f) => ({ ...f, predictionHorizon: e.target.value }))}
                >
                  {PREDICTION_HORIZONS.map((h) => (
                    <option key={h} value={h}>{h}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className={labelClass}>{t('predictiveIntelligence.errorRatePercent')}</label>
                <input
                  type="number"
                  className={inputClass}
                  value={form.errorRatePercent}
                  min={0}
                  max={100}
                  onChange={(e) => setForm((f) => ({ ...f, errorRatePercent: Number(e.target.value) }))}
                />
              </div>
              <div>
                <label className={labelClass}>{t('predictiveIntelligence.incidentCount')}</label>
                <input
                  type="number"
                  className={inputClass}
                  value={form.incidentCountLast30Days}
                  min={0}
                  onChange={(e) => setForm((f) => ({ ...f, incidentCountLast30Days: Number(e.target.value) }))}
                />
              </div>
              <div>
                <label className={labelClass}>{t('predictiveIntelligence.changeFrequencyScore')}</label>
                <input
                  type="number"
                  className={inputClass}
                  value={form.changeFrequencyScore}
                  min={0}
                  max={10}
                  step={0.1}
                  onChange={(e) => setForm((f) => ({ ...f, changeFrequencyScore: Number(e.target.value) }))}
                />
              </div>
              <div>
                <label className={labelClass}>{t('predictiveIntelligence.additionalContext')}</label>
                <textarea
                  className={inputClass}
                  value={form.additionalContext}
                  rows={2}
                  onChange={(e) => setForm((f) => ({ ...f, additionalContext: e.target.value }))}
                />
              </div>
            </div>

            {mutation.isError && (
              <p className="text-sm text-red-600 dark:text-red-400">{t('predictiveIntelligence.error')}</p>
            )}

            <div className="flex justify-end">
              <Button type="submit" disabled={mutation.isPending}>
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
              <span className="text-sm font-medium text-gray-900 dark:text-white">{result.serviceName}</span>
              <Badge variant={RISK_VARIANT[result.riskLevel] ?? 'secondary'}>
                {t(`predictiveIntelligence.${result.riskLevel.toLowerCase()}` as never, {
                  defaultValue: result.riskLevel,
                })}
              </Badge>
            </div>
          </CardHeader>
          <CardBody className="space-y-4">
            <div className="flex items-end gap-2">
              <span className="text-5xl font-bold text-gray-900 dark:text-white">
                {result.failureProbabilityPercent}
                <span className="text-2xl">%</span>
              </span>
              <span className="text-sm text-gray-500 dark:text-gray-400 mb-1">
                {t('predictiveIntelligence.failureProbability')}
              </span>
            </div>

            <div>
              <p className="mb-2 text-xs font-medium text-gray-700 dark:text-gray-300">
                {t('predictiveIntelligence.causalFactors')}
              </p>
              <div className="flex flex-wrap gap-2">
                {result.causalFactors.map((factor, i) => (
                  <span
                    key={i}
                    className="rounded-full bg-blue-100 dark:bg-blue-900/30 px-2 py-0.5 text-xs text-blue-700 dark:text-blue-300"
                  >
                    {factor}
                  </span>
                ))}
              </div>
            </div>

            <div>
              <p className="mb-1 text-xs font-medium text-gray-700 dark:text-gray-300">
                {t('predictiveIntelligence.recommendedAction')}
              </p>
              <p className="text-sm text-gray-800 dark:text-gray-200">{result.recommendedAction}</p>
            </div>

            <p className="text-xs text-gray-400">
              {t('predictiveIntelligence.computedAt')}: {new Date(result.computedAt).toLocaleString()}
            </p>
          </CardBody>
        </Card>
      )}

      {!result && !mutation.isPending && (
        <p className="text-sm text-gray-500 dark:text-gray-400">{t('predictiveIntelligence.noResult')}</p>
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

  const inputClass =
    'w-full rounded border border-gray-300 dark:border-gray-600 bg-white dark:bg-gray-800 px-3 py-2 text-sm text-gray-900 dark:text-white focus:outline-none focus:ring-2 focus:ring-blue-400';
  const labelClass = 'mb-1 block text-xs font-medium text-gray-700 dark:text-gray-300';
  const checkboxClass = 'mr-2 h-4 w-4 rounded border-gray-300 text-blue-600 focus:ring-blue-400';

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
              <div>
                <label className={labelClass}>{t('predictiveIntelligence.serviceId')}</label>
                <input
                  type="text"
                  className={inputClass}
                  value={form.serviceId}
                  onChange={(e) => setForm((f) => ({ ...f, serviceId: e.target.value }))}
                  required
                />
              </div>
              <div>
                <label className={labelClass}>{t('predictiveIntelligence.environment')}</label>
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
                <label className={labelClass}>{t('predictiveIntelligence.changeType')}</label>
                <select
                  className={inputClass}
                  value={form.changeType}
                  onChange={(e) => setForm((f) => ({ ...f, changeType: e.target.value }))}
                >
                  {CHANGE_TYPES.map((ct) => (
                    <option key={ct} value={ct}>{ct}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className={labelClass}>{t('predictiveIntelligence.priorIncidentRate')}</label>
                <input
                  type="number"
                  className={inputClass}
                  value={form.priorIncidentRate}
                  min={0}
                  step={0.01}
                  onChange={(e) => setForm((f) => ({ ...f, priorIncidentRate: Number(e.target.value) }))}
                />
              </div>
              <div>
                <label className={labelClass}>{t('predictiveIntelligence.blastRadius')}</label>
                <input
                  type="number"
                  className={inputClass}
                  value={form.blastRadius}
                  min={0}
                  max={100}
                  onChange={(e) => setForm((f) => ({ ...f, blastRadius: Number(e.target.value) }))}
                />
              </div>
            </div>

            <div className="flex flex-col gap-3">
              <label className="flex items-center text-sm text-gray-700 dark:text-gray-300">
                <input
                  type="checkbox"
                  className={checkboxClass}
                  checked={form.hasTestEvidence}
                  onChange={(e) => setForm((f) => ({ ...f, hasTestEvidence: e.target.checked }))}
                />
                {t('predictiveIntelligence.hasTestEvidence')}
              </label>
              <label className="flex items-center text-sm text-gray-700 dark:text-gray-300">
                <input
                  type="checkbox"
                  className={checkboxClass}
                  checked={form.isBusinessHours}
                  onChange={(e) => setForm((f) => ({ ...f, isBusinessHours: e.target.checked }))}
                />
                {t('predictiveIntelligence.isBusinessHours')}
              </label>
            </div>

            {mutation.isError && (
              <p className="text-sm text-red-600 dark:text-red-400">{t('predictiveIntelligence.error')}</p>
            )}

            <div className="flex justify-end">
              <Button type="submit" disabled={mutation.isPending}>
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
              <span className="text-sm font-medium text-gray-900 dark:text-white">{result.serviceId}</span>
              <Badge variant={RISK_VARIANT[result.riskLevel] ?? 'secondary'}>
                {t(`predictiveIntelligence.${result.riskLevel.toLowerCase()}` as never, {
                  defaultValue: result.riskLevel,
                })}
              </Badge>
            </div>
          </CardHeader>
          <CardBody className="space-y-4">
            <div className="flex items-end gap-2">
              <span className="text-5xl font-bold text-gray-900 dark:text-white">{result.riskScore}</span>
              <span className="text-sm text-gray-500 dark:text-gray-400 mb-1">
                {t('predictiveIntelligence.riskScore')}
              </span>
            </div>

            {result.riskFactors.length > 0 && (
              <div>
                <p className="mb-2 text-xs font-medium text-gray-700 dark:text-gray-300">
                  {t('predictiveIntelligence.riskFactors')}
                </p>
                <ul className="space-y-1">
                  {result.riskFactors.map((factor, i) => (
                    <li key={i} className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
                      <span className="h-1.5 w-1.5 rounded-full bg-orange-500 shrink-0" />
                      {factor}
                    </li>
                  ))}
                </ul>
              </div>
            )}

            {result.recommendations.length > 0 && (
              <div>
                <p className="mb-2 text-xs font-medium text-gray-700 dark:text-gray-300">
                  {t('predictiveIntelligence.recommendations')}
                </p>
                <ul className="space-y-1">
                  {result.recommendations.map((rec, i) => (
                    <li key={i} className="flex items-center gap-2 text-sm text-gray-700 dark:text-gray-300">
                      <span className="h-1.5 w-1.5 rounded-full bg-blue-500 shrink-0" />
                      {rec}
                    </li>
                  ))}
                </ul>
              </div>
            )}

            <p className="text-xs text-gray-400">
              {t('predictiveIntelligence.assessedAt')}: {new Date(result.assessedAt).toLocaleString()}
            </p>
          </CardBody>
        </Card>
      )}

      {!result && !mutation.isPending && (
        <p className="text-sm text-gray-500 dark:text-gray-400">{t('predictiveIntelligence.noResult')}</p>
      )}
    </div>
  );
}

// ── Page ───────────────────────────────────────────────────────────────────

type ActiveTab = 'serviceFailure' | 'changeRisk';

export function PredictiveIntelligencePage() {
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState<ActiveTab>('serviceFailure');

  const tabClass = (tab: ActiveTab) =>
    `px-4 py-2 text-sm font-medium rounded-t border-b-2 transition-colors ${
      activeTab === tab
        ? 'border-blue-500 text-blue-600 dark:text-blue-400'
        : 'border-transparent text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-white'
    }`;

  return (
    <PageContainer>
      <PageHeader
        title={t('predictiveIntelligence.title')}
        subtitle={t('predictiveIntelligence.subtitle')}
        icon={<BrainCircuit size={24} />}
      />

      <PageSection title="">
        <div className="mb-4 flex gap-1 border-b border-gray-200 dark:border-gray-700">
          <button
            type="button"
            className={tabClass('serviceFailure')}
            onClick={() => setActiveTab('serviceFailure')}
          >
            {t('predictiveIntelligence.serviceFailurePrediction')}
          </button>
          <button
            type="button"
            className={tabClass('changeRisk')}
            onClick={() => setActiveTab('changeRisk')}
          >
            {t('predictiveIntelligence.changeRiskAssessment')}
          </button>
        </div>

        {activeTab === 'serviceFailure' && <ServiceFailurePredictionForm />}
        {activeTab === 'changeRisk' && <ChangeRiskAssessmentForm />}
      </PageSection>
    </PageContainer>
  );
}
