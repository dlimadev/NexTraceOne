/**
 * Tab de Subscriptions do Developer Portal.
 *
 * Extraído de DeveloperPortalPage para reduzir complexidade.
 * Contém formulário de criação e tabela de subscriptions existentes.
 */
import { useTranslation } from 'react-i18next';
import { Plus, Trash2 } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import type { Subscription, SubscriptionLevel, NotificationChannel } from '../../../types';
import type { SubscriptionForm } from './DeveloperPortalPage';

const SUBSCRIPTION_LEVELS: SubscriptionLevel[] = [
  'BreakingChangesOnly',
  'AllChanges',
  'DeprecationNotices',
  'SecurityAdvisories',
];

const NOTIFICATION_CHANNELS: NotificationChannel[] = ['Email', 'Webhook'];

export interface DevPortalSubscriptionsTabProps {
  showSubForm: boolean;
  onToggleForm: () => void;
  subForm: SubscriptionForm;
  onSubFormChange: (form: SubscriptionForm) => void;
  onSubscribe: () => void;
  isSubscribing: boolean;
  subscriptions: Subscription[] | undefined;
  isLoading: boolean;
  onDeleteSubscription: (id: string) => void;
  fieldClass: string;
}

export function DevPortalSubscriptionsTab({
  showSubForm,
  onToggleForm,
  subForm,
  onSubFormChange,
  onSubscribe,
  isSubscribing,
  subscriptions,
  isLoading,
  onDeleteSubscription,
  fieldClass,
}: DevPortalSubscriptionsTabProps) {
  const { t } = useTranslation();

  return (
    <div className="space-y-4">
      <div className="flex justify-between items-center">
        <h2 className="text-lg font-semibold text-heading">
          {t('developerPortal.subscriptions.title')}
        </h2>
        <Button onClick={onToggleForm}>
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
                  onChange={(e) => onSubFormChange({ ...subForm, apiAssetId: e.target.value })}
                />
              </div>
              <div>
                <label className="block text-sm font-medium text-body mb-1">
                  {t('developerPortal.subscriptions.form.apiName')}
                </label>
                <input
                  className={fieldClass}
                  value={subForm.apiName}
                  onChange={(e) => onSubFormChange({ ...subForm, apiName: e.target.value })}
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
                  onChange={(e) => onSubFormChange({ ...subForm, subscriberEmail: e.target.value })}
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
                    onSubFormChange({ ...subForm, consumerServiceName: e.target.value })
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
                    onSubFormChange({ ...subForm, consumerServiceVersion: e.target.value })
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
                    onSubFormChange({ ...subForm, level: e.target.value as SubscriptionLevel })
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
                    onSubFormChange({ ...subForm, channel: e.target.value as NotificationChannel })
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
                    onChange={(e) => onSubFormChange({ ...subForm, webhookUrl: e.target.value })}
                  />
                </div>
              )}
            </div>
            <div className="mt-4">
              <Button onClick={onSubscribe} disabled={isSubscribing}>
                {t('developerPortal.subscriptions.form.submit')}
              </Button>
            </div>
          </CardBody>
        </Card>
      )}

      {isLoading && (
        <p className="text-muted text-sm">{t('common.loading')}</p>
      )}
      {subscriptions && subscriptions.length === 0 && (
        <p className="text-muted text-sm">
          {t('developerPortal.subscriptions.noSubscriptions')}
        </p>
      )}
      {subscriptions && subscriptions.length > 0 && (
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead className="sticky top-0 z-10 bg-panel">
              <tr className="border-b border-edge text-left text-muted">
                <th className="py-2 px-3">{t('developerPortal.subscriptions.apiName')}</th>
                <th className="py-2 px-3">{t('developerPortal.subscriptions.level')}</th>
                <th className="py-2 px-3">{t('developerPortal.subscriptions.channel')}</th>
                <th className="py-2 px-3">{t('developerPortal.subscriptions.status')}</th>
                <th className="py-2 px-3">{t('common.actions')}</th>
              </tr>
            </thead>
            <tbody>
              {subscriptions.map((sub: Subscription) => (
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
                      onClick={() => onDeleteSubscription(sub.id)}
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
  );
}
