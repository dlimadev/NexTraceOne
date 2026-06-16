/**
 * Tab de Subscriptions do Developer Portal.
 *
 * Extraído de DeveloperPortalPage para reduzir complexidade.
 * Contém formulário de criação e tabela de subscriptions existentes.
 * Redesenhado com componentes DS: TextField, Select, Button, EmptyState, PageLoadingState.
 */
import { useTranslation } from 'react-i18next';
import { Plus, Trash2 } from 'lucide-react';
import { Card, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { TextField } from '../../../components/TextField';
import { Select } from '../../../components/Select';
import { EmptyState } from '../../../components/EmptyState';
import { PageLoadingState } from '../../../components/PageLoadingState';
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
}: DevPortalSubscriptionsTabProps) {
  const { t } = useTranslation();

  return (
    <div className="space-y-4">
      {/* Cabeçalho de secção com CTA primário */}
      <div className="flex justify-between items-center">
        <h2 className="text-base font-semibold text-heading">
          {t('developerPortal.subscriptions.title')}
        </h2>
        <Button variant="primary" size="sm" icon={<Plus size={14} />} onClick={onToggleForm}>
          {t('developerPortal.subscriptions.create')}
        </Button>
      </div>

      {/* Formulário de criação inline */}
      {showSubForm && (
        <Card>
          <CardBody>
            <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
              {/* Campos de identificação da API */}
              <TextField
                label={t('developerPortal.subscriptions.form.apiAssetId')}
                value={subForm.apiAssetId}
                onChange={(e) => onSubFormChange({ ...subForm, apiAssetId: e.target.value })}
              />
              <TextField
                label={t('developerPortal.subscriptions.form.apiName')}
                value={subForm.apiName}
                onChange={(e) => onSubFormChange({ ...subForm, apiName: e.target.value })}
              />

              {/* Email e serviço consumidor */}
              <TextField
                label={t('developerPortal.subscriptions.form.subscriberEmail')}
                type="email"
                value={subForm.subscriberEmail}
                onChange={(e) => onSubFormChange({ ...subForm, subscriberEmail: e.target.value })}
              />
              <TextField
                label={t('developerPortal.subscriptions.form.consumerServiceName')}
                value={subForm.consumerServiceName}
                onChange={(e) =>
                  onSubFormChange({ ...subForm, consumerServiceName: e.target.value })
                }
              />
              <TextField
                label={t('developerPortal.subscriptions.form.consumerServiceVersion')}
                value={subForm.consumerServiceVersion}
                onChange={(e) =>
                  onSubFormChange({ ...subForm, consumerServiceVersion: e.target.value })
                }
              />

              {/* Nível de notificação */}
              <Select
                label={t('developerPortal.subscriptions.form.level')}
                value={subForm.level}
                onChange={(e) =>
                  onSubFormChange({ ...subForm, level: e.target.value as SubscriptionLevel })
                }
                options={SUBSCRIPTION_LEVELS.map((lvl) => ({
                  value: lvl,
                  label: t(`developerPortal.subscriptions.levels.${lvl}`),
                }))}
              />

              {/* Canal de notificação */}
              <Select
                label={t('developerPortal.subscriptions.form.channel')}
                value={subForm.channel}
                onChange={(e) =>
                  onSubFormChange({ ...subForm, channel: e.target.value as NotificationChannel })
                }
                options={NOTIFICATION_CHANNELS.map((ch) => ({
                  value: ch,
                  label: t(`developerPortal.subscriptions.channels.${ch}`),
                }))}
              />

              {/* Webhook URL — exibido apenas quando canal é Webhook */}
              {subForm.channel === 'Webhook' && (
                <TextField
                  label={t('developerPortal.subscriptions.form.webhookUrl')}
                  value={subForm.webhookUrl}
                  onChange={(e) => onSubFormChange({ ...subForm, webhookUrl: e.target.value })}
                />
              )}
            </div>

            <div className="mt-4">
              <Button
                variant="primary"
                onClick={onSubscribe}
                loading={isSubscribing}
                disabled={isSubscribing}
              >
                {t('developerPortal.subscriptions.form.submit')}
              </Button>
            </div>
          </CardBody>
        </Card>
      )}

      {/* Estado de carregamento */}
      {isLoading && <PageLoadingState size="sm" />}

      {/* Estado vazio */}
      {!isLoading && subscriptions && subscriptions.length === 0 && (
        <EmptyState
          title={t('developerPortal.subscriptions.noSubscriptions')}
          size="compact"
        />
      )}

      {/* Tabela de subscriptions */}
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
                    {/* Botão de exclusão via DS — variante ghost com ícone crítico */}
                    <Button
                      variant="ghost"
                      size="sm"
                      icon={<Trash2 size={14} className="text-critical" />}
                      onClick={() => onDeleteSubscription(sub.id)}
                      aria-label={t('developerPortal.subscriptions.unsubscribe')}
                    >
                      <span className="sr-only">{t('developerPortal.subscriptions.unsubscribe')}</span>
                    </Button>
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
