import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery } from '@tanstack/react-query';
import { AlertTriangle, ShieldCheck } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { reliabilityApi, type BurnRateWindow, type ServiceSloItem, type SloType } from '../api/reliability';
import { useEnvironment } from '../../../contexts/EnvironmentContext';

const WINDOWS: BurnRateWindow[] = ['OneHour', 'SixHours', 'TwentyFourHours', 'SevenDays'];
const SLO_TYPES: SloType[] = ['Availability', 'Latency', 'ErrorRate', 'Throughput'];

const defaultCreateForm = {
  environment: 'Production',
  name: '',
  type: 'Availability' as SloType,
  targetPercent: 99.9,
  windowDays: 30,
  alertThresholdPercent: 70,
};

function statusVariant(status: string): 'success' | 'warning' | 'danger' | 'default' {
  if (status === 'Healthy') return 'success';
  if (status === 'AtRisk') return 'warning';
  if (status === 'Violated') return 'danger';
  return 'default';
}

export function ReliabilitySloManagementPage() {
  const { t } = useTranslation();
  const { activeEnvironmentId } = useEnvironment();
  const [selectedService, setSelectedService] = useState('');
  const [selectedSloId, setSelectedSloId] = useState('');
  const [burnWindow, setBurnWindow] = useState<BurnRateWindow>('SixHours');
  const [createForm, setCreateForm] = useState(defaultCreateForm);

  const servicesQuery = useQuery({
    queryKey: ['reliability-services-for-slo', activeEnvironmentId],
    queryFn: () => reliabilityApi.listServices({ page: 1, pageSize: 200 }),
    staleTime: 30_000,
  });

  const resolvedSelectedService = useMemo(() => {
    if (selectedService) return selectedService;
    return servicesQuery.data?.items?.[0]?.serviceName ?? '';
  }, [selectedService, servicesQuery.data?.items]);

  const slosQuery = useQuery({
    queryKey: ['reliability-service-slos', resolvedSelectedService, activeEnvironmentId],
    queryFn: () => reliabilityApi.listServiceSlos(resolvedSelectedService),
    enabled: !!resolvedSelectedService,
  });

  const resolvedSelectedSloId = useMemo(() => {
    if (selectedSloId) return selectedSloId;
    return slosQuery.data?.items?.[0]?.id ?? '';
  }, [selectedSloId, slosQuery.data?.items]);

  const selectedSlo = useMemo(() => {
    return (slosQuery.data?.items ?? []).find((item) => item.id === resolvedSelectedSloId) ?? null;
  }, [slosQuery.data?.items, resolvedSelectedSloId]);

  const errorBudgetQuery = useQuery({
    queryKey: ['reliability-error-budget', resolvedSelectedSloId, activeEnvironmentId],
    queryFn: () => reliabilityApi.getErrorBudget(resolvedSelectedSloId),
    enabled: !!resolvedSelectedSloId,
  });

  const burnRateQuery = useQuery({
    queryKey: ['reliability-burn-rate', resolvedSelectedSloId, burnWindow, activeEnvironmentId],
    queryFn: () => reliabilityApi.getBurnRate(resolvedSelectedSloId, burnWindow),
    enabled: !!resolvedSelectedSloId,
  });

  const slasQuery = useQuery({
    queryKey: ['reliability-slas', resolvedSelectedSloId],
    queryFn: () => reliabilityApi.listSloSlas(resolvedSelectedSloId),
    enabled: !!resolvedSelectedSloId,
  });

  const createSloMutation = useMutation({
    mutationFn: () => reliabilityApi.registerSlo({
      serviceId: resolvedSelectedService,
      ...createForm,
    }),
    onSuccess: () => {
      slosQuery.refetch();
      setCreateForm(defaultCreateForm);
    },
  });

  const recomputeBudgetMutation = useMutation({
    mutationFn: () => reliabilityApi.computeErrorBudget(resolvedSelectedSloId),
    onSuccess: () => {
      errorBudgetQuery.refetch();
    },
  });

  const recomputeBurnMutation = useMutation({
    mutationFn: () => reliabilityApi.computeBurnRate(resolvedSelectedSloId, burnWindow),
    onSuccess: () => {
      burnRateQuery.refetch();
    },
  });

  if (servicesQuery.isLoading) {
    return (
      <PageContainer>
        <PageLoadingState />
      </PageContainer>
    );
  }

  if (servicesQuery.isError) {
    return (
      <PageContainer>
        <PageErrorState />
      </PageContainer>
    );
  }

  const slos = slosQuery.data?.items ?? [];
  const burnRateValue = burnRateQuery.data?.burnRate ?? null;
  const atRisk = (burnRateValue !== null && burnRateValue >= 1) || burnRateQuery.data?.status === 'AtRisk' || burnRateQuery.data?.status === 'Violated';

  return (
    <PageContainer>
      <PageHeader
        title={t('reliability.slo.title')}
        subtitle={t('reliability.slo.subtitle')}
      />

      <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
        <Card className="xl:col-span-1">
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading">{t('reliability.slo.serviceAndDefinitions')}</h2>
          </CardHeader>
          <CardBody className="space-y-4">
            <div>
              <label className="block text-xs text-muted mb-1">{t('reliability.slo.service')}</label>
              <select
                value={resolvedSelectedService}
                onChange={(e) => {
                  setSelectedService(e.target.value);
                  setSelectedSloId('');
                }}
                className="w-full px-3 py-2 rounded-md bg-canvas border border-edge text-sm text-heading"
              >
                {(servicesQuery.data?.items ?? []).map((service) => (
                  <option key={service.serviceName} value={service.serviceName}>
                    {service.displayName}
                  </option>
                ))}
              </select>
            </div>

            <div className="space-y-2">
              {slosQuery.isLoading ? (
                <p className="text-sm text-muted">{t('common.loading')}</p>
              ) : slos.length === 0 ? (
                <p className="text-sm text-muted">{t('reliability.slo.noSlo')}</p>
              ) : (
                slos.map((slo: ServiceSloItem) => (
                  <button
                    key={slo.id}
                    type="button"
                    onClick={() => setSelectedSloId(slo.id)}
                    className={`w-full text-left rounded-md border px-3 py-2 ${resolvedSelectedSloId === slo.id ? 'border-accent bg-accent/10' : 'border-edge bg-elevated'}`}
                  >
                    <div className="flex items-center justify-between">
                      <span className="text-sm text-heading font-medium">{slo.name}</span>
                      <Badge variant={slo.isActive ? 'success' : 'default'}>{slo.isActive ? t('reliability.slo.active') : t('reliability.slo.inactive')}</Badge>
                    </div>
                    <p className="text-xs text-muted mt-1">{slo.type} • {slo.targetPercent}% • {slo.environment}</p>
                  </button>
                ))
              )}
            </div>

            <div className="pt-3 border-t border-edge">
              <h3 className="text-xs uppercase tracking-wide text-muted mb-2">{t('reliability.slo.create')}</h3>
              <div className="space-y-2">
                <input
                  value={createForm.name}
                  onChange={(e) => setCreateForm((c) => ({ ...c, name: e.target.value }))}
                  placeholder={t('reliability.slo.namePlaceholder')}
                  className="w-full px-3 py-2 rounded-md bg-canvas border border-edge text-sm text-heading"
                />
                <input
                  value={createForm.environment}
                  onChange={(e) => setCreateForm((c) => ({ ...c, environment: e.target.value }))}
                  placeholder={t('reliability.slo.environment')}
                  className="w-full px-3 py-2 rounded-md bg-canvas border border-edge text-sm text-heading"
                />
                <select
                  value={createForm.type}
                  onChange={(e) => setCreateForm((c) => ({ ...c, type: e.target.value as SloType }))}
                  className="w-full px-3 py-2 rounded-md bg-canvas border border-edge text-sm text-heading"
                >
                  {SLO_TYPES.map((type) => <option key={type} value={type}>{type}</option>)}
                </select>
                <input
                  type="number"
                  value={createForm.targetPercent}
                  onChange={(e) => setCreateForm((c) => ({ ...c, targetPercent: Number(e.target.value) }))}
                  className="w-full px-3 py-2 rounded-md bg-canvas border border-edge text-sm text-heading"
                />
                <button
                  type="button"
                  onClick={() => createSloMutation.mutate()}
                  disabled={!resolvedSelectedService || !createForm.name || createSloMutation.isPending}
                  className="w-full px-3 py-2 rounded-md bg-accent/15 text-accent border border-accent/40 text-sm disabled:opacity-60"
                >
                  {createSloMutation.isPending ? t('common.loading') : t('reliability.slo.createAction')}
                </button>
              </div>
            </div>
          </CardBody>
        </Card>

        <Card className="xl:col-span-2">
          <CardHeader>
            <h2 className="text-sm font-semibold text-heading">{t('reliability.slo.health')}</h2>
          </CardHeader>
          <CardBody>
            {!selectedSlo ? (
              <p className="text-sm text-muted">{t('reliability.slo.selectSlo')}</p>
            ) : (
              <div className="space-y-4">
                <div className="flex items-center justify-between">
                  <div>
                    <p className="text-sm font-semibold text-heading">{selectedSlo.name}</p>
                    <p className="text-xs text-muted">{selectedSlo.environment} • {selectedSlo.type} • {selectedSlo.targetPercent}%</p>
                  </div>
                  <Badge variant={statusVariant(errorBudgetQuery.data?.status ?? 'Healthy')}>
                    {errorBudgetQuery.data?.status ?? 'Healthy'}
                  </Badge>
                </div>

                {atRisk && (
                  <div className="p-3 rounded-md border border-warning/30 bg-warning/10 flex items-center gap-2">
                    <AlertTriangle size={14} className="text-warning" />
                    <span className="text-sm text-warning">{t('reliability.slo.burnRateAlert')}</span>
                  </div>
                )}

                <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
                  <div className="p-3 rounded-md border border-edge bg-elevated">
                    <p className="text-xs text-muted">{t('reliability.slo.errorBudgetConsumed')}</p>
                    <p className="text-lg font-semibold text-heading">{errorBudgetQuery.data?.consumedPercent?.toFixed(1) ?? '0'}%</p>
                  </div>
                  <div className="p-3 rounded-md border border-edge bg-elevated">
                    <p className="text-xs text-muted">{t('reliability.slo.remainingBudget')}</p>
                    <p className="text-lg font-semibold text-heading">{errorBudgetQuery.data?.remainingBudgetMinutes?.toFixed(0) ?? '0'} min</p>
                  </div>
                  <div className="p-3 rounded-md border border-edge bg-elevated">
                    <p className="text-xs text-muted">{t('reliability.slo.burnRate')}</p>
                    <p className="text-lg font-semibold text-heading">{burnRateValue?.toFixed(2) ?? '0.00'}x</p>
                  </div>
                </div>

                <div className="flex flex-wrap items-center gap-2">
                  <select
                    value={burnWindow}
                    onChange={(e) => setBurnWindow(e.target.value as BurnRateWindow)}
                    className="px-3 py-2 rounded-md bg-canvas border border-edge text-sm text-heading"
                  >
                    {WINDOWS.map((window) => (
                      <option key={window} value={window}>{window}</option>
                    ))}
                  </select>
                  <button
                    type="button"
                    onClick={() => recomputeBudgetMutation.mutate()}
                    disabled={recomputeBudgetMutation.isPending}
                    className="px-3 py-2 rounded-md border border-edge text-sm text-muted hover:text-body"
                  >
                    {t('reliability.slo.recomputeBudget')}
                  </button>
                  <button
                    type="button"
                    onClick={() => recomputeBurnMutation.mutate()}
                    disabled={recomputeBurnMutation.isPending}
                    className="px-3 py-2 rounded-md border border-edge text-sm text-muted hover:text-body"
                  >
                    {t('reliability.slo.recomputeBurn')}
                  </button>
                </div>

                <div className="p-3 rounded-md border border-edge bg-elevated">
                  <p className="text-xs text-muted mb-1">{t('reliability.slo.linkedSlas')}</p>
                  <p className="text-sm text-body flex items-center gap-2">
                    <ShieldCheck size={14} className="text-accent" />
                    {slasQuery.data?.items?.length ?? 0} {t('reliability.slo.slaCount')}
                  </p>
                </div>
              </div>
            )}
          </CardBody>
        </Card>
      </div>
    </PageContainer>
  );
}
