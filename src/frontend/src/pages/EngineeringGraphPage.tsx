import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { Plus, RefreshCw, Server, Globe } from 'lucide-react';
import { useTranslation } from 'react-i18next';
import { Card, CardHeader, CardBody } from '../components/Card';
import { Button } from '../components/Button';
import { Badge } from '../components/Badge';
import { engineeringGraphApi } from '../api';

const trustColors: Record<string, 'default' | 'success' | 'warning' | 'danger' | 'info'> = {
  Inferred: 'default',
  Low: 'warning',
  Medium: 'info',
  High: 'success',
  Confirmed: 'success',
};

type Tab = 'graph' | 'services' | 'apis';

export function EngineeringGraphPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [tab, setTab] = useState<Tab>('services');
  const [showServiceForm, setShowServiceForm] = useState(false);
  const [showApiForm, setShowApiForm] = useState(false);

  const [serviceForm, setServiceForm] = useState({ name: '', team: '', description: '' });
  const [apiForm, setApiForm] = useState({ name: '', baseUrl: '', ownerServiceId: '', description: '' });

  const { data: graph, isLoading } = useQuery({
    queryKey: ['graph'],
    queryFn: () => engineeringGraphApi.getGraph(),
    staleTime: 30_000,
  });

  const registerService = useMutation({
    mutationFn: engineeringGraphApi.registerService,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['graph'] });
      setShowServiceForm(false);
      setServiceForm({ name: '', team: '', description: '' });
    },
  });

  const registerApi = useMutation({
    mutationFn: engineeringGraphApi.registerApi,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['graph'] });
      setShowApiForm(false);
      setApiForm({ name: '', baseUrl: '', ownerServiceId: '', description: '' });
    },
  });

  const tabs: { key: Tab; label: string }[] = [
    { key: 'services', label: t('engineeringGraph.tabs.services') },
    { key: 'apis', label: t('engineeringGraph.tabs.apis') },
    { key: 'graph', label: t('engineeringGraph.tabs.dependencies') },
  ];

  return (
    <div className="p-8">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">{t('engineeringGraph.title')}</h1>
          <p className="text-gray-500 mt-1">{t('engineeringGraph.subtitle')}</p>
        </div>
        <div className="flex gap-2">
          <Button variant="secondary" onClick={() => setShowServiceForm((v) => !v)}>
            <Plus size={16} /> {t('engineeringGraph.registerService')}
          </Button>
          <Button onClick={() => setShowApiForm((v) => !v)}>
            <Plus size={16} /> {t('engineeringGraph.registerApi')}
          </Button>
        </div>
      </div>

      {/* Register Service Form */}
      {showServiceForm && (
        <Card className="mb-6">
          <CardHeader><h2 className="font-semibold text-gray-800">{t('engineeringGraph.registerServiceTitle')}</h2></CardHeader>
          <CardBody>
            <form
              onSubmit={(e) => { e.preventDefault(); registerService.mutate(serviceForm); }}
              className="grid grid-cols-3 gap-4"
            >
              {([{field: 'name', key: 'engineeringGraph.name'}, {field: 'team', key: 'engineeringGraph.team'}, {field: 'description', key: 'engineeringGraph.description'}] as const).map(({field, key}) => (
                <div key={field}>
                  <label className="block text-sm font-medium text-gray-700 mb-1">{t(key)}</label>
                  <input
                    type="text"
                    value={serviceForm[field]}
                    onChange={(e) => setServiceForm((f) => ({ ...f, [field]: e.target.value }))}
                    required={field !== 'description'}
                    className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                  />
                </div>
              ))}
              <div className="col-span-3 flex gap-2 justify-end">
                <Button variant="secondary" type="button" onClick={() => setShowServiceForm(false)}>{t('common.cancel')}</Button>
                <Button type="submit" loading={registerService.isPending}>{t('engineeringGraph.register')}</Button>
              </div>
            </form>
          </CardBody>
        </Card>
      )}

      {/* Register API Form */}
      {showApiForm && (
        <Card className="mb-6">
          <CardHeader><h2 className="font-semibold text-gray-800">{t('engineeringGraph.registerApiTitle')}</h2></CardHeader>
          <CardBody>
            <form
              onSubmit={(e) => { e.preventDefault(); registerApi.mutate(apiForm); }}
              className="grid grid-cols-2 gap-4"
            >
              {([
                { field: 'name', label: t('engineeringGraph.name') },
                { field: 'baseUrl', label: t('engineeringGraph.baseUrl') },
                { field: 'ownerServiceId', label: t('engineeringGraph.ownerServiceId') },
                { field: 'description', label: t('engineeringGraph.description') },
              ] as const).map(({ field, label }) => (
                <div key={field}>
                  <label className="block text-sm font-medium text-gray-700 mb-1">{label}</label>
                  <input
                    type="text"
                    value={apiForm[field]}
                    onChange={(e) => setApiForm((f) => ({ ...f, [field]: e.target.value }))}
                    required={field !== 'description'}
                    className="w-full rounded-md border border-gray-300 px-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-indigo-500"
                  />
                </div>
              ))}
              <div className="col-span-2 flex gap-2 justify-end">
                <Button variant="secondary" type="button" onClick={() => setShowApiForm(false)}>{t('common.cancel')}</Button>
                <Button type="submit" loading={registerApi.isPending}>{t('engineeringGraph.register')}</Button>
              </div>
            </form>
          </CardBody>
        </Card>
      )}

      {/* Tabs */}
      <div className="flex gap-1 mb-4 bg-gray-100 rounded-lg p-1 w-fit">
        {tabs.map((t) => (
          <button
            key={t.key}
            onClick={() => setTab(t.key)}
            className={`px-4 py-1.5 rounded-md text-sm font-medium transition-colors ${
              tab === t.key ? 'bg-white shadow text-gray-900' : 'text-gray-500 hover:text-gray-700'
            }`}
          >
            {t.label}
          </button>
        ))}
      </div>

      {isLoading ? (
        <div className="flex items-center justify-center py-12">
          <RefreshCw size={20} className="animate-spin text-gray-400" />
        </div>
      ) : (
        <>
          {tab === 'services' && (
            <Card>
              <CardBody className="p-0">
                {!graph?.services?.length ? (
                  <p className="px-6 py-12 text-sm text-gray-400 text-center">{t('engineeringGraph.noServices')}</p>
                ) : (
                  <ul className="divide-y divide-gray-100">
                    {graph.services.map((svc) => (
                      <li key={svc.id} className="px-6 py-4 flex items-center gap-4">
                        <div className="w-10 h-10 rounded-lg bg-indigo-100 flex items-center justify-center text-indigo-700">
                          <Server size={18} />
                        </div>
                        <div className="flex-1 min-w-0">
                          <p className="font-medium text-gray-800">{svc.name}</p>
                          <p className="text-xs text-gray-400">{svc.team}</p>
                        </div>
                        <p className="text-xs text-gray-400 font-mono">{svc.id.slice(0, 8)}…</p>
                      </li>
                    ))}
                  </ul>
                )}
              </CardBody>
            </Card>
          )}

          {tab === 'apis' && (
            <Card>
              <CardBody className="p-0">
                {!graph?.apis?.length ? (
                  <p className="px-6 py-12 text-sm text-gray-400 text-center">{t('engineeringGraph.noApis')}</p>
                ) : (
                  <ul className="divide-y divide-gray-100">
                    {graph.apis.map((api) => (
                      <li key={api.id} className="px-6 py-4 flex items-center gap-4">
                        <div className="w-10 h-10 rounded-lg bg-blue-100 flex items-center justify-center text-blue-700">
                          <Globe size={18} />
                        </div>
                        <div className="flex-1 min-w-0">
                          <p className="font-medium text-gray-800">{api.name}</p>
                          <p className="text-xs text-gray-400">{api.baseUrl}</p>
                        </div>
                        <p className="text-xs text-gray-400 font-mono">{api.id.slice(0, 8)}…</p>
                      </li>
                    ))}
                  </ul>
                )}
              </CardBody>
            </Card>
          )}

          {tab === 'graph' && (
            <Card>
              <CardBody className="p-0">
                {!graph?.relationships?.length ? (
                  <p className="px-6 py-12 text-sm text-gray-400 text-center">{t('engineeringGraph.noDependencies')}</p>
                ) : (
                  <table className="min-w-full text-sm">
                    <thead>
                      <tr className="border-b border-gray-200 bg-gray-50 text-left">
                        <th className="px-6 py-3 font-medium text-gray-500">{t('engineeringGraph.apiAsset')}</th>
                        <th className="px-6 py-3 font-medium text-gray-500">{t('engineeringGraph.consumerService')}</th>
                        <th className="px-6 py-3 font-medium text-gray-500">{t('engineeringGraph.trustLevel')}</th>
                      </tr>
                    </thead>
                    <tbody className="divide-y divide-gray-100">
                      {graph.relationships.map((r, i) => (
                        <tr key={i} className="hover:bg-gray-50">
                          <td className="px-6 py-3 font-mono text-xs text-gray-600">{r.apiAssetId.slice(0, 8)}…</td>
                          <td className="px-6 py-3 font-mono text-xs text-gray-600">{r.consumerServiceId.slice(0, 8)}…</td>
                          <td className="px-6 py-3">
                            <Badge variant={trustColors[r.trustLevel] ?? 'default'}>{r.trustLevel}</Badge>
                          </td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                )}
              </CardBody>
            </Card>
          )}
        </>
      )}
    </div>
  );
}
