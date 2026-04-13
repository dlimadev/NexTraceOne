/**
 * Tab de Inbox / Change Awareness e My Consumption do Developer Portal.
 *
 * Extraído de DeveloperPortalPage para reduzir complexidade.
 * Agrupa My Consumption e Inbox em componentes exportados separados mas no mesmo ficheiro.
 */
import { useTranslation } from 'react-i18next';
import { RefreshCw, AlertTriangle, Clock, ExternalLink, Code } from 'lucide-react';
import { Card, CardHeader, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import type { CatalogItem, Subscription } from '../../../types';

// ── My Consumption Tab ──────────────────────────────────────────────────────

export interface DevPortalMyConsumptionTabProps {
  consumingItems: CatalogItem[] | undefined;
  consumingLoading: boolean;
  consumingError: boolean;
  myApisItems: CatalogItem[] | undefined;
  myApisLoading: boolean;
  onRefreshConsuming: () => void;
}

export function DevPortalMyConsumptionTab({
  consumingItems,
  consumingLoading,
  consumingError,
  myApisItems,
  myApisLoading,
  onRefreshConsuming,
}: DevPortalMyConsumptionTabProps) {
  const { t } = useTranslation();

  return (
    <div className="space-y-6">
      {/* APIs que o utilizador consome */}
      <div className="space-y-4">
        <div className="flex justify-between items-center">
          <h2 className="text-lg font-semibold text-heading">
            {t('developerPortal.myConsumption.consuming')}
          </h2>
          <Button variant="secondary" onClick={onRefreshConsuming}>
            <RefreshCw size={16} className="mr-1" />
            {t('common.refresh')}
          </Button>
        </div>

        {consumingLoading && (
          <p className="text-muted text-sm">{t('common.loading')}</p>
        )}
        {consumingError && (
          <p className="text-critical text-sm">{t('common.error')}</p>
        )}
        {consumingItems && consumingItems.length === 0 && (
          <Card>
            <CardBody>
              <p className="text-muted text-sm text-center py-4">
                {t('developerPortal.myConsumption.noConsuming')}
              </p>
            </CardBody>
          </Card>
        )}
        {consumingItems && consumingItems.length > 0 && (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {consumingItems.map((item: CatalogItem) => (
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

      {/* APIs que o utilizador é dono */}
      <div className="space-y-4">
        <h2 className="text-lg font-semibold text-heading">
          {t('developerPortal.myConsumption.myApis')}
        </h2>
        {myApisLoading && (
          <p className="text-muted text-sm">{t('common.loading')}</p>
        )}
        {myApisItems && myApisItems.length === 0 && (
          <Card>
            <CardBody>
              <p className="text-muted text-sm text-center py-4">
                {t('developerPortal.myConsumption.noMyApis')}
              </p>
            </CardBody>
          </Card>
        )}
        {myApisItems && myApisItems.length > 0 && (
          <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4">
            {myApisItems.map((item: CatalogItem) => (
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
    </div>
  );
}

// ── Inbox / Change Awareness Tab ────────────────────────────────────────────

export interface DevPortalInboxTabProps {
  subscriptions: Subscription[] | undefined;
  subscriptionsLoading: boolean;
}

export function DevPortalInboxTab({
  subscriptions,
  subscriptionsLoading,
}: DevPortalInboxTabProps) {
  const { t } = useTranslation();

  return (
    <div className="space-y-4">
      <div className="flex justify-between items-center">
        <h2 className="text-lg font-semibold text-heading">
          {t('developerPortal.inbox.title')}
        </h2>
      </div>
      <p className="text-sm text-muted">
        {t('developerPortal.inbox.description')}
      </p>

      {/* Painel de notificações — alimentado pelas subscriptions ativas */}
      {subscriptionsLoading && (
        <p className="text-muted text-sm">{t('common.loading')}</p>
      )}

      {/* Cards informativos sobre tipos de notificações que o utilizador receberá */}
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <AlertTriangle size={18} className="text-warning" />
              <h3 className="font-semibold text-heading">
                {t('developerPortal.inbox.breakingChanges')}
              </h3>
            </div>
          </CardHeader>
          <CardBody>
            <p className="text-sm text-muted">
              {t('developerPortal.inbox.breakingChangesDescription')}
            </p>
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Clock size={18} className="text-info" />
              <h3 className="font-semibold text-heading">
                {t('developerPortal.inbox.deprecations')}
              </h3>
            </div>
          </CardHeader>
          <CardBody>
            <p className="text-sm text-muted">
              {t('developerPortal.inbox.deprecationsDescription')}
            </p>
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <ExternalLink size={18} className="text-accent" />
              <h3 className="font-semibold text-heading">
                {t('developerPortal.inbox.migrations')}
              </h3>
            </div>
          </CardHeader>
          <CardBody>
            <p className="text-sm text-muted">
              {t('developerPortal.inbox.migrationsDescription')}
            </p>
          </CardBody>
        </Card>

        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Code size={18} className="text-success" />
              <h3 className="font-semibold text-heading">
                {t('developerPortal.inbox.newVersions')}
              </h3>
            </div>
          </CardHeader>
          <CardBody>
            <p className="text-sm text-muted">
              {t('developerPortal.inbox.newVersionsDescription')}
            </p>
          </CardBody>
        </Card>
      </div>

      {/* Lista de subscriptions ativas que geram notificações */}
      <Card>
        <CardHeader>
          <h3 className="font-semibold text-heading">
            {t('developerPortal.inbox.activeSubscriptions')}
          </h3>
        </CardHeader>
        <CardBody>
          {subscriptions && subscriptions.length > 0 ? (
            <div className="space-y-2">
              {subscriptions
                .filter((sub: Subscription) => sub.isActive)
                .map((sub: Subscription) => (
                  <div
                    key={sub.id}
                    className="flex items-center justify-between p-3 bg-surface rounded-lg border border-edge/50"
                  >
                    <div>
                      <span className="font-medium text-body">{sub.apiName}</span>
                      <div className="flex gap-2 mt-1">
                        <Badge variant="info">
                          {t(`developerPortal.subscriptions.levels.${sub.level}`)}
                        </Badge>
                        <Badge variant="default">
                          {t(`developerPortal.subscriptions.channels.${sub.channel}`)}
                        </Badge>
                      </div>
                    </div>
                    <Badge variant="success">
                      {t('developerPortal.subscriptions.active')}
                    </Badge>
                  </div>
                ))}
            </div>
          ) : (
            <p className="text-muted text-sm text-center py-4">
              {t('developerPortal.inbox.noSubscriptions')}
            </p>
          )}
        </CardBody>
      </Card>
    </div>
  );
}
