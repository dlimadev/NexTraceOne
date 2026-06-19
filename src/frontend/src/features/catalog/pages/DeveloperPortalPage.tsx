/**
 * Página do Developer Portal — catálogo de APIs, subscriptions, playground e analytics.
 *
 * Organizada em tabs seguindo o padrão Betterstack: PageHeader + DS Tabs + TabPanel.
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
  Bell,
  Play,
  BarChart3,
  RefreshCw,
  Package,
  Inbox,
  Search as SearchIcon,
} from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { PageHeader } from '../../../components/PageHeader';
import { PageContainer } from '../../../components/shell';
import { Tabs, TabPanel } from '../../../components/Tabs';
import { SearchInput } from '../../../components/SearchInput';
import { StatCard } from '../../../components/StatCard';
import { EmptyState } from '../../../components/EmptyState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
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
import { useEnvironment } from '../../../contexts/EnvironmentContext';

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

/** ID do grupo de tabs para aria-controls. */
const TABS_ID = 'developer-portal-tabs';

export function DeveloperPortalPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const { activeEnvironmentId } = useEnvironment();
  const [activeTab, setActiveTab] = useState<Tab>('catalog');
  const [searchQuery, setSearchQuery] = useState('');
  const [showSubForm, setShowSubForm] = useState(false);
  const [subForm, setSubForm] = useState<SubscriptionForm>(emptySubForm);
  const [playForm, setPlayForm] = useState<PlaygroundForm>(emptyPlayForm);
  const [playResult, setPlayResult] = useState<PlaygroundResult | null>(null);

  // ── Queries ─────────────────────────────────────────────────────────────────

  const catalogQuery = useQuery({
    queryKey: ['developerPortal', 'catalog', searchQuery, activeEnvironmentId],
    queryFn: () => developerPortalApi.searchCatalog(searchQuery, 1, 20),
    enabled: activeTab === 'catalog' && searchQuery.length > 0,
    staleTime: 15_000,
  });

  const subscriptionsQuery = useQuery({
    queryKey: ['developerPortal', 'subscriptions', activeEnvironmentId],
    queryFn: () => developerPortalApi.listSubscriptions(),
    enabled: activeTab === 'subscriptions',
    staleTime: 15_000,
  });

  const historyQuery = useQuery({
    queryKey: ['developerPortal', 'playground', 'history', activeEnvironmentId],
    queryFn: () => developerPortalApi.getPlaygroundHistory(1, 20),
    enabled: activeTab === 'playground',
    staleTime: 15_000,
  });

  const analyticsQuery = useQuery({
    queryKey: ['developerPortal', 'analytics', activeEnvironmentId],
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
    queryKey: ['developerPortal', 'consuming', activeEnvironmentId],
    queryFn: () => developerPortalApi.getConsuming(1, 50),
    enabled: activeTab === 'myConsumption',
    staleTime: 15_000,
  });

  /**
   * Query para "My APIs" — APIs de propriedade do utilizador.
   * Reutilizada tanto no catálogo "My APIs" como na aba de consumo cruzado.
   */
  const myApisQuery = useQuery({
    queryKey: ['developerPortal', 'myApis', activeEnvironmentId],
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

  // ── Definição de tabs ────────────────────────────────────────────────────────

  const tabItems = [
    { id: 'catalog', label: t('developerPortal.tabs.catalog'), icon: <SearchIcon size={14} /> },
    { id: 'subscriptions', label: t('developerPortal.tabs.subscriptions'), icon: <Bell size={14} /> },
    { id: 'playground', label: t('developerPortal.tabs.playground'), icon: <Play size={14} /> },
    { id: 'myConsumption', label: t('developerPortal.tabs.myConsumption'), icon: <Package size={14} /> },
    { id: 'inbox', label: t('developerPortal.tabs.inbox'), icon: <Inbox size={14} /> },
    { id: 'analytics', label: t('developerPortal.tabs.analytics'), icon: <BarChart3 size={14} /> },
  ];

  return (
    <PageContainer>
      {/* Cabeçalho da página — sem CTA global (ações ficam dentro das abas) */}
      <PageHeader
        title={t('developerPortal.title')}
        subtitle={t('developerPortal.description')}
      />

      {/* Navegação por abas — DS Tabs com role=tablist + aria-controls */}
      <Tabs
        id={TABS_ID}
        items={tabItems}
        activeId={activeTab}
        onChange={(id) => setActiveTab(id as Tab)}
        className="mb-6"
      />

      {/* ── Tab: Catalog ────────────────────────────────────────────────────── */}
      <TabPanel tabId="catalog" tabsId={TABS_ID} active={activeTab === 'catalog'}>
        <div className="space-y-4">
          {/* Campo de busca DS */}
          <SearchInput
            value={searchQuery}
            onChange={(e) => setSearchQuery(e.target.value)}
            placeholder={t('developerPortal.catalog.searchPlaceholder')}
            aria-label={t('developerPortal.catalog.searchPlaceholder')}
          />

          {catalogQuery.isLoading && <PageLoadingState size="sm" />}

          {catalogQuery.isError && (
            <PageErrorState
              variant="compact"
              onRetry={() =>
                queryClient.invalidateQueries({ queryKey: ['developerPortal', 'catalog'] })
              }
            />
          )}

          {catalogQuery.data && catalogQuery.data.items.length === 0 && (
            <EmptyState
              title={t('developerPortal.catalog.noResults')}
              size="compact"
            />
          )}

          {catalogQuery.data && catalogQuery.data.items.length > 0 && (
            <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
              {catalogQuery.data.items.map((item: CatalogItem) => (
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
          )}
        </div>
      </TabPanel>

      {/* ── Tab: Subscriptions ──────────────────────────────────────────────── */}
      <TabPanel tabId="subscriptions" tabsId={TABS_ID} active={activeTab === 'subscriptions'}>
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
        />
      </TabPanel>

      {/* ── Tab: Playground ─────────────────────────────────────────────────── */}
      <TabPanel tabId="playground" tabsId={TABS_ID} active={activeTab === 'playground'}>
        <DevPortalPlaygroundTab
          playForm={playForm}
          onPlayFormChange={setPlayForm}
          onExecute={handleExecute}
          isExecuting={executeMutation.isPending}
          playResult={playResult}
          historyItems={historyQuery.data?.items}
          historyLoading={historyQuery.isLoading}
        />
      </TabPanel>

      {/* ── Tab: Analytics ──────────────────────────────────────────────────── */}
      <TabPanel tabId="analytics" tabsId={TABS_ID} active={activeTab === 'analytics'}>
        <div className="space-y-6">
          {/* Cabeçalho de secção com refresh */}
          <div className="flex justify-between items-center">
            <h2 className="text-base font-semibold text-heading">
              {t('developerPortal.analytics.title')}
            </h2>
            <Button
              variant="outline"
              size="sm"
              icon={<RefreshCw size={14} />}
              onClick={() =>
                queryClient.invalidateQueries({ queryKey: ['developerPortal', 'analytics'] })
              }
            >
              {t('common.refresh')}
            </Button>
          </div>

          {analyticsQuery.isLoading && <PageLoadingState size="sm" />}

          {analyticsQuery.isError && (
            <PageErrorState
              variant="compact"
              onRetry={() =>
                queryClient.invalidateQueries({ queryKey: ['developerPortal', 'analytics'] })
              }
            />
          )}

          {analyticsQuery.data && (
            <>
              {/* KPIs via StatCard */}
              <div className="grid grid-cols-2 md:grid-cols-4 gap-4">
                <StatCard
                  title={t('developerPortal.analytics.totalSearches')}
                  value={analyticsQuery.data.totalSearches ?? 0}
                  color="text-accent"
                />
                <StatCard
                  title={t('developerPortal.analytics.totalApiViews')}
                  value={analyticsQuery.data.totalApiViews ?? 0}
                  color="text-info"
                />
                <StatCard
                  title={t('developerPortal.analytics.totalPlaygroundExecutions')}
                  value={analyticsQuery.data.totalPlaygroundExecutions ?? 0}
                  color="text-success"
                />
                <StatCard
                  title={t('developerPortal.analytics.totalCodeGenerations')}
                  value={analyticsQuery.data.totalCodeGenerations ?? 0}
                  color="text-warning"
                />
              </div>

              {/* Tabela de top searches */}
              {(analyticsQuery.data.topSearches ?? []).length > 0 ? (
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
              ) : (
                <EmptyState
                  title={t('developerPortal.analytics.noData')}
                  size="compact"
                />
              )}
            </>
          )}
        </div>
      </TabPanel>

      {/* ── Tab: My Consumption ─────────────────────────────────────────────── */}
      <TabPanel tabId="myConsumption" tabsId={TABS_ID} active={activeTab === 'myConsumption'}>
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
      </TabPanel>

      {/* ── Tab: Inbox / Change Awareness ───────────────────────────────────── */}
      <TabPanel tabId="inbox" tabsId={TABS_ID} active={activeTab === 'inbox'}>
        <DevPortalInboxTab
          subscriptions={subscriptionsQuery.data}
          subscriptionsLoading={subscriptionsQuery.isLoading}
        />
      </TabPanel>
    </PageContainer>
  );
}
