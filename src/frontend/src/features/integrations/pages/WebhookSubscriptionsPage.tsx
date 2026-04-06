import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import {
  Webhook, Plus, Shield, ShieldOff, CheckCircle, XCircle, Tag, Clock,
} from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../components/Card';
import { Badge } from '../../../components/Badge';
import { PageContainer, PageSection } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import { integrationsApi } from '../api/integrations';
import type { WebhookSubscriptionDto } from '../api/integrations';

const categoryColor = (category: string): 'danger' | 'warning' | 'info' | 'success' | 'default' => {
  switch (category) {
    case 'Incidents': return 'danger';
    case 'Changes': return 'warning';
    case 'Contracts': return 'info';
    case 'Services': return 'success';
    case 'Alerts': return 'default';
    default: return 'default';
  }
};

const EVENT_TYPE_I18N: Record<string, string> = {
  'incident.created': 'incidentCreated',
  'incident.resolved': 'incidentResolved',
  'change.deployed': 'changeDeployed',
  'change.promoted': 'changePromoted',
  'contract.published': 'contractPublished',
  'contract.deprecated': 'contractDeprecated',
  'service.registered': 'serviceRegistered',
  'alert.triggered': 'alertTriggered',
};

const ALL_EVENT_TYPE_CODES = Object.keys(EVENT_TYPE_I18N);

function truncateUrl(url: string, max = 50): string {
  if (url.length <= max) return url;
  return url.slice(0, max) + '…';
}

interface RegisterFormState {
  name: string;
  targetUrl: string;
  selectedEvents: string[];
  secret: string;
  description: string;
  isActive: boolean;
}

const INITIAL_FORM: RegisterFormState = {
  name: '',
  targetUrl: '',
  selectedEvents: [],
  secret: '',
  description: '',
  isActive: true,
};

export function WebhookSubscriptionsPage() {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const [showForm, setShowForm] = useState(false);
  const [form, setForm] = useState<RegisterFormState>(INITIAL_FORM);
  const [formError, setFormError] = useState<string | null>(null);
  const [formSuccess, setFormSuccess] = useState(false);

  const { data: eventTypesData } = useQuery({
    queryKey: ['integrations', 'webhooks', 'event-types'],
    queryFn: () => integrationsApi.getWebhookEventTypes(),
    staleTime: 3_600_000,
  });

  const { data: subscriptionsData, isLoading, isError, refetch } = useQuery({
    queryKey: ['integrations', 'webhooks', 'subscriptions'],
    queryFn: () => integrationsApi.listWebhookSubscriptions(),
    staleTime: 30_000,
  });

  const registerMutation = useMutation({
    mutationFn: (req: RegisterFormState) =>
      integrationsApi.registerWebhookSubscription({
        tenantId: 'current',
        name: req.name,
        targetUrl: req.targetUrl,
        eventTypes: req.selectedEvents,
        secret: req.secret || undefined,
        description: req.description || undefined,
        isActive: req.isActive,
      }),
    onSuccess: () => {
      setFormSuccess(true);
      setFormError(null);
      setForm(INITIAL_FORM);
      setShowForm(false);
      queryClient.invalidateQueries({ queryKey: ['integrations', 'webhooks', 'subscriptions'] });
    },
    onError: () => {
      setFormError(t('webhookSubscriptions.createError'));
      setFormSuccess(false);
    },
  });

  const eventTypes = eventTypesData?.eventTypes ?? [];
  const subscriptions: WebhookSubscriptionDto[] = subscriptionsData?.items ?? [];

  const handleEventToggle = (code: string) => {
    setForm(prev => ({
      ...prev,
      selectedEvents: prev.selectedEvents.includes(code)
        ? prev.selectedEvents.filter(e => e !== code)
        : [...prev.selectedEvents, code],
    }));
  };

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    setFormError(null);
    setFormSuccess(false);
    registerMutation.mutate(form);
  };

  if (isLoading) return <PageLoadingState />;
  if (isError) return (
    <PageErrorState
      action={
        <button onClick={() => refetch()} className="btn btn-sm btn-primary">
          {t('common.retry')}
        </button>
      }
    />
  );

  return (
    <PageContainer>
      <PageHeader
        title={t('webhookSubscriptions.title')}
        subtitle={t('webhookSubscriptions.subtitle')}
        actions={
          <button
            className="btn btn-sm btn-primary flex items-center gap-2"
            onClick={() => { setShowForm(s => !s); setFormSuccess(false); setFormError(null); }}
          >
            <Plus size={16} />
            {t('webhookSubscriptions.registerWebhook')}
          </button>
        }
      />

      {/* Available Event Types */}
      <PageSection>
        <Card>
          <CardHeader>
            <div className="flex items-center gap-2">
              <Tag size={16} className="text-accent" />
              <span className="text-sm font-semibold text-primary">{t('webhookSubscriptions.availableEventTypes')}</span>
            </div>
          </CardHeader>
          <CardBody>
            <div className="flex flex-wrap gap-2">
              {eventTypes.length > 0
                ? eventTypes.map(et => (
                    <div key={et.code} className="flex items-center gap-1.5" title={et.description}>
                      <Badge variant={categoryColor(et.category)}>{et.category}</Badge>
                      <span className="text-xs text-secondary font-mono">{et.code}</span>
                    </div>
                  ))
                : ALL_EVENT_TYPE_CODES.map(code => (
                    <div key={code} className="flex items-center gap-1.5">
                      <span className="text-xs text-secondary font-mono">{code}</span>
                    </div>
                  ))
              }
            </div>
          </CardBody>
        </Card>
      </PageSection>

      {/* Registration Form */}
      {showForm && (
        <PageSection>
          <Card>
            <CardHeader>
              <div className="flex items-center gap-2">
                <Webhook size={16} className="text-accent" />
                <span className="text-sm font-semibold text-primary">{t('webhookSubscriptions.registerWebhook')}</span>
              </div>
            </CardHeader>
            <CardBody>
              <form onSubmit={handleSubmit} className="space-y-4 max-w-xl">
                <div>
                  <label className="block text-xs font-medium text-secondary mb-1">
                    {t('webhookSubscriptions.webhookName')}
                  </label>
                  <input
                    type="text"
                    className="input input-sm w-full"
                    value={form.name}
                    onChange={e => setForm(p => ({ ...p, name: e.target.value }))}
                    required
                    maxLength={100}
                  />
                </div>

                <div>
                  <label className="block text-xs font-medium text-secondary mb-1">
                    {t('webhookSubscriptions.targetUrl')}
                  </label>
                  <input
                    type="url"
                    className="input input-sm w-full"
                    value={form.targetUrl}
                    onChange={e => setForm(p => ({ ...p, targetUrl: e.target.value }))}
                    placeholder="https://"
                    required
                    maxLength={500}
                  />
                </div>

                <div>
                  <label className="block text-xs font-medium text-secondary mb-2">
                    {t('webhookSubscriptions.selectEvents')}
                  </label>
                  <div className="grid grid-cols-2 gap-1.5">
                    {ALL_EVENT_TYPE_CODES.map(code => (
                      <label key={code} className="flex items-center gap-2 cursor-pointer select-none">
                        <input
                          type="checkbox"
                          checked={form.selectedEvents.includes(code)}
                          onChange={() => handleEventToggle(code)}
                          className="checkbox checkbox-sm"
                        />
                        <span className="text-xs text-secondary font-mono">{code}</span>
                      </label>
                    ))}
                  </div>
                </div>

                <div>
                  <label className="block text-xs font-medium text-secondary mb-1">
                    {t('webhookSubscriptions.secret')}
                  </label>
                  <input
                    type="password"
                    className="input input-sm w-full"
                    value={form.secret}
                    onChange={e => setForm(p => ({ ...p, secret: e.target.value }))}
                    autoComplete="new-password"
                  />
                  <p className="text-xs text-muted mt-1">{t('webhookSubscriptions.secretHint')}</p>
                </div>

                <div>
                  <label className="block text-xs font-medium text-secondary mb-1">
                    {t('webhookSubscriptions.description')}
                  </label>
                  <textarea
                    className="input input-sm w-full min-h-[60px]"
                    value={form.description}
                    onChange={e => setForm(p => ({ ...p, description: e.target.value }))}
                    maxLength={500}
                  />
                </div>

                <div className="flex items-center gap-2">
                  <input
                    type="checkbox"
                    id="webhook-active"
                    className="checkbox checkbox-sm"
                    checked={form.isActive}
                    onChange={e => setForm(p => ({ ...p, isActive: e.target.checked }))}
                  />
                  <label htmlFor="webhook-active" className="text-xs text-secondary cursor-pointer">
                    {t('webhookSubscriptions.isActive')}
                  </label>
                </div>

                {formError && (
                  <p className="text-xs text-critical">{formError}</p>
                )}

                <div className="flex gap-2 pt-1">
                  <button
                    type="submit"
                    className="btn btn-sm btn-primary"
                    disabled={registerMutation.isPending}
                  >
                    {t('webhookSubscriptions.submit')}
                  </button>
                  <button
                    type="button"
                    className="btn btn-sm btn-ghost"
                    onClick={() => setShowForm(false)}
                  >
                    {t('common.cancel')}
                  </button>
                </div>
              </form>
            </CardBody>
          </Card>
        </PageSection>
      )}

      {formSuccess && (
        <PageSection>
          <div className="flex items-center gap-2 text-success text-sm">
            <CheckCircle size={16} />
            {t('webhookSubscriptions.createSuccess')}
          </div>
        </PageSection>
      )}

      {/* Subscriptions List */}
      <PageSection>
        {subscriptions.length === 0 ? (
          <Card>
            <CardBody>
              <div className="flex flex-col items-center justify-center py-12 text-center gap-3">
                <Webhook size={32} className="text-muted" />
                <p className="text-sm font-medium text-primary">{t('webhookSubscriptions.noWebhooks')}</p>
                <p className="text-xs text-muted max-w-sm">{t('webhookSubscriptions.noWebhooksHint')}</p>
              </div>
            </CardBody>
          </Card>
        ) : (
          <div className="space-y-3">
            {subscriptions.map(sub => (
              <Card key={sub.subscriptionId}>
                <CardBody>
                  <div className="flex flex-wrap items-start justify-between gap-3">
                    <div className="flex-1 min-w-0">
                      <div className="flex items-center gap-2 mb-1">
                        <span className="text-sm font-semibold text-primary truncate">{sub.name}</span>
                        {sub.isActive
                          ? <Badge variant="success"><CheckCircle size={10} className="inline mr-0.5" />{t('integrations.active')}</Badge>
                          : <Badge variant="default"><XCircle size={10} className="inline mr-0.5" />{t('integrations.disabled')}</Badge>
                        }
                        {sub.hasSecret
                          ? <Badge variant="info"><Shield size={10} className="inline mr-0.5" />{t('webhookSubscriptions.hasSecret')}</Badge>
                          : <Badge variant="default"><ShieldOff size={10} className="inline mr-0.5" /></Badge>
                        }
                      </div>
                      <p className="text-xs text-muted font-mono mb-2" title={sub.targetUrl}>
                        {truncateUrl(sub.targetUrl)}
                      </p>
                      <div className="flex flex-wrap gap-1.5">
                        {sub.eventTypes.map(code => (
                          <Badge key={code} variant="default">
                            <span className="font-mono text-xs">{code}</span>
                          </Badge>
                        ))}
                      </div>
                    </div>
                    <div className="flex flex-col items-end gap-1 text-xs text-muted shrink-0">
                      <span className="flex items-center gap-1">
                        <Tag size={11} />
                        {sub.eventCount} {t('webhookSubscriptions.eventCount')}
                      </span>
                      {sub.lastTriggeredAt && (
                        <span className="flex items-center gap-1">
                          <Clock size={11} />
                          {t('webhookSubscriptions.lastTriggered')}: {new Date(sub.lastTriggeredAt).toLocaleString()}
                        </span>
                      )}
                    </div>
                  </div>
                </CardBody>
              </Card>
            ))}
          </div>
        )}
      </PageSection>
    </PageContainer>
  );
}
