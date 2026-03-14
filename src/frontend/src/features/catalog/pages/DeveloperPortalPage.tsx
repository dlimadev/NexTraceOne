/**
 * Página do Developer Portal — catálogo de APIs, subscriptions, playground e analytics.
 *
 * Organizada em tabs seguindo o padrão de EngineeringGraphPage e LicensingPage.
 * Todo texto visível usa i18n via t('developerPortal.*').
 * Mutations invalidam as queries relacionadas para manter a UI consistente.
 */
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  BookOpen,
  Search,
  Bell,
  Play,
  BarChart3,
  Trash2,
  Plus,
  RefreshCw,
  Copy,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { developerPortalApi } from '../api';
import type {
  CatalogItem,
  Subscription,
  PlaygroundResult,
  SubscriptionLevel,
  NotificationChannel,
} from '../../../types';

type Tab = 'catalog' | 'subscriptions' | 'playground' | 'analytics';

/** Formulário de criação de subscription. */
interface SubscriptionForm {
  apiAssetId: string;
  apiName: string;
  subscriberEmail: string;
  consumerServiceName: string;
  consumerServiceVersion: string;
  level: SubscriptionLevel;
  channel: NotificationChannel;
  webhookUrl: string;
}

/** Formulário de execução do playground. */
interface PlaygroundForm {
  apiAssetId: string;
  apiName: string;
  httpMethod: string;
  requestPath: string;
  requestBody: string;
  requestHeaders: string;
  environment: string;
}

const emptySubForm: SubscriptionForm = {
  apiAssetId: '',
  apiName: '',
  subscriberEmail: '',
  consumerServiceName: '',
  consumerServiceVersion: '',
  level: 'BreakingChangesOnly',
  channel: 'Email',
  webhookUrl: '',
};

const emptyPlayForm: PlaygroundForm = {
  apiAssetId: '',
  apiName: '',
  httpMethod: 'GET',
  requestPath: '/',
  requestBody: '',
  requestHeaders: '',
  environment: '',
};

const SUBSCRIPTION_LEVELS: SubscriptionLevel[] = [
  'BreakingChangesOnly',
  'AllChanges',
  'DeprecationNotices',
  'SecurityAdvisories',
];

const NOTIFICATION_CHANNELS: NotificationChannel[] = ['Email', 'Webhook'];
const HTTP_METHODS = ['GET', 'POST', 'PUT', 'PATCH', 'DELETE'];

export function DeveloperPortalPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<Tab>('catalog');
  const [searchQuery, setSearchQuery] = useState('');
  const [showSubForm, setShowSubForm] = useState(false);
  const [subForm, setSubForm] = useState<SubscriptionForm>(emptySubForm);
  const [playForm, setPlayForm] = useState<PlaygroundForm>(emptyPlayForm);
  const [playResult, setPlayResult] = useState<PlaygroundResult | null>(null);

  // ── Queries ─────────────────────────────────────────────────────────────────

  const catalogQuery = useQuery({
    queryKey: ['developerPortal', 'catalog', searchQuery],
    queryFn: () => developerPortalApi.searchCatalog(searchQuery, 1, 20),
    enabled: activeTab === 'catalog' && searchQuery.length > 0,
    staleTime: 15_000,
  });

  const subscriptionsQuery = useQuery({
    queryKey: ['developerPortal', 'subscriptions'],
    queryFn: () => developerPortalApi.listSubscriptions(),
    enabled: activeTab === 'subscriptions',
    staleTime: 15_000,
  });

  const historyQuery = useQuery({
    queryKey: ['developerPortal', 'playground', 'history'],
    queryFn: () => developerPortalApi.getPlaygroundHistory(1, 20),
    enabled: activeTab === 'playground',
    staleTime: 15_000,
  });

  const analyticsQuery = useQuery({
    queryKey: ['developerPortal', 'analytics'],
    queryFn: () => {
      const since = new Date(Date.now() - 30 * 24 * 60 * 60 * 1000).toISOString();
      return developerPortalApi.getAnalytics(since);
    },
    enabled: activeTab === 'analytics',
    staleTime: 30_000,
  });

  // ── Mutations ───────────────────────────────────────────────────────────────

  const createSubMutation = useMutation({
    mutationFn: developerPortalApi.createSubscription,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['developerPortal', 'subscriptions'] });
      setShowSubForm(false);
      setSubForm(emptySubForm);
    },
  });

  const deleteSubMutation = useMutation({
    mutationFn: developerPortalApi.deleteSubscription,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['developerPortal', 'subscriptions'] });
    },
  });

  const executeMutation = useMutation({
    mutationFn: developerPortalApi.executePlayground,
    onSuccess: (data) => {
      setPlayResult(data);
      queryClient.invalidateQueries({ queryKey: ['developerPortal', 'playground', 'history'] });
    },
  });

  // ── Handlers ────────────────────────────────────────────────────────────────

  const handleSubscribe = () => {
    const payload: Parameters<typeof developerPortalApi.createSubscription>[0] = {
      ...subForm,
      webhookUrl: subForm.webhookUrl || undefined,
    };
    createSubMutation.mutate(payload);
  };

  const handleExecute = () => {
    const payload: Parameters<typeof developerPortalApi.executePlayground>[0] = {
      ...playForm,
      requestBody: playForm.requestBody || undefined,
      requestHeaders: playForm.requestHeaders || undefined,
      environment: playForm.environment || undefined,
    };
    executeMutation.mutate(payload);
  };

  // ── Tab rendering ───────────────────────────────────────────────────────────

  const tabs: { key: Tab; label: string; icon: React.ReactNode }[] = [
    { key: 'catalog', label: t('developerPortal.tabs.catalog'), icon: <Search size={16} /> },
    { key: 'subscriptions', label: t('developerPortal.tabs.subscriptions'), icon: <Bell size={16} /> },
    { key: 'playground', label: t('developerPortal.tabs.playground'), icon: <Play size={16} /> },
    { key: 'analytics', label: t('developerPortal.tabs.analytics'), icon: <BarChart3 size={16} /> },
  ];

  const fieldClass =
    'w-full px-3 py-2 bg-surface border border-edge rounded-md text-sm text-body focus:outline-none focus:ring-2 focus:ring-accent/40';

  return (
    <div className="space-y-6">
      {/* Cabeçalho */}
      <div>
        <h1 className="text-2xl font-bold text-heading flex items-center gap-2">
          <BookOpen size={24} />
          {t('developerPortal.title')}
        </h1>
        <p className="text-muted text-sm mt-1">{t('developerPortal.description')}</p>
      </div>

      {/* Tabs */}
      <div className="flex gap-1 border-b border-edge">
        {tabs.map((tab) => (
          <button
            key={tab.key}
            onClick={() => setActiveTab(tab.key)}
            className={`flex items-center gap-2 px-4 py-2.5 text-sm font-medium border-b-2 transition-colors ${
              activeTab === tab.key
                ? 'border-accent text-accent'
                : 'border-transparent text-muted hover:text-body'
            }`}
          >
            {tab.icon}
            {tab.label}
          </button>
        ))}
      </div>

      {/* ── Tab: Catalog ─────────────────────────────────────────────────────── */}
      {activeTab === 'catalog' && (
        <div className="space-y-4">
          <input
            type="text"
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder={t('developerPortal.catalog.searchPlaceholder')}
            className={fieldClass}
          />
          {catalogQuery.isLoading && (
            <p className="text-muted text-sm">{t('common.loading')}</p>
          )}
          {catalogQuery.isError && (
            <p className="text-critical text-sm">{t('common.error')}</p>
          )}
          {catalogQuery.data && catalogQuery.data.items.length === 0 && (
            <p className="text-muted text-sm">{t('developerPortal.catalog.noResults')}</p>
          )}
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {catalogQuery.data?.items.map((item: CatalogItem) => (
              <Card key={item.apiAssetId}>
                <CardHeader>
                  <div className="flex justify-between items-start">
                    <h3 className="font-semibold text-heading">{item.apiName}</h3>
                    <Badge variant={item.healthStatus === 'Healthy' ? 'success' : 'warning'}>
                      {item.healthStatus}
                    </Badge>
                  </div>
                </CardHeader>
                <CardBody>
                  <p className="text-sm text-muted mb-2">{item.description}</p>
                  <div className="flex justify-between text-xs text-muted">
                    <span>{t('developerPortal.catalog.owner')}: {item.ownerServiceName}</span>
                    <span>{t('developerPortal.catalog.version')}: {item.version}</span>
                  </div>
                </CardBody>
              </Card>
            ))}
          </div>
        </div>
      )}

      {/* ── Tab: Subscriptions ───────────────────────────────────────────────── */}
      {activeTab === 'subscriptions' && (
        <div className="space-y-4">
          <div className="flex justify-between items-center">
            <h2 className="text-lg font-semibold text-heading">
              {t('developerPortal.subscriptions.title')}
            </h2>
            <Button onClick={() => setShowSubForm(!showSubForm)}>
              <Plus size={16} className="mr-1" />
              {t('developerPortal.subscriptions.create')}
            </Button>
          </div>

          {showSubForm && (
            <Card>
              <CardBody>
                <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">
                      {t('developerPortal.subscriptions.form.apiAssetId')}
                    </label>
                    <input
                      className={fieldClass}
                      value={subForm.apiAssetId}
                      onChange={(e) => setSubForm({ ...subForm, apiAssetId: e.target.value })}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">
                      {t('developerPortal.subscriptions.form.apiName')}
                    </label>
                    <input
                      className={fieldClass}
                      value={subForm.apiName}
                      onChange={(e) => setSubForm({ ...subForm, apiName: e.target.value })}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">
                      {t('developerPortal.subscriptions.form.subscriberEmail')}
                    </label>
                    <input
                      type="email"
                      className={fieldClass}
                      value={subForm.subscriberEmail}
                      onChange={(e) => setSubForm({ ...subForm, subscriberEmail: e.target.value })}
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">
                      {t('developerPortal.subscriptions.form.consumerServiceName')}
                    </label>
                    <input
                      className={fieldClass}
                      value={subForm.consumerServiceName}
                      onChange={(e) =>
                        setSubForm({ ...subForm, consumerServiceName: e.target.value })
                      }
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">
                      {t('developerPortal.subscriptions.form.consumerServiceVersion')}
                    </label>
                    <input
                      className={fieldClass}
                      value={subForm.consumerServiceVersion}
                      onChange={(e) =>
                        setSubForm({ ...subForm, consumerServiceVersion: e.target.value })
                      }
                    />
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">
                      {t('developerPortal.subscriptions.form.level')}
                    </label>
                    <select
                      className={fieldClass}
                      value={subForm.level}
                      onChange={(e) =>
                        setSubForm({ ...subForm, level: e.target.value as SubscriptionLevel })
                      }
                    >
                      {SUBSCRIPTION_LEVELS.map((lvl) => (
                        <option key={lvl} value={lvl}>
                          {t(`developerPortal.subscriptions.levels.${lvl}`)}
                        </option>
                      ))}
                    </select>
                  </div>
                  <div>
                    <label className="block text-sm font-medium text-body mb-1">
                      {t('developerPortal.subscriptions.form.channel')}
                    </label>
                    <select
                      className={fieldClass}
                      value={subForm.channel}
                      onChange={(e) =>
                        setSubForm({ ...subForm, channel: e.target.value as NotificationChannel })
                      }
                    >
                      {NOTIFICATION_CHANNELS.map((ch) => (
                        <option key={ch} value={ch}>
                          {t(`developerPortal.subscriptions.channels.${ch}`)}
                        </option>
                      ))}
                    </select>
                  </div>
                  {subForm.channel === 'Webhook' && (
                    <div>
                      <label className="block text-sm font-medium text-body mb-1">
                        {t('developerPortal.subscriptions.form.webhookUrl')}
                      </label>
                      <input
                        className={fieldClass}
                        value={subForm.webhookUrl}
                        onChange={(e) => setSubForm({ ...subForm, webhookUrl: e.target.value })}
                      />
                    </div>
                  )}
                </div>
                <div className="mt-4">
                  <Button onClick={handleSubscribe} disabled={createSubMutation.isPending}>
                    {t('developerPortal.subscriptions.form.submit')}
                  </Button>
                </div>
              </CardBody>
            </Card>
          )}

          {subscriptionsQuery.isLoading && (
            <p className="text-muted text-sm">{t('common.loading')}</p>
          )}
          {subscriptionsQuery.data && subscriptionsQuery.data.length === 0 && (
            <p className="text-muted text-sm">
              {t('developerPortal.subscriptions.noSubscriptions')}
            </p>
          )}
          {subscriptionsQuery.data && subscriptionsQuery.data.length > 0 && (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-edge text-left text-muted">
                    <th className="py-2 px-3">{t('developerPortal.subscriptions.apiName')}</th>
                    <th className="py-2 px-3">{t('developerPortal.subscriptions.level')}</th>
                    <th className="py-2 px-3">{t('developerPortal.subscriptions.channel')}</th>
                    <th className="py-2 px-3">{t('developerPortal.subscriptions.status')}</th>
                    <th className="py-2 px-3">{t('common.actions')}</th>
                  </tr>
                </thead>
                <tbody>
                  {subscriptionsQuery.data.map((sub: Subscription) => (
                    <tr key={sub.id} className="border-b border-edge/50">
                      <td className="py-2 px-3 text-body">{sub.apiName}</td>
                      <td className="py-2 px-3">
                        <Badge variant="info">
                          {t(`developerPortal.subscriptions.levels.${sub.level}`)}
                        </Badge>
                      </td>
                      <td className="py-2 px-3 text-muted">
                        {t(`developerPortal.subscriptions.channels.${sub.channel}`)}
                      </td>
                      <td className="py-2 px-3">
                        <Badge variant={sub.isActive ? 'success' : 'default'}>
                          {sub.isActive
                            ? t('developerPortal.subscriptions.active')
                            : t('developerPortal.subscriptions.inactive')}
                        </Badge>
                      </td>
                      <td className="py-2 px-3">
                        <button
                          onClick={() => deleteSubMutation.mutate(sub.id)}
                          className="text-critical hover:text-critical/80 transition-colors"
                          title={t('developerPortal.subscriptions.unsubscribe')}
                        >
                          <Trash2 size={16} />
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </div>
      )}

      {/* ── Tab: Playground ──────────────────────────────────────────────────── */}
      {activeTab === 'playground' && (
        <div className="space-y-4">
          <Card>
            <CardHeader>
              <h2 className="text-lg font-semibold text-heading">
                {t('developerPortal.playground.title')}
              </h2>
            </CardHeader>
            <CardBody>
              <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
                <div>
                  <label className="block text-sm font-medium text-body mb-1">
                    {t('developerPortal.playground.form.apiAssetId')}
                  </label>
                  <input
                    className={fieldClass}
                    value={playForm.apiAssetId}
                    onChange={(e) => setPlayForm({ ...playForm, apiAssetId: e.target.value })}
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-body mb-1">
                    {t('developerPortal.playground.form.apiName')}
                  </label>
                  <input
                    className={fieldClass}
                    value={playForm.apiName}
                    onChange={(e) => setPlayForm({ ...playForm, apiName: e.target.value })}
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-body mb-1">
                    {t('developerPortal.playground.form.httpMethod')}
                  </label>
                  <select
                    className={fieldClass}
                    value={playForm.httpMethod}
                    onChange={(e) => setPlayForm({ ...playForm, httpMethod: e.target.value })}
                  >
                    {HTTP_METHODS.map((m) => (
                      <option key={m} value={m}>
                        {m}
                      </option>
                    ))}
                  </select>
                </div>
                <div>
                  <label className="block text-sm font-medium text-body mb-1">
                    {t('developerPortal.playground.form.requestPath')}
                  </label>
                  <input
                    className={fieldClass}
                    value={playForm.requestPath}
                    onChange={(e) => setPlayForm({ ...playForm, requestPath: e.target.value })}
                  />
                </div>
                <div className="md:col-span-2">
                  <label className="block text-sm font-medium text-body mb-1">
                    {t('developerPortal.playground.form.requestBody')}
                  </label>
                  <textarea
                    className={`${inputClass} h-24 font-mono`}
                    value={playForm.requestBody}
                    onChange={(e) => setPlayForm({ ...playForm, requestBody: e.target.value })}
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-body mb-1">
                    {t('developerPortal.playground.form.requestHeaders')}
                  </label>
                  <input
                    className={fieldClass}
                    value={playForm.requestHeaders}
                    onChange={(e) => setPlayForm({ ...playForm, requestHeaders: e.target.value })}
                  />
                </div>
                <div>
                  <label className="block text-sm font-medium text-body mb-1">
                    {t('developerPortal.playground.form.environment')}
                  </label>
                  <input
                    className={fieldClass}
                    value={playForm.environment}
                    onChange={(e) => setPlayForm({ ...playForm, environment: e.target.value })}
                  />
                </div>
              </div>
              <div className="mt-4">
                <Button onClick={handleExecute} disabled={executeMutation.isPending}>
                  <Play size={16} className="mr-1" />
                  {t('developerPortal.playground.execute')}
                </Button>
              </div>
            </CardBody>
          </Card>

          {playResult && (
            <Card>
              <CardHeader>
                <div className="flex justify-between items-center">
                  <h3 className="font-semibold text-heading">
                    {t('developerPortal.playground.result.responseBody')}
                  </h3>
                  <Badge
                    variant={playResult.responseStatusCode < 400 ? 'success' : 'danger'}
                  >
                    {t('developerPortal.playground.result.statusCode')}: {playResult.responseStatusCode}
                  </Badge>
                </div>
              </CardHeader>
              <CardBody>
                <pre className="bg-surface p-3 rounded-md text-xs font-mono overflow-x-auto max-h-64">
                  {playResult.responseBody}
                </pre>
                <div className="flex gap-4 mt-2 text-xs text-muted">
                  <span>
                    {t('developerPortal.playground.result.duration')}: {playResult.durationMs}ms
                  </span>
                  <span>
                    {t('developerPortal.playground.result.executedAt')}:{' '}
                    {new Date(playResult.executedAt).toLocaleString()}
                  </span>
                </div>
              </CardBody>
            </Card>
          )}

          {/* Histórico do playground */}
          <Card>
            <CardHeader>
              <h3 className="font-semibold text-heading">
                {t('developerPortal.playground.history')}
              </h3>
            </CardHeader>
            <CardBody>
              {historyQuery.isLoading && (
                <p className="text-muted text-sm">{t('common.loading')}</p>
              )}
              {historyQuery.data && historyQuery.data.items.length === 0 && (
                <p className="text-muted text-sm">
                  {t('developerPortal.playground.noHistory')}
                </p>
              )}
              {historyQuery.data && historyQuery.data.items.length > 0 && (
                <div className="overflow-x-auto">
                  <table className="w-full text-sm">
                    <thead>
                      <tr className="border-b border-edge text-left text-muted">
                        <th className="py-2 px-3">API</th>
                        <th className="py-2 px-3">
                          {t('developerPortal.playground.form.httpMethod')}
                        </th>
                        <th className="py-2 px-3">
                          {t('developerPortal.playground.form.requestPath')}
                        </th>
                        <th className="py-2 px-3">
                          {t('developerPortal.playground.result.statusCode')}
                        </th>
                        <th className="py-2 px-3">
                          {t('developerPortal.playground.result.duration')}
                        </th>
                      </tr>
                    </thead>
                    <tbody>
                      {historyQuery.data.items.map((h) => (
                        <tr key={h.sessionId} className="border-b border-edge/50">
                          <td className="py-2 px-3 text-body">{h.apiName}</td>
                          <td className="py-2 px-3">
                            <Badge variant="info">{h.httpMethod}</Badge>
                          </td>
                          <td className="py-2 px-3 text-muted font-mono text-xs">
                            {h.requestPath}
                          </td>
                          <td className="py-2 px-3">
                            <Badge variant={h.responseStatusCode < 400 ? 'success' : 'danger'}>
                              {h.responseStatusCode}
                            </Badge>
                          </td>
                          <td className="py-2 px-3 text-muted">{h.durationMs}ms</td>
                        </tr>
                      ))}
                    </tbody>
                  </table>
                </div>
              )}
            </CardBody>
          </Card>
        </div>
      )}

      {/* ── Tab: Analytics ───────────────────────────────────────────────────── */}
      {activeTab === 'analytics' && (
        <div className="space-y-4">
          <div className="flex justify-between items-center">
            <h2 className="text-lg font-semibold text-heading">
              {t('developerPortal.analytics.title')}
            </h2>
            <Button
              variant="secondary"
              onClick={() =>
                queryClient.invalidateQueries({ queryKey: ['developerPortal', 'analytics'] })
              }
            >
              <RefreshCw size={16} className="mr-1" />
              {t('common.refresh')}
            </Button>
          </div>

          {analyticsQuery.isLoading && (
            <p className="text-muted text-sm">{t('common.loading')}</p>
          )}
          {analyticsQuery.isError && (
            <p className="text-critical text-sm">{t('common.error')}</p>
          )}

          {analyticsQuery.data && (
            <>
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                {[
                  {
                    label: t('developerPortal.analytics.totalSearches'),
                    value: analyticsQuery.data.totalSearches,
                  },
                  {
                    label: t('developerPortal.analytics.totalApiViews'),
                    value: analyticsQuery.data.totalApiViews,
                  },
                  {
                    label: t('developerPortal.analytics.totalPlaygroundExecutions'),
                    value: analyticsQuery.data.totalPlaygroundExecutions,
                  },
                  {
                    label: t('developerPortal.analytics.totalCodeGenerations'),
                    value: analyticsQuery.data.totalCodeGenerations,
                  },
                ].map((stat) => (
                  <Card key={stat.label}>
                    <CardBody>
                      <p className="text-xs text-muted uppercase tracking-wide">{stat.label}</p>
                      <p className="text-2xl font-bold text-heading mt-1">{stat.value}</p>
                    </CardBody>
                  </Card>
                ))}
              </div>

              {analyticsQuery.data.topSearches.length > 0 && (
                <Card>
                  <CardHeader>
                    <h3 className="font-semibold text-heading">
                      {t('developerPortal.analytics.topSearches')}
                    </h3>
                  </CardHeader>
                  <CardBody>
                    <table className="w-full text-sm">
                      <thead>
                        <tr className="border-b border-edge text-left text-muted">
                          <th className="py-2 px-3">{t('developerPortal.analytics.query')}</th>
                          <th className="py-2 px-3">{t('developerPortal.analytics.count')}</th>
                        </tr>
                      </thead>
                      <tbody>
                        {analyticsQuery.data.topSearches.map((s) => (
                          <tr key={s.query} className="border-b border-edge/50">
                            <td className="py-2 px-3 text-body">{s.query}</td>
                            <td className="py-2 px-3 text-muted">{s.count}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </CardBody>
                </Card>
              )}

              {analyticsQuery.data.topSearches.length === 0 && (
                <p className="text-muted text-sm">{t('developerPortal.analytics.noData')}</p>
              )}
            </>
          )}
        </div>
      )}
    </div>
  );
}
