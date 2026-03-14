/**
 * Página de Licensing — visualização do estado da licença, capabilities,
 * quotas de uso e gestão de trial.
 *
 * Segue o padrão de tabs do EngineeringGraphPage e a estrutura de formulários
 * do PromotionPage. Todo texto visível usa i18n via t('licensing.*').
 */
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  Shield,
  Key,
  BarChart3,
  FlaskConical,
  AlertTriangle,
  CheckCircle,
  XCircle,
  RefreshCw,
  Heart,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { licensingApi } from '../api';
import type {
  LicenseStatus,
  LicenseHealthResult,
  UsageQuotaStatus,
  CapabilityStatus,
  LicenseThresholdAlert,
} from '../../../types';

type Tab = 'status' | 'capabilities' | 'quotas' | 'trial';

/** Mapeia o nível de aviso para variante visual do Badge. */
function warningVariant(level: string): 'default' | 'success' | 'warning' | 'danger' | 'info' {
  if (level === 'Normal') return 'success';
  if (level === 'Advisory') return 'info';
  if (level === 'Warning') return 'warning';
  if (level === 'Critical' || level === 'Exceeded') return 'danger';
  return 'default';
}

/** Formulário de início de trial. */
interface TrialStartForm {
  customerName: string;
  email: string;
}

/** Formulário de extensão de trial. */
interface TrialExtendForm {
  licenseKey: string;
  additionalDays: number;
}

/** Formulário de conversão de trial. */
interface TrialConvertForm {
  licenseKey: string;
  edition: string;
  expiresAt: string;
  maxActivations: number;
  gracePeriodDays: number;
}

const emptyStartForm: TrialStartForm = { customerName: '', email: '' };
const emptyExtendForm: TrialExtendForm = { licenseKey: '', additionalDays: 14 };
const emptyConvertForm: TrialConvertForm = {
  licenseKey: '',
  edition: 'Professional',
  expiresAt: '',
  maxActivations: 5,
  gracePeriodDays: 15,
};

/** Chave da licença usada para consultas — mantida apenas em memória (segurança). */

export function LicensingPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<Tab>('status');
  const [licenseKey, setLicenseKey] = useState('');

  const [startForm, setStartForm] = useState<TrialStartForm>(emptyStartForm);
  const [extendForm, setExtendForm] = useState<TrialExtendForm>(emptyExtendForm);
  const [convertForm, setConvertForm] = useState<TrialConvertForm>(emptyConvertForm);

  // ── Atualizar chave apenas em memória — nunca persistir em storage ──
  const updateLicenseKey = (key: string) => {
    setLicenseKey(key);
  };

  // ── Queries ─────────────────────────────────────────────────────────
  const { data: status, isLoading: statusLoading, isError: statusError } = useQuery({
    queryKey: ['licensing', 'status', licenseKey],
    queryFn: () => licensingApi.getStatus(licenseKey),
    enabled: !!licenseKey && tab === 'status',
    staleTime: 30_000,
  });

  const { data: health, isLoading: healthLoading } = useQuery({
    queryKey: ['licensing', 'health', licenseKey],
    queryFn: () => licensingApi.getHealth(licenseKey),
    enabled: !!licenseKey && tab === 'status',
    staleTime: 30_000,
  });

  const { data: thresholds, isLoading: thresholdsLoading } = useQuery({
    queryKey: ['licensing', 'thresholds', licenseKey],
    queryFn: () => licensingApi.getThresholds(licenseKey),
    enabled: !!licenseKey && tab === 'quotas',
    staleTime: 15_000,
  });

  // ── Mutations ───────────────────────────────────────────────────────
  const startTrialMutation = useMutation({
    mutationFn: licensingApi.startTrial,
    onSuccess: (data) => {
      updateLicenseKey(data.licenseKey);
      queryClient.invalidateQueries({ queryKey: ['licensing'] });
      setStartForm(emptyStartForm);
    },
  });

  const extendTrialMutation = useMutation({
    mutationFn: licensingApi.extendTrial,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['licensing'] });
      setExtendForm(emptyExtendForm);
    },
  });

  const convertTrialMutation = useMutation({
    mutationFn: licensingApi.convertTrial,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['licensing'] });
      setConvertForm(emptyConvertForm);
    },
  });

  // ── Definição das tabs ──────────────────────────────────────────────
  const tabs: { key: Tab; label: string; icon: React.ReactNode }[] = [
    { key: 'status', label: t('licensing.tabs.status'), icon: <Shield size={14} /> },
    { key: 'capabilities', label: t('licensing.tabs.capabilities'), icon: <Key size={14} /> },
    { key: 'quotas', label: t('licensing.tabs.quotas'), icon: <BarChart3 size={14} /> },
    { key: 'trial', label: t('licensing.tabs.trial'), icon: <FlaskConical size={14} /> },
  ];

  return (
    <div className="p-6 lg:p-8 animate-fade-in">
      {/* Cabeçalho */}
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-heading">{t('licensing.title')}</h1>
          <p className="text-muted mt-1">{t('licensing.subtitle')}</p>
        </div>
      </div>

      {/* Seletor de License Key */}
      <Card className="mb-6">
        <CardBody>
          <div className="flex items-center gap-3">
            <label className="text-sm font-medium text-body whitespace-nowrap">
              {t('licensing.licenseKey')}
            </label>
            <input
              type="text"
              value={licenseKey}
              onChange={(e) => updateLicenseKey(e.target.value)}
              placeholder={t('licensing.licenseKeyPlaceholder')}
              className="flex-1 rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors font-mono"
            />
          </div>
        </CardBody>
      </Card>

      {/* Tabs */}
      <div className="flex gap-1 bg-elevated rounded-lg p-1 mb-6">
        {tabs.map((tabItem) => (
          <button
            key={tabItem.key}
            onClick={() => setTab(tabItem.key)}
            className={`flex items-center gap-1.5 px-3 py-1.5 rounded-md text-sm font-medium transition-colors ${
              tab === tabItem.key ? 'bg-card shadow text-heading' : 'text-muted hover:text-body'
            }`}
          >
            {tabItem.icon} {tabItem.label}
          </button>
        ))}
      </div>

      {/* ── Tab: Status ──────────────────────────────────────────────── */}
      {tab === 'status' && (
        <>
          {!licenseKey ? (
            <Card>
              <CardBody>
                <p className="text-sm text-muted text-center py-8">
                  {t('licensing.enterKeyPrompt')}
                </p>
              </CardBody>
            </Card>
          ) : statusLoading || healthLoading ? (
            <div className="flex items-center justify-center py-12">
              <RefreshCw size={20} className="animate-spin text-muted" />
            </div>
          ) : statusError ? (
            <Card>
              <CardBody>
                <p className="text-sm text-critical text-center py-8">
                  {t('licensing.loadFailed')}
                </p>
              </CardBody>
            </Card>
          ) : status ? (
            <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
              {/* Informações da licença */}
              <Card>
                <CardHeader>
                  <h2 className="font-semibold text-heading">{t('licensing.licenseInfo')}</h2>
                </CardHeader>
                <CardBody>
                  <dl className="space-y-3">
                    <div className="flex justify-between">
                      <dt className="text-sm text-muted">{t('licensing.customerName')}</dt>
                      <dd className="text-sm font-medium text-heading">{status.customerName}</dd>
                    </div>
                    <div className="flex justify-between">
                      <dt className="text-sm text-muted">{t('licensing.type')}</dt>
                      <dd><Badge variant="info">{status.type}</Badge></dd>
                    </div>
                    <div className="flex justify-between">
                      <dt className="text-sm text-muted">{t('licensing.edition')}</dt>
                      <dd><Badge variant="default">{status.edition}</Badge></dd>
                    </div>
                    <div className="flex justify-between">
                      <dt className="text-sm text-muted">{t('licensing.statusLabel')}</dt>
                      <dd>
                        <Badge variant={status.isActive ? 'success' : 'danger'}>
                          {status.isActive ? t('licensing.active') : t('licensing.inactive')}
                        </Badge>
                      </dd>
                    </div>
                    <div className="flex justify-between">
                      <dt className="text-sm text-muted">{t('licensing.expiresAt')}</dt>
                      <dd className="text-sm text-heading">
                        {new Date(status.expiresAt).toLocaleDateString()}
                      </dd>
                    </div>
                    <div className="flex justify-between">
                      <dt className="text-sm text-muted">{t('licensing.daysRemaining')}</dt>
                      <dd>
                        <Badge variant={status.daysUntilExpiration <= 7 ? 'danger' : status.daysUntilExpiration <= 30 ? 'warning' : 'success'}>
                          {status.daysUntilExpiration} {t('licensing.days')}
                        </Badge>
                      </dd>
                    </div>
                    {status.isInGracePeriod && (
                      <div className="flex items-center gap-2 mt-2 text-sm text-warning">
                        <AlertTriangle size={14} />
                        {t('licensing.gracePeriodWarning')}
                      </div>
                    )}
                    {status.trialConverted && (
                      <div className="flex items-center gap-2 mt-2 text-sm text-info">
                        <CheckCircle size={14} />
                        {t('licensing.trialConverted')}
                      </div>
                    )}
                  </dl>
                </CardBody>
              </Card>

              {/* Health Score */}
              {health && (
                <Card>
                  <CardHeader>
                    <div className="flex items-center gap-2">
                      <Heart size={16} className="text-accent" />
                      <h2 className="font-semibold text-heading">{t('licensing.healthScore')}</h2>
                    </div>
                  </CardHeader>
                  <CardBody>
                    <div className="flex items-center justify-center mb-4">
                      <div className="text-4xl font-bold text-heading">
                        {Math.round(health.healthScore * 100)}%
                      </div>
                    </div>
                    {health.quotaWarnings.length > 0 && (
                      <div className="space-y-2">
                        <h3 className="text-sm font-medium text-body">
                          {t('licensing.quotaWarnings')}
                        </h3>
                        {health.quotaWarnings.map((w) => (
                          <div key={w.metricCode} className="flex items-center justify-between text-sm">
                            <span className="text-muted font-mono">{w.metricCode}</span>
                            <Badge variant={warningVariant(w.warningLevel)}>{w.warningLevel}</Badge>
                          </div>
                        ))}
                      </div>
                    )}
                  </CardBody>
                </Card>
              )}
            </div>
          ) : null}
        </>
      )}

      {/* ── Tab: Capabilities ────────────────────────────────────────── */}
      {tab === 'capabilities' && (
        <Card>
          <CardHeader>
            <h2 className="font-semibold text-heading">{t('licensing.capabilitiesTitle')}</h2>
          </CardHeader>
          {!licenseKey ? (
            <CardBody>
              <p className="text-sm text-muted text-center py-8">
                {t('licensing.enterKeyPrompt')}
              </p>
            </CardBody>
          ) : statusLoading ? (
            <CardBody>
              <div className="flex items-center justify-center py-12">
                <RefreshCw size={20} className="animate-spin text-muted" />
              </div>
            </CardBody>
          ) : status?.capabilities?.length ? (
            <CardBody className="p-0">
              <ul className="divide-y divide-edge">
                {status.capabilities.map((cap: CapabilityStatus) => (
                  <li key={cap.code} className="px-6 py-3 flex items-center justify-between">
                    <div>
                      <p className="text-sm font-medium text-heading">{cap.name}</p>
                      <p className="text-xs text-muted font-mono">{cap.code}</p>
                    </div>
                    {cap.isEnabled ? (
                      <CheckCircle size={16} className="text-success" />
                    ) : (
                      <XCircle size={16} className="text-critical" />
                    )}
                  </li>
                ))}
              </ul>
            </CardBody>
          ) : (
            <CardBody>
              <p className="text-sm text-muted text-center py-8">
                {t('licensing.noCapabilities')}
              </p>
            </CardBody>
          )}
        </Card>
      )}

      {/* ── Tab: Quotas ──────────────────────────────────────────────── */}
      {tab === 'quotas' && (
        <div className="space-y-6">
          {/* Quotas de uso */}
          <Card>
            <CardHeader>
              <h2 className="font-semibold text-heading">{t('licensing.usageQuotas')}</h2>
            </CardHeader>
            {!licenseKey ? (
              <CardBody>
                <p className="text-sm text-muted text-center py-8">
                  {t('licensing.enterKeyPrompt')}
                </p>
              </CardBody>
            ) : statusLoading ? (
              <CardBody>
                <div className="flex items-center justify-center py-12">
                  <RefreshCw size={20} className="animate-spin text-muted" />
                </div>
              </CardBody>
            ) : status?.quotas?.length ? (
              <CardBody>
                <div className="space-y-4">
                  {status.quotas.map((q: UsageQuotaStatus) => (
                    <div key={q.metricCode}>
                      <div className="flex items-center justify-between mb-1">
                        <span className="text-sm font-medium text-heading font-mono">
                          {q.metricCode}
                        </span>
                        <div className="flex items-center gap-2">
                          <span className="text-xs text-muted">
                            {q.currentUsage} / {q.limit}
                          </span>
                          <Badge variant={warningVariant(q.warningLevel)}>{q.warningLevel}</Badge>
                        </div>
                      </div>
                      {/* Barra de progresso */}
                      <div className="w-full h-2 bg-elevated rounded-full overflow-hidden">
                        <div
                          className={`h-full rounded-full transition-all ${
                            q.usagePercentage >= 90
                              ? 'bg-critical'
                              : q.usagePercentage >= 70
                                ? 'bg-warning'
                                : 'bg-success'
                          }`}
                          style={{ width: `${Math.min(q.usagePercentage, 100)}%` }}
                        />
                      </div>
                      <div className="flex justify-between mt-1">
                        <span className="text-xs text-muted">
                          {t('licensing.enforcement')}: {q.enforcementLevel}
                        </span>
                        <span className="text-xs text-muted">
                          {Math.round(q.usagePercentage)}%
                        </span>
                      </div>
                    </div>
                  ))}
                </div>
              </CardBody>
            ) : (
              <CardBody>
                <p className="text-sm text-muted text-center py-8">
                  {t('licensing.noQuotas')}
                </p>
              </CardBody>
            )}
          </Card>

          {/* Alertas de threshold */}
          {thresholds && thresholds.length > 0 && (
            <Card>
              <CardHeader>
                <div className="flex items-center gap-2">
                  <AlertTriangle size={16} className="text-warning" />
                  <h2 className="font-semibold text-heading">{t('licensing.thresholdAlerts')}</h2>
                </div>
              </CardHeader>
              <CardBody className="p-0">
                <ul className="divide-y divide-edge">
                  {thresholds.map((alert: LicenseThresholdAlert) => (
                    <li key={alert.metricCode} className="px-6 py-3 flex items-center justify-between">
                      <div>
                        <p className="text-sm font-medium text-heading font-mono">{alert.metricCode}</p>
                        <p className="text-xs text-muted">
                          {alert.currentUsage} / {alert.limit} ({alert.thresholdPercentage}%)
                        </p>
                      </div>
                      <Badge variant={warningVariant(alert.warningLevel)}>{alert.warningLevel}</Badge>
                    </li>
                  ))}
                </ul>
              </CardBody>
            </Card>
          )}
        </div>
      )}

      {/* ── Tab: Trial ───────────────────────────────────────────────── */}
      {tab === 'trial' && (
        <div className="space-y-6">
          {/* Iniciar Trial */}
          <Card>
            <CardHeader>
              <h2 className="font-semibold text-heading">{t('licensing.startTrial')}</h2>
            </CardHeader>
            <CardBody>
              <form
                onSubmit={(e) => {
                  e.preventDefault();
                  startTrialMutation.mutate(startForm);
                }}
                className="grid grid-cols-1 md:grid-cols-2 gap-4"
              >
                <div>
                  <label className="block text-sm font-medium text-body mb-1">
                    {t('licensing.customerNameLabel')}
                  </label>
                  <input
                    type="text"
                    value={startForm.customerName}
                    onChange={(e) => setStartForm((f) => ({ ...f, customerName: e.target.value }))}
                    required
                    placeholder={t('licensing.customerNamePlaceholder')}
                    className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-body mb-1">
                    {t('licensing.emailLabel')}
                  </label>
                  <input
                    type="email"
                    value={startForm.email}
                    onChange={(e) => setStartForm((f) => ({ ...f, email: e.target.value }))}
                    required
                    placeholder={t('licensing.emailPlaceholder')}
                    className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                  />
                </div>
                <div className="md:col-span-2 flex justify-end">
                  <Button type="submit" loading={startTrialMutation.isPending}>
                    <FlaskConical size={14} /> {t('licensing.startTrialButton')}
                  </Button>
                </div>
              </form>
            </CardBody>
          </Card>

          {/* Estender Trial */}
          <Card>
            <CardHeader>
              <h2 className="font-semibold text-heading">{t('licensing.extendTrial')}</h2>
            </CardHeader>
            <CardBody>
              <form
                onSubmit={(e) => {
                  e.preventDefault();
                  extendTrialMutation.mutate(extendForm);
                }}
                className="grid grid-cols-1 md:grid-cols-2 gap-4"
              >
                <div>
                  <label className="block text-sm font-medium text-body mb-1">
                    {t('licensing.licenseKey')}
                  </label>
                  <input
                    type="text"
                    value={extendForm.licenseKey}
                    onChange={(e) => setExtendForm((f) => ({ ...f, licenseKey: e.target.value }))}
                    required
                    placeholder={t('licensing.licenseKeyPlaceholder')}
                    className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors font-mono"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-body mb-1">
                    {t('licensing.additionalDays')}
                  </label>
                  <input
                    type="number"
                    value={extendForm.additionalDays}
                    onChange={(e) =>
                      setExtendForm((f) => ({ ...f, additionalDays: Number(e.target.value) }))
                    }
                    required
                    min={1}
                    max={90}
                    className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                  />
                </div>
                <div className="md:col-span-2 flex justify-end">
                  <Button type="submit" loading={extendTrialMutation.isPending}>
                    {t('licensing.extendTrialButton')}
                  </Button>
                </div>
              </form>
            </CardBody>
          </Card>

          {/* Converter Trial */}
          <Card>
            <CardHeader>
              <h2 className="font-semibold text-heading">{t('licensing.convertTrial')}</h2>
            </CardHeader>
            <CardBody>
              <form
                onSubmit={(e) => {
                  e.preventDefault();
                  convertTrialMutation.mutate(convertForm);
                }}
                className="space-y-4"
              >
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">
                      {t('licensing.licenseKey')}
                    </label>
                    <input
                      type="text"
                      value={convertForm.licenseKey}
                      onChange={(e) =>
                        setConvertForm((f) => ({ ...f, licenseKey: e.target.value }))
                      }
                      required
                      placeholder={t('licensing.licenseKeyPlaceholder')}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading placeholder:text-muted focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors font-mono"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">
                      {t('licensing.editionLabel')}
                    </label>
                    <select
                      value={convertForm.edition}
                      onChange={(e) =>
                        setConvertForm((f) => ({ ...f, edition: e.target.value }))
                      }
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                    >
                      <option value="Community">{t('licensing.editions.community')}</option>
                      <option value="Professional">{t('licensing.editions.professional')}</option>
                      <option value="Enterprise">{t('licensing.editions.enterprise')}</option>
                      <option value="Unlimited">{t('licensing.editions.unlimited')}</option>
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">
                      {t('licensing.expiresAtLabel')}
                    </label>
                    <input
                      type="date"
                      value={convertForm.expiresAt}
                      onChange={(e) =>
                        setConvertForm((f) => ({ ...f, expiresAt: e.target.value }))
                      }
                      required
                      min={new Date().toISOString().split('T')[0]}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">
                      {t('licensing.maxActivations')}
                    </label>
                    <input
                      type="number"
                      value={convertForm.maxActivations}
                      onChange={(e) =>
                        setConvertForm((f) => ({ ...f, maxActivations: Number(e.target.value) }))
                      }
                      required
                      min={1}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">
                      {t('licensing.gracePeriodDays')}
                    </label>
                    <input
                      type="number"
                      value={convertForm.gracePeriodDays}
                      onChange={(e) =>
                        setConvertForm((f) => ({
                          ...f,
                          gracePeriodDays: Number(e.target.value),
                        }))
                      }
                      required
                      min={0}
                      max={90}
                      className="w-full rounded-md bg-canvas border border-edge px-3 py-2 text-sm text-heading focus:outline-none focus:ring-2 focus:ring-accent focus:border-accent transition-colors"
                    />
                  </div>
                </div>
                <div className="flex justify-end">
                  <Button type="submit" loading={convertTrialMutation.isPending}>
                    {t('licensing.convertTrialButton')}
                  </Button>
                </div>
              </form>
            </CardBody>
          </Card>
        </div>
      )}
    </div>
  );
}
