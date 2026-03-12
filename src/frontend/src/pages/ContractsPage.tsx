import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, Lock, RefreshCw } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../components/Card';
import { Button } from '../components/Button';
import { Badge } from '../components/Badge';
import { contractsApi } from '../api';

export function ContractsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [apiAssetId, setApiAssetId] = useState('');
  const [showImportForm, setShowImportForm] = useState(false);
  const [importForm, setImportForm] = useState({ apiAssetId: '', content: '', version: '' });

  const { data: history, isLoading } = useQuery({
    queryKey: ['contracts', 'history', apiAssetId],
    queryFn: () => contractsApi.getHistory(apiAssetId),
    enabled: !!apiAssetId,
  });

  const importMutation = useMutation({
    mutationFn: contractsApi.importContract,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['contracts'] });
      setShowImportForm(false);
      setImportForm({ apiAssetId: '', content: '', version: '' });
    },
  });

  const lockMutation = useMutation({
    mutationFn: ({ id, reason }: { id: string; reason: string }) =>
      contractsApi.lockVersion(id, reason),
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['contracts'] }),
  });

  return (
    <div className="p-8">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">{t('contracts.title')}</h1>
          <p className="text-gray-500 mt-1">{t('contracts.subtitle')}</p>
        </div>
        <Button onClick={() => setShowImportForm((v) => !v)}>
          <Plus size={16} /> {t('contracts.importContract')}
        </Button>
      </div>

      {/* Import Form */}
      {showImportForm && (
        <Card className="mb-6">
          <CardHeader><h2 className="font-semibold text-gray-800">{t('contracts.importTitle')}</h2></CardHeader>
          <CardBody>
            <form
              onSubmit={(e) => { e.preventDefault(); importMutation.mutate(importForm); }}
              className="space-y-4"
            >
              <div className="grid grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">{t('contracts.apiAssetId')}</label>
                  <input
                    type="text"
                    value={importForm.apiAssetId}
                    onChange={(e) => setImportForm((f) => ({ ...f, apiAssetId: e.target.value }))}
                    required
                    className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    placeholder="UUID"
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-gray-700 mb-1">{t('contracts.version')}</label>
                  <input
                    type="text"
                    value={importForm.version}
                    onChange={(e) => setImportForm((f) => ({ ...f, version: e.target.value }))}
                    required
                    className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                    placeholder="1.0.0"
                  />
                </div>
              </div>
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">{t('contracts.openApiContent')}</label>
                <textarea
                  value={importForm.content}
                  onChange={(e) => setImportForm((f) => ({ ...f, content: e.target.value }))}
                  required
                  rows={6}
                  className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-indigo-500"
                  placeholder={t('contracts.openApiPlaceholder')}
                />
              </div>
              <div className="flex gap-2 justify-end">
                <Button variant="secondary" type="button" onClick={() => setShowImportForm(false)}>{t('common.cancel')}</Button>
                <Button type="submit" loading={importMutation.isPending}>{t('contracts.import')}</Button>
              </div>
            </form>
          </CardBody>
        </Card>
      )}

      {/* History Filter */}
      <Card className="mb-6">
        <CardBody>
          <div className="flex gap-3 items-center">
            <label className="text-sm font-medium text-gray-700 whitespace-nowrap">{t('contracts.apiAssetIdLabel')}</label>
            <input
              type="text"
              value={apiAssetId}
              onChange={(e) => setApiAssetId(e.target.value)}
              placeholder={t('contracts.filterPlaceholder')}
              className="flex-1 text-sm border border-gray-300 rounded-md px-3 py-1.5 focus:outline-none focus:ring-2 focus:ring-indigo-500"
            />
          </div>
        </CardBody>
      </Card>

      {/* Contract History */}
      <Card>
        <CardHeader>
          <h2 className="text-base font-semibold text-gray-800">{t('contracts.contractVersions')}</h2>
        </CardHeader>
        <div className="overflow-x-auto">
          {!apiAssetId ? (
            <p className="px-6 py-12 text-sm text-gray-400 text-center">
              {t('contracts.enterApiAssetId')}
            </p>
          ) : isLoading ? (
            <div className="flex items-center justify-center py-12">
              <RefreshCw size={20} className="animate-spin text-gray-400" />
            </div>
          ) : !history?.length ? (
            <p className="px-6 py-12 text-sm text-gray-400 text-center">
              {t('contracts.noContracts')}
            </p>
          ) : (
            <table className="min-w-full text-sm">
              <thead>
                <tr className="border-b border-gray-200 bg-gray-50 text-left">
                  <th className="px-6 py-3 font-medium text-gray-500">{t('contracts.version')}</th>
                  <th className="px-6 py-3 font-medium text-gray-500">{t('contracts.status')}</th>
                  <th className="px-6 py-3 font-medium text-gray-500">{t('contracts.created')}</th>
                  <th className="px-6 py-3 font-medium text-gray-500">{t('common.actions')}</th>
                </tr>
              </thead>
              <tbody className="divide-y divide-gray-100">
                {history.map((cv) => (
                  <tr key={cv.id} className="hover:bg-gray-50">
                    <td className="px-6 py-3 font-mono font-medium text-gray-800">{cv.version}</td>
                    <td className="px-6 py-3">
                      <Badge variant={cv.isLocked ? 'danger' : 'success'}>
                        {cv.isLocked ? t('contracts.locked') : t('contracts.active')}
                      </Badge>
                    </td>
                    <td className="px-6 py-3 text-xs text-gray-500">
                      {new Date(cv.createdAt).toLocaleString()}
                    </td>
                    <td className="px-6 py-3">
                      {!cv.isLocked && (
                        <button
                          onClick={() => lockMutation.mutate({ id: cv.id, reason: 'Locked via UI' })}
                          className="inline-flex items-center gap-1 text-xs text-gray-500 hover:text-red-600 transition-colors"
                        >
                          <Lock size={12} /> {t('contracts.lock')}
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </Card>
    </div>
  );
}
