import { useMemo, useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery } from '@tanstack/react-query';
import { AlertTriangle, ShieldCheck, Plus, RefreshCw, Gauge, Clock, Flame } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { StatCard } from '../../../components/StatCard';
import { PageContainer, PageSection, ContentGrid } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { EmptyState } from '../../../components/EmptyState';
import { Button } from '../../../components/Button';
import { TextField } from '../../../components/TextField';
import { Select } from '../../../components/Select';
import { Drawer } from '../../../components/Drawer';
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

  // Estado do Drawer de criação
  const [isCreateOpen, setIsCreateOpen] = useState(false);

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
      setIsCreateOpen(false);
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

  // Valida campos obrigatórios do formulário
  const isFormValid = createForm.name.trim().length > 0 && !!resolvedSelectedService;

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

  // Opções do Select de serviço
  const serviceOptions = (servicesQuery.data?.items ?? []).map((s) => ({
    value: s.serviceName,
    label: s.displayName,
  }));

  // Opções do Select de tipo de SLO
  const sloTypeOptions = SLO_TYPES.map((type) => ({ value: type, label: type }));

  // Opções do Select de janela de burn rate
  const windowOptions = WINDOWS.map((w) => ({ value: w, label: w }));

  return (
    <PageContainer>
      {/* Cabeçalho com CTA de criação de SLO */}
      <PageHeader
        title={t('reliability.slo.title')}
        subtitle={t('reliability.slo.subtitle')}
        actions={
          <Button
            variant="primary"
            size="sm"
            icon={<Plus size={15} />}
            onClick={() => setIsCreateOpen(true)}
            disabled={!resolvedSelectedService}
          >
            {t('reliability.slo.createAction')}
          </Button>
        }
      />

      {/* Drawer lateral de criação de SLO */}
      <Drawer
        open={isCreateOpen}
        onClose={() => {
          setIsCreateOpen(false);
          setCreateForm(defaultCreateForm);
        }}
        title={t('reliability.slo.create')}
        description={t('reliability.slo.subtitle')}
        size="md"
        footer={
          <>
            <Button
              variant="outline"
              size="sm"
              onClick={() => {
                setIsCreateOpen(false);
                setCreateForm(defaultCreateForm);
              }}
            >
              {t('common.cancel', 'Cancel')}
            </Button>
            <Button
              variant="primary"
              size="sm"
              disabled={!isFormValid}
              loading={createSloMutation.isPending}
              onClick={() => createSloMutation.mutate()}
            >
              {t('reliability.slo.createAction')}
            </Button>
          </>
        }
      >
        <div className="flex flex-col gap-4">
          {/* Nome do SLO */}
          <TextField
            label={t('reliability.slo.namePlaceholder')}
            value={createForm.name}
            onChange={(e) => setCreateForm((c) => ({ ...c, name: e.target.value }))}
            placeholder={t('reliability.slo.namePlaceholder')}
          />

          {/* Ambiente */}
          <TextField
            label={t('reliability.slo.environment')}
            value={createForm.environment}
            onChange={(e) => setCreateForm((c) => ({ ...c, environment: e.target.value }))}
            placeholder={t('reliability.slo.environment')}
          />

          {/* Tipo de SLO */}
          <Select
            label="Type"
            value={createForm.type}
            onChange={(e) => setCreateForm((c) => ({ ...c, type: e.target.value as SloType }))}
            options={sloTypeOptions}
            size="md"
          />

          {/* Target % */}
          <TextField
            label="Target %"
            type="number"
            value={String(createForm.targetPercent)}
            onChange={(e) => setCreateForm((c) => ({ ...c, targetPercent: Number(e.target.value) }))}
          />
        </div>
      </Drawer>

      {/* Grelha principal: seleção de serviço/SLO à esquerda, painel de saúde à direita */}
      <PageSection>
        <div className="grid grid-cols-1 xl:grid-cols-3 gap-6">
          {/* Coluna de seleção — serviço + lista de SLOs */}
          <Card className="xl:col-span-1">
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading">{t('reliability.slo.serviceAndDefinitions')}</h2>
            </CardHeader>
            <CardBody className="space-y-4">
              {/* Selector de serviço com componente DS */}
              <Select
                label={t('reliability.slo.service')}
                value={resolvedSelectedService}
                onChange={(e) => {
                  setSelectedService(e.target.value);
                  setSelectedSloId('');
                }}
                options={serviceOptions}
                size="md"
              />

              {/* Lista de SLOs do serviço selecionado */}
              <div className="space-y-2">
                {slosQuery.isLoading ? (
                  <p className="text-sm text-muted">{t('common.loading')}</p>
                ) : slos.length === 0 ? (
                  <EmptyState
                    size="compact"
                    title={t('reliability.slo.noSlo')}
                    variant="onboarding"
                    action={
                      <Button
                        variant="subtle"
                        size="xs"
                        icon={<Plus size={13} />}
                        onClick={() => setIsCreateOpen(true)}
                        disabled={!resolvedSelectedService}
                      >
                        {t('reliability.slo.createAction')}
                      </Button>
                    }
                  />
                ) : (
                  slos.map((slo: ServiceSloItem) => (
                    <Button
                      key={slo.id}
                      variant="ghost"
                      onClick={() => setSelectedSloId(slo.id)}
                      className={`w-full h-auto flex-col items-start justify-start gap-0 rounded-md border px-3 py-2 ${resolvedSelectedSloId === slo.id ? 'border-accent bg-accent/10' : 'border-edge bg-elevated hover:border-edge-strong hover:bg-hover'}`}
                    >
                      <div className="flex w-full items-center justify-between">
                        <span className="text-sm text-heading font-medium">{slo.name}</span>
                        <Badge variant={slo.isActive ? 'success' : 'default'}>
                          {slo.isActive ? t('reliability.slo.active') : t('reliability.slo.inactive')}
                        </Badge>
                      </div>
                      <p className="text-xs text-muted mt-1">{slo.type} • {slo.targetPercent}% • {slo.environment}</p>
                    </Button>
                  ))
                )}
              </div>
            </CardBody>
          </Card>

          {/* Coluna de saúde do SLO selecionado */}
          <Card className="xl:col-span-2">
            <CardHeader>
              <h2 className="text-sm font-semibold text-heading">{t('reliability.slo.health')}</h2>
            </CardHeader>
            <CardBody>
              {!selectedSlo ? (
                <EmptyState
                  title={t('reliability.slo.selectSlo')}
                  variant="default"
                />
              ) : (
                <div className="space-y-4">
                  {/* Identidade do SLO + status */}
                  <div className="flex items-center justify-between">
                    <div>
                      <p className="text-sm font-semibold text-heading">{selectedSlo.name}</p>
                      <p className="text-xs text-muted">{selectedSlo.environment} • {selectedSlo.type} • {selectedSlo.targetPercent}%</p>
                    </div>
                    <Badge variant={statusVariant(errorBudgetQuery.data?.status ?? 'Healthy')}>
                      {errorBudgetQuery.data?.status ?? 'Healthy'}
                    </Badge>
                  </div>

                  {/* Banner de alerta de burn rate elevado */}
                  {atRisk && (
                    <div className="p-3 rounded-md border border-warning/30 bg-warning/10 flex items-center gap-2">
                      <AlertTriangle size={14} className="text-warning shrink-0" />
                      <span className="text-sm text-warning">{t('reliability.slo.burnRateAlert')}</span>
                    </div>
                  )}

                  {/* KPIs via StatCard + ContentGrid */}
                  <ContentGrid columns={3}>
                    <StatCard
                      title={t('reliability.slo.errorBudgetConsumed')}
                      value={`${errorBudgetQuery.data?.consumedPercent?.toFixed(1) ?? '0'}%`}
                      icon={<Gauge size={18} />}
                      color={atRisk ? 'text-critical' : 'text-accent'}
                    />
                    <StatCard
                      title={t('reliability.slo.remainingBudget')}
                      value={`${errorBudgetQuery.data?.remainingBudgetMinutes?.toFixed(0) ?? '0'} min`}
                      icon={<Clock size={18} />}
                      color="text-success"
                    />
                    <StatCard
                      title={t('reliability.slo.burnRate')}
                      value={`${burnRateValue?.toFixed(2) ?? '0.00'}x`}
                      icon={<Flame size={18} />}
                      color={atRisk ? 'text-warning' : 'text-info'}
                    />
                  </ContentGrid>

                  {/* Controles de janela temporal e recomputo */}
                  <div className="flex flex-wrap items-end gap-3">
                    <Select
                      value={burnWindow}
                      onChange={(e) => setBurnWindow(e.target.value as BurnRateWindow)}
                      options={windowOptions}
                      size="sm"
                      className="w-44"
                    />
                    <Button
                      variant="outline"
                      size="sm"
                      icon={<RefreshCw size={14} />}
                      onClick={() => recomputeBudgetMutation.mutate()}
                      loading={recomputeBudgetMutation.isPending}
                    >
                      {t('reliability.slo.recomputeBudget')}
                    </Button>
                    <Button
                      variant="outline"
                      size="sm"
                      icon={<RefreshCw size={14} />}
                      onClick={() => recomputeBurnMutation.mutate()}
                      loading={recomputeBurnMutation.isPending}
                    >
                      {t('reliability.slo.recomputeBurn')}
                    </Button>
                  </div>

                  {/* SLAs vinculados */}
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
      </PageSection>
    </PageContainer>
  );
}
