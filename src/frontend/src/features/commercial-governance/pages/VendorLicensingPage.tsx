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
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { licensingApi } from '../api';
import type { VendorLicenseItem } from '../api/licensing';

type Tab = 'licenses' | 'issue';

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

  const tabs = [
    { key: 'licenses' as Tab, label: t('vendorLicensing.tabs.licenses'), icon: Key },
    { key: 'issue' as Tab, label: t('vendorLicensing.tabs.issue'), icon: Plus },
  ];

  return (
    <div className="space-y-6">
      <div className="flex items-center gap-3">
        <Shield className="h-8 w-8 text-indigo-600" />
        <div>
          <h1 className="text-2xl font-bold text-gray-900">{t('vendorLicensing.title')}</h1>
          <p className="text-sm text-gray-500">{t('vendorLicensing.subtitle')}</p>
        </div>
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200">
        <nav className="-mb-px flex space-x-8">
          {tabs.map(({ key, label, icon: Icon }) => (
            <button
              key={key}
              onClick={() => setActiveTab(key)}
              className={`flex items-center gap-2 border-b-2 py-3 px-1 text-sm font-medium ${
                activeTab === key
                  ? 'border-indigo-500 text-indigo-600'
                  : 'border-transparent text-gray-500 hover:border-gray-300 hover:text-gray-700'
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
              <p className="text-gray-500">{t('common.loading')}</p>
            )}
            {licensesQuery.isError && (
              <p className="text-red-600">{t('vendorLicensing.loadFailed')}</p>
            )}
            {licensesQuery.data && licensesQuery.data.items.length === 0 && (
              <p className="text-gray-500">{t('vendorLicensing.noLicenses')}</p>
            )}
            {licensesQuery.data && licensesQuery.data.items.length > 0 && (
              <>
                <div className="overflow-x-auto">
                  <table className="min-w-full divide-y divide-gray-200">
                    <thead className="bg-gray-50">
                      <tr>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                          {t('vendorLicensing.customer')}
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                          {t('vendorLicensing.licenseKeyCol')}
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                          {t('vendorLicensing.typeCol')}
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                          {t('vendorLicensing.deploymentCol')}
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                          {t('vendorLicensing.statusCol')}
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                          {t('vendorLicensing.expiresCol')}
                        </th>
                        <th className="px-4 py-3 text-left text-xs font-medium text-gray-500 uppercase">
                          {t('common.actions')}
                        </th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-200">
                      {licensesQuery.data.items.map((lic: VendorLicenseItem) => (
                        <tr key={lic.licenseId}>
                          <td className="px-4 py-3 text-sm">
                            <div className="flex items-center gap-2">
                              <Building2 className="h-4 w-4 text-gray-400" />
                              {lic.customerName}
                            </div>
                          </td>
                          <td className="px-4 py-3 text-sm font-mono text-gray-600">
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
                  <span className="text-sm text-gray-500">
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
                <label className="block text-sm font-medium text-gray-700">
                  {t('vendorLicensing.customerNameLabel')}
                </label>
                <input
                  type="text"
                  className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                  value={issueForm.customerName}
                  onChange={(e) => setIssueForm({ ...issueForm, customerName: e.target.value })}
                  placeholder={t('vendorLicensing.customerNamePlaceholder')}
                  required
                />
              </div>

              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    {t('vendorLicensing.durationDays')}
                  </label>
                  <input
                    type="number"
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                    value={issueForm.durationDays}
                    onChange={(e) =>
                      setIssueForm({ ...issueForm, durationDays: Number(e.target.value) })
                    }
                    min={1}
                    max={3650}
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700">
                    {t('vendorLicensing.maxActivationsLabel')}
                  </label>
                  <input
                    type="number"
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
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
                  <label className="block text-sm font-medium text-gray-700">
                    {t('vendorLicensing.editionLabel')}
                  </label>
                  <select
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
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
                  <label className="block text-sm font-medium text-gray-700">
                    {t('vendorLicensing.deploymentModelLabel')}
                  </label>
                  <select
                    className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
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
                <label className="block text-sm font-medium text-gray-700">
                  {t('vendorLicensing.gracePeriodDaysLabel')}
                </label>
                <input
                  type="number"
                  className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-indigo-500 focus:ring-indigo-500 sm:text-sm"
                  value={issueForm.gracePeriodDays}
                  onChange={(e) =>
                    setIssueForm({ ...issueForm, gracePeriodDays: Number(e.target.value) })
                  }
                  min={0}
                />
              </div>

              {issueMutation.isError && (
                <p className="text-sm text-red-600">{t('vendorLicensing.issueFailed')}</p>
              )}
              {issueMutation.isSuccess && (
                <p className="text-sm text-green-600">{t('vendorLicensing.issueSuccess')}</p>
              )}

              <Button type="submit" disabled={issueMutation.isPending}>
                <Plus className="h-4 w-4 mr-1" />
                {t('vendorLicensing.issueButton')}
              </Button>
            </form>
          </CardBody>
        </Card>
      )}
    </div>
  );
}
