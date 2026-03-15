/**
 * Página de Vendor Licensing Operations — backoffice interno da NexTraceOne.
 *
 * Separada da experiência do tenant para garantir que operações internas
 * (emissão, revogação, rehost, gestão de trials) fiquem restritas ao backoffice.
 *
 * Todo texto visível usa i18n via t('vendorLicensing.*').
 * Permissão requerida: licensing:vendor:license:read
 */
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  Shield,
  Plus,
  XCircle,
  RefreshCw,
  Key,
  Building2,
  AlertTriangle,
  Package,
  Layers,
  Key as KeyIcon,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { licensingApi } from '../api';
import type { VendorLicenseItem, PlanItem, FeaturePackItem as FeaturePackItemType } from '../api/licensing';

type Tab = 'licenses' | 'issue' | 'plans' | 'featurePacks' | 'generateKey';

/** Mapeia o status da licença para variante visual do Badge. */
function statusVariant(status: string): 'default' | 'success' | 'warning' | 'danger' | 'info' {
  if (status === 'Active') return 'success';
  if (status === 'GracePeriod') return 'warning';
  if (status === 'PendingActivation') return 'info';
  if (status === 'Expired' || status === 'Revoked' || status === 'Suspended') return 'danger';
  return 'default';
}

/** Formulário de emissão de licença. */
interface IssueForm {
  customerName: string;
  durationDays: number;
  maxActivations: number;
  edition: number;
  deploymentModel: number;
  gracePeriodDays: number;
}

const defaultIssueForm: IssueForm = {
  customerName: '',
  durationDays: 365,
  maxActivations: 5,
  edition: 1,
  deploymentModel: 0,
  gracePeriodDays: 15,
};

export function VendorLicensingPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<Tab>('licenses');
  const [page, setPage] = useState(1);
  const [issueForm, setIssueForm] = useState<IssueForm>(defaultIssueForm);

  const licensesQuery = useQuery({
    queryKey: ['vendor-licenses', page],
    queryFn: () => licensingApi.vendorListLicenses(page, 20),
    staleTime: 15_000,
  });

  const issueMutation = useMutation({
    mutationFn: licensingApi.vendorIssueLicense,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['vendor-licenses'] });
      setIssueForm(defaultIssueForm);
      setActiveTab('licenses');
    },
  });

  const revokeMutation = useMutation({
    mutationFn: licensingApi.vendorRevokeLicense,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['vendor-licenses'] }),
  });

  const rehostMutation = useMutation({
    mutationFn: licensingApi.vendorRehostLicense,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['vendor-licenses'] }),
  });

  const plansQuery = useQuery({
    queryKey: ['vendor-plans'],
    queryFn: () => licensingApi.vendorListPlans(),
    staleTime: 30_000,
  });

  const featurePacksQuery = useQuery({
    queryKey: ['vendor-feature-packs'],
    queryFn: () => licensingApi.vendorListFeaturePacks(),
    staleTime: 30_000,
  });

  const generateKeyMutation = useMutation({
    mutationFn: licensingApi.vendorGenerateKey,
  });

  const [generateKeyForm, setGenerateKeyForm] = useState({ licenseId: '' });
  const [generatedKey, setGeneratedKey] = useState<string | null>(null);

  const tabs = [
    { key: 'licenses' as Tab, label: t('vendorLicensing.tabs.licenses'), icon: Key },
    { key: 'issue' as Tab, label: t('vendorLicensing.tabs.issue'), icon: Plus },
    { key: 'plans' as Tab, label: t('vendorCatalog.tabs.plans'), icon: Layers },
    { key: 'featurePacks' as Tab, label: t('vendorCatalog.tabs.featurePacks'), icon: Package },
    { key: 'generateKey' as Tab, label: t('vendorCatalog.tabs.generateKey'), icon: KeyIcon },
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Shield className="h-8 w-8 text-accent" />
        <div>
          <h1 className="text-2xl font-bold text-heading">{t('vendorLicensing.title')}</h1>
          <p className="text-sm text-muted">{t('vendorLicensing.subtitle')}</p>
        </div>
      </div>

      {/* Tabs */}
      <div className="border-b border-edge">
        <nav className="-mb-px flex space-x-8">
          {tabs.map(({ key, label, icon: Icon }) => (
            <button
              key={key}
              onClick={() => setActiveTab(key)}
              className={`flex items-center gap-2 border-b-2 py-3 px-1 text-sm font-medium ${
                activeTab === key
                  ? 'border-accent text-accent'
                  : 'border-transparent text-muted hover:border-edge hover:text-heading'
              }`}
            >
              <Icon className="h-4 w-4" />
              {label}
            </button>
          ))}
        </nav>
      </div>

      {/* Tab: Licenses */}
      {activeTab === 'licenses' && (
        <Card>
          <CardHeader>
            <div className="flex items-center justify-between">
              <h2 className="text-lg font-semibold">{t('vendorLicensing.licenseList')}</h2>
              <Button
                variant="ghost"
                size="sm"
                onClick={() => licensesQuery.refetch()}
              >
                <RefreshCw className="h-4 w-4 mr-1" />
                {t('common.refresh')}
              </Button>
            </div>
          </CardHeader>
          <CardBody>
            {licensesQuery.isLoading && (
              <p className="text-muted">{t('common.loading')}</p>
            )}
            {licensesQuery.isError && (
              <p className="text-red-400">{t('vendorLicensing.loadFailed')}</p>
            )}
            {licensesQuery.data && licensesQuery.data.items.length === 0 && (
              <p className="text-muted">{t('vendorLicensing.noLicenses')}</p>
            )}
            {licensesQuery.data && licensesQuery.data.items.length > 0 && (
              <>
                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-edge">
                    <thead className="bg-panel">
                      <tr>
                        <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase">
                          {t('vendorLicensing.customer')}
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase">
                          {t('vendorLicensing.licenseKeyCol')}
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase">
                          {t('vendorLicensing.typeCol')}
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase">
                          {t('vendorLicensing.deploymentCol')}
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase">
                          {t('vendorLicensing.statusCol')}
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase">
                          {t('vendorLicensing.expiresCol')}
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase">
                          {t('common.actions')}
                        </th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-edge">
                      {licensesQuery.data.items.map((lic: VendorLicenseItem) => (
                        <tr key={lic.licenseId}>
                          <td className="px-4 py-3 text-sm">
                            <div className="flex items-center gap-2">
                              <Building2 className="h-4 w-4 text-muted" />
                              {lic.customerName}
                            </div>
                          </td>
                          <td className="px-4 py-3 text-sm font-mono text-muted">
                            {lic.licenseKey}
                          </td>
                          <td className="px-4 py-3 text-sm">
                            <Badge variant={lic.isTrial ? 'warning' : 'default'}>
                              {lic.licenseType} / {lic.edition}
                            </Badge>
                          </td>
                          <td className="px-4 py-3 text-sm">{lic.deploymentModel}</td>
                          <td className="px-4 py-3 text-sm">
                            <Badge variant={statusVariant(lic.status)}>{lic.status}</Badge>
                          </td>
                          <td className="px-4 py-3 text-sm">
                            {new Date(lic.expiresAt).toLocaleDateString()}
                          </td>
                          <td className="px-4 py-3 text-sm">
                            <div className="flex gap-2">
                              {lic.isActive && (
                                <>
                                  <Button
                                    variant="ghost"
                                    size="sm"
                                    onClick={() => rehostMutation.mutate(lic.licenseKey)}
                                    disabled={rehostMutation.isPending}
                                  >
                                    <RefreshCw className="h-3 w-3 mr-1" />
                                    {t('vendorLicensing.rehost')}
                                  </Button>
                                  <Button
                                    variant="danger"
                                    size="sm"
                                    onClick={() => {
                                      if (window.confirm(t('vendorLicensing.confirmRevoke'))) {
                                        revokeMutation.mutate(lic.licenseKey);
                                      }
                                    }}
                                    disabled={revokeMutation.isPending}
                                  >
                                    <XCircle className="h-3 w-3 mr-1" />
                                    {t('vendorLicensing.revoke')}
                                  </Button>
                                </>
                              )}
                              {!lic.isActive && (
                                <Badge variant="danger">
                                  <AlertTriangle className="h-3 w-3 mr-1" />
                                  {lic.status}
                                </Badge>
                              )}
                            </div>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>

                {/* Paginação */}
                <div className="mt-4 flex items-center justify-between">
                  <span className="text-sm text-muted">
                    {t('vendorLicensing.totalLicenses', { count: licensesQuery.data.totalCount })}
                  </span>
                  <div className="flex gap-2">
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setPage((p) => Math.max(1, p - 1))}
                      disabled={page <= 1}
                    >
                      {t('common.back')}
                    </Button>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => setPage((p) => p + 1)}
                      disabled={
                        !licensesQuery.data ||
                        page * 20 >= licensesQuery.data.totalCount
                      }
                    >
                      {t('common.next')}
                    </Button>
                  </div>
                </div>
              </>
            )}
          </CardBody>
        </Card>
      )}

      {/* Tab: Issue License */}
      {activeTab === 'issue' && (
        <Card>
          <CardHeader>
            <h2 className="text-lg font-semibold">{t('vendorLicensing.issueLicense')}</h2>
          </CardHeader>
          <CardBody>
            <form
              className="space-y-4 max-w-lg"
              onSubmit={(e) => {
                e.preventDefault();
                issueMutation.mutate({
                  customerName: issueForm.customerName,
                  durationDays: issueForm.durationDays,
                  maxActivations: issueForm.maxActivations,
                  edition: issueForm.edition,
                  deploymentModel: issueForm.deploymentModel,
                  gracePeriodDays: issueForm.gracePeriodDays,
                });
              }}
            >
              <div>
                <label className="block text-sm font-medium text-body">
                  {t('vendorLicensing.customerNameLabel')}
                </label>
                <input
                  type="text"
                  className="mt-1 block w-full rounded-md border-edge bg-elevated text-body shadow-sm focus:border-accent focus:ring-accent sm:text-sm"
                  value={issueForm.customerName}
                  onChange={(e) => setIssueForm({ ...issueForm, customerName: e.target.value })}
                  placeholder={t('vendorLicensing.customerNamePlaceholder')}
                  required
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-body">
                    {t('vendorLicensing.durationDays')}
                  </label>
                  <input
                    type="number"
                    className="mt-1 block w-full rounded-md border-edge bg-elevated text-body shadow-sm focus:border-accent focus:ring-accent sm:text-sm"
                    value={issueForm.durationDays}
                    onChange={(e) =>
                      setIssueForm({ ...issueForm, durationDays: Number(e.target.value) })
                    }
                    min={1}
                    max={3650}
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-body">
                    {t('vendorLicensing.maxActivationsLabel')}
                  </label>
                  <input
                    type="number"
                    className="mt-1 block w-full rounded-md border-edge bg-elevated text-body shadow-sm focus:border-accent focus:ring-accent sm:text-sm"
                    value={issueForm.maxActivations}
                    onChange={(e) =>
                      setIssueForm({ ...issueForm, maxActivations: Number(e.target.value) })
                    }
                    min={1}
                  />
                </div>
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-body">
                    {t('vendorLicensing.editionLabel')}
                  </label>
                  <select
                    className="mt-1 block w-full rounded-md border-edge bg-elevated text-body shadow-sm focus:border-accent focus:ring-accent sm:text-sm"
                    value={issueForm.edition}
                    onChange={(e) =>
                      setIssueForm({ ...issueForm, edition: Number(e.target.value) })
                    }
                  >
                    <option value={0}>{t('licensing.editions.community')}</option>
                    <option value={1}>{t('licensing.editions.professional')}</option>
                    <option value={2}>{t('licensing.editions.enterprise')}</option>
                    <option value={3}>{t('licensing.editions.unlimited')}</option>
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-body">
                    {t('vendorLicensing.deploymentModelLabel')}
                  </label>
                  <select
                    className="mt-1 block w-full rounded-md border-edge bg-elevated text-body shadow-sm focus:border-accent focus:ring-accent sm:text-sm"
                    value={issueForm.deploymentModel}
                    onChange={(e) =>
                      setIssueForm({ ...issueForm, deploymentModel: Number(e.target.value) })
                    }
                  >
                    <option value={0}>{t('vendorLicensing.deploymentModels.saas')}</option>
                    <option value={1}>{t('vendorLicensing.deploymentModels.selfHosted')}</option>
                    <option value={2}>{t('vendorLicensing.deploymentModels.onPremise')}</option>
                  </select>
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-body">
                  {t('vendorLicensing.gracePeriodDaysLabel')}
                </label>
                <input
                  type="number"
                  className="mt-1 block w-full rounded-md border-edge bg-elevated text-body shadow-sm focus:border-accent focus:ring-accent sm:text-sm"
                  value={issueForm.gracePeriodDays}
                  onChange={(e) =>
                    setIssueForm({ ...issueForm, gracePeriodDays: Number(e.target.value) })
                  }
                  min={0}
                />
              </div>

              {issueMutation.isError && (
                <p className="text-sm text-red-400">{t('vendorLicensing.issueFailed')}</p>
              )}
              {issueMutation.isSuccess && (
                <p className="text-sm text-success">{t('vendorLicensing.issueSuccess')}</p>
              )}

              <Button type="submit" disabled={issueMutation.isPending}>
                <Plus className="h-4 w-4 mr-1" />
                {t('vendorLicensing.issueButton')}
              </Button>
            </form>
          </CardBody>
        </Card>
      )}

      {/* Tab: Plans */}
      {activeTab === 'plans' && (
        <Card>
          <CardHeader>
            <h2 className="text-lg font-semibold">{t('vendorCatalog.plans.title')}</h2>
          </CardHeader>
          <CardBody>
            {plansQuery.isLoading && <p className="text-muted">{t('common.loading')}</p>}
            {plansQuery.isError && <p className="text-red-400">{t('vendorCatalog.plans.loadFailed')}</p>}
            {plansQuery.data && plansQuery.data.length === 0 && (
              <p className="text-muted">{t('vendorCatalog.plans.noPlans')}</p>
            )}
            {plansQuery.data && plansQuery.data.length > 0 && (
              <div className="overflow-x-auto">
                <table className="min-w-full divide-y divide-edge">
                  <thead className="bg-panel">
                    <tr>
                      <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase">{t('vendorCatalog.plans.code')}</th>
                      <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase">{t('vendorCatalog.plans.name')}</th>
                      <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase">{t('vendorCatalog.plans.commercialModel')}</th>
                      <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase">{t('vendorCatalog.plans.deploymentModel')}</th>
                      <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase">{t('vendorCatalog.plans.maxActivations')}</th>
                      <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase">{t('vendorCatalog.plans.priceTag')}</th>
                      <th className="px-4 py-3 text-left text-xs font-medium text-muted uppercase">{t('vendorCatalog.plans.status')}</th>
                    </tr>
                  </thead>
                  <tbody className="divide-y divide-edge">
                    {plansQuery.data.map((plan: PlanItem) => (
                      <tr key={plan.planId}>
                        <td className="px-4 py-3 text-sm font-mono">{plan.code}</td>
                        <td className="px-4 py-3 text-sm font-medium">{plan.name}</td>
                        <td className="px-4 py-3 text-sm">{plan.commercialModel}</td>
                        <td className="px-4 py-3 text-sm">{plan.deploymentModel}</td>
                        <td className="px-4 py-3 text-sm">{plan.maxActivations}</td>
                        <td className="px-4 py-3 text-sm">{plan.priceTag || '—'}</td>
                        <td className="px-4 py-3 text-sm">
                          <Badge variant={plan.isActive ? 'success' : 'default'}>
                            {plan.isActive ? t('vendorCatalog.plans.active') : t('vendorCatalog.plans.inactive')}
                          </Badge>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </CardBody>
        </Card>
      )}

      {/* Tab: Feature Packs */}
      {activeTab === 'featurePacks' && (
        <Card>
          <CardHeader>
            <h2 className="text-lg font-semibold">{t('vendorCatalog.featurePacks.title')}</h2>
          </CardHeader>
          <CardBody>
            {featurePacksQuery.isLoading && <p className="text-muted">{t('common.loading')}</p>}
            {featurePacksQuery.isError && <p className="text-red-400">{t('vendorCatalog.featurePacks.loadFailed')}</p>}
            {featurePacksQuery.data && featurePacksQuery.data.length === 0 && (
              <p className="text-muted">{t('vendorCatalog.featurePacks.noPacks')}</p>
            )}
            {featurePacksQuery.data && featurePacksQuery.data.length > 0 && (
              <div className="space-y-4">
                {featurePacksQuery.data.map((pack: FeaturePackItemType) => (
                  <div key={pack.featurePackId} className="border rounded-lg p-4">
                    <div className="flex items-center justify-between mb-2">
                      <div>
                        <h3 className="text-sm font-semibold">{pack.name}</h3>
                        <span className="text-xs font-mono text-muted">{pack.code}</span>
                      </div>
                      <Badge variant={pack.isActive ? 'success' : 'default'}>
                        {pack.isActive ? t('vendorCatalog.featurePacks.active') : t('vendorCatalog.featurePacks.inactive')}
                      </Badge>
                    </div>
                    {pack.description && <p className="text-sm text-muted mb-2">{pack.description}</p>}
                    <div className="text-xs text-muted">
                      {t('vendorCatalog.featurePacks.itemCount', { count: pack.items?.length || 0 })}
                    </div>
                    {pack.items && pack.items.length > 0 && (
                      <div className="mt-2 flex flex-wrap gap-1">
                        {pack.items.map((item) => (
                          <Badge key={item.capabilityCode} variant="info">
                            {item.capabilityName}
                            {item.defaultLimit != null && ` (≤${item.defaultLimit})`}
                          </Badge>
                        ))}
                      </div>
                    )}
                  </div>
                ))}
              </div>
            )}
          </CardBody>
        </Card>
      )}

      {/* Tab: Generate Key */}
      {activeTab === 'generateKey' && (
        <Card>
          <CardHeader>
            <h2 className="text-lg font-semibold">{t('vendorCatalog.generateKey.title')}</h2>
          </CardHeader>
          <CardBody>
            <form
              className="space-y-4 max-w-lg"
              onSubmit={(e) => {
                e.preventDefault();
                generateKeyMutation.mutate(
                  { licenseId: generateKeyForm.licenseId },
                  {
                    onSuccess: (data) => setGeneratedKey(data.newLicenseKey),
                  },
                );
              }}
            >
              <div>
                <label className="block text-sm font-medium text-body">
                  {t('vendorCatalog.generateKey.licenseId')}
                </label>
                <input
                  type="text"
                  className="mt-1 block w-full rounded-md border-edge bg-elevated text-body shadow-sm focus:border-accent focus:ring-accent sm:text-sm"
                  value={generateKeyForm.licenseId}
                  onChange={(e) => setGenerateKeyForm({ licenseId: e.target.value })}
                  placeholder={t('vendorCatalog.generateKey.licenseIdPlaceholder')}
                  required
                />
              </div>

              {generateKeyMutation.isError && (
                <p className="text-sm text-red-400">{t('vendorCatalog.generateKey.generateFailed')}</p>
              )}

              {generatedKey && (
                <div className="rounded-md bg-success/10 p-4">
                  <p className="text-sm text-success mb-2">{t('vendorCatalog.generateKey.generateSuccess')}</p>
                  <div className="flex items-center gap-2">
                    <code className="text-sm font-mono bg-elevated px-3 py-2 rounded border flex-1">
                      {generatedKey}
                    </code>
                    <Button
                      variant="ghost"
                      size="sm"
                      onClick={() => {
                        navigator.clipboard.writeText(generatedKey);
                      }}
                    >
                      {t('vendorCatalog.generateKey.copyKey')}
                    </Button>
                  </div>
                </div>
              )}

              <Button type="submit" disabled={generateKeyMutation.isPending}>
                <KeyIcon className="h-4 w-4 mr-1" />
                {t('vendorCatalog.generateKey.generate')}
              </Button>
            </form>
          </CardBody>
        </Card>
      )}
    </div>
  );
}
