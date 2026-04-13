/**
 * Página do Developer Portal — catálogo de APIs, subscriptions, playground e analytics.
 *
 * Organizada em tabs seguindo o padrão de ServiceCatalogPage e ContractCatalogPage.
 * Todo texto visível usa i18n via t('developerPortal.*').
 * Mutations invalidam as queries relacionadas para manter a UI consistente.
 *
 * Tabs extraídos em sub-componentes:
 *  - DevPortalSubscriptionsTab
 *  - DevPortalPlaygroundTab
 *  - DevPortalInboxTab / DevPortalMyConsumptionTab
 */
import { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import {
  Search,
  Bell,
  Play,
  BarChart3,
  RefreshCw,
  Package,
  Inbox,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { PageHeader } from '../../../components/PageHeader';
import { PageContainer } from '../../../components/shell';
import { developerPortalApi } from '../api';
import type {
  CatalogItem,
  PlaygroundResult,
  SubscriptionLevel,
  NotificationChannel,
} from '../../../types';
import { DevPortalSubscriptionsTab } from './DevPortalSubscriptionsTab';
import { DevPortalPlaygroundTab } from './DevPortalPlaygroundTab';
import { DevPortalMyConsumptionTab, DevPortalInboxTab } from './DevPortalInboxTab';

type Tab = 'catalog' | 'subscriptions' | 'playground' | 'analytics' | 'myConsumption' | 'inbox';

/** Formulário de criação de subscription. */
export interface SubscriptionForm {
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
export interface PlaygroundForm {
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

  /**
   * Query para APIs que o utilizador consome — alimenta a aba "My Consumption".
   * Usa endpoint getConsuming que retorna APIs filtradas pelo consumidor autenticado.
   */
  const consumingQuery = useQuery({
    queryKey: ['developerPortal', 'consuming'],
    queryFn: () => developerPortalApi.getConsuming(1, 50),
    enabled: activeTab === 'myConsumption',
    staleTime: 15_000,
  });

  /**
   * Query para "My APIs" — APIs de propriedade do utilizador.
   * Reutilizada tanto no catálogo "My APIs" como na aba de consumo cruzado.
   */
  const myApisQuery = useQuery({
    queryKey: ['developerPortal', 'myApis'],
    queryFn: () => developerPortalApi.getMyApis(1, 50),
    enabled: activeTab === 'myConsumption',
    staleTime: 15_000,
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
    { key: 'myConsumption', label: t('developerPortal.tabs.myConsumption'), icon: <Package size={16} /> },
    { key: 'inbox', label: t('developerPortal.tabs.inbox'), icon: <Inbox size={16} /> },
    { key: 'analytics', label: t('developerPortal.tabs.analytics'), icon: <BarChart3 size={16} /> },
  ];

  const fieldClass =
    'w-full px-3 py-2 bg-surface border border-edge rounded-md text-sm text-body focus:outline-none focus:ring-2 focus:ring-accent/40';

  return (
    <PageContainer>
      <PageHeader
        title={t('developerPortal.title')}
        subtitle={t('developerPortal.description')}
      />

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
        <DevPortalSubscriptionsTab
          showSubForm={showSubForm}
          onToggleForm={() => setShowSubForm(!showSubForm)}
          subForm={subForm}
          onSubFormChange={setSubForm}
          onSubscribe={handleSubscribe}
          isSubscribing={createSubMutation.isPending}
          subscriptions={subscriptionsQuery.data}
          isLoading={subscriptionsQuery.isLoading}
          onDeleteSubscription={(id) => deleteSubMutation.mutate(id)}
          fieldClass={fieldClass}
        />
      )}

      {/* ── Tab: Playground ──────────────────────────────────────────────────── */}
      {activeTab === 'playground' && (
        <DevPortalPlaygroundTab
          playForm={playForm}
          onPlayFormChange={setPlayForm}
          onExecute={handleExecute}
          isExecuting={executeMutation.isPending}
          playResult={playResult}
          historyItems={historyQuery.data?.items}
          historyLoading={historyQuery.isLoading}
          fieldClass={fieldClass}
        />
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

              {(analyticsQuery.data.topSearches ?? []).length > 0 && (
                <Card>
                  <CardHeader>
                    <h3 className="font-semibold text-heading">
                      {t('developerPortal.analytics.topSearches')}
                    </h3>
                  </CardHeader>
                  <CardBody>
                    <table className="w-full text-sm">
                      <thead className="sticky top-0 z-10 bg-panel">
                        <tr className="border-b border-edge text-left text-muted">
                          <th className="py-2 px-3">{t('developerPortal.analytics.query')}</th>
                          <th className="py-2 px-3">{t('developerPortal.analytics.count')}</th>
                        </tr>
                      </thead>
                      <tbody>
                        {(analyticsQuery.data.topSearches ?? []).map((s) => (
                          <tr key={s.term} className="border-b border-edge/50">
                            <td className="py-2 px-3 text-body">{s.term}</td>
                            <td className="py-2 px-3 text-muted">{s.count}</td>
                          </tr>
                        ))}
                      </tbody>
                    </table>
                  </CardBody>
                </Card>
              )}

              {(analyticsQuery.data.topSearches ?? []).length === 0 && (
                <p className="text-muted text-sm">{t('developerPortal.analytics.noData')}</p>
              )}
            </>
          )}
        </div>
      )}

      {/* ── Tab: My Consumption ────────────────────────────────────────────── */}
      {activeTab === 'myConsumption' && (
        <DevPortalMyConsumptionTab
          consumingItems={consumingQuery.data?.items}
          consumingLoading={consumingQuery.isLoading}
          consumingError={consumingQuery.isError}
          myApisItems={myApisQuery.data?.items}
          myApisLoading={myApisQuery.isLoading}
          onRefreshConsuming={() =>
            queryClient.invalidateQueries({ queryKey: ['developerPortal', 'consuming'] })
          }
        />
      )}

      {/* ── Tab: Inbox / Change Awareness ──────────────────────────────────── */}
      {activeTab === 'inbox' && (
        <DevPortalInboxTab
          subscriptions={subscriptionsQuery.data}
          subscriptionsLoading={subscriptionsQuery.isLoading}
        />
      )}
    </PageContainer>
  );
}
