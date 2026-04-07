import { useState } from 'react';
import { useTranslation } from 'react-i18next';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Webhook, Plus, Trash2, ToggleLeft, ToggleRight } from 'lucide-react';
import { PageContainer } from '../../../components/shell';
import { PageHeader } from '../../../components/PageHeader';
import { Card, CardBody } from '../../../components/Card';
import { Button } from '../../../components/Button';
import { Badge } from '../../../components/Badge';
import { EmptyState } from '../../../components/EmptyState';
import { PageLoadingState } from '../../../components/PageLoadingState';
import { PageErrorState } from '../../../components/PageErrorState';
import client from '../../../api/client';

// ── Types ──────────────────────────────────────────────────────────────────────

type EventType = 'change.created' | 'incident.opened' | 'contract.published' | 'approval.expired';

interface TemplateSummary {
  templateId: string;
  name: string;
  eventType: EventType;
  isEnabled: boolean;
  createdAt: string;
}

// ── Hooks ──────────────────────────────────────────────────────────────────────

const useWebhookTemplates = () =>
  useQuery({
    queryKey: ['webhook-templates'],
    queryFn: () =>
      client
        .get<{ items: TemplateSummary[]; totalCount: number }>('/api/v1/webhook-templates')
        .then(r => r.data),
  });

const useCreateTemplate = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: { name: string; eventType: EventType; payloadTemplate: string; headersJson?: string }) =>
      client.post('/api/v1/webhook-templates', data).then(r => r.data),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['webhook-templates'] }),
  });
};

const useToggleTemplate = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ templateId, enabled }: { templateId: string; enabled: boolean }) =>
      client.patch(`/api/v1/webhook-templates/${templateId}/toggle`, { enabled }),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['webhook-templates'] }),
  });
};

const useDeleteTemplate = () => {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (templateId: string) => client.delete(`/api/v1/webhook-templates/${templateId}`),
    onSuccess: () => qc.invalidateQueries({ queryKey: ['webhook-templates'] }),
  });
};

// ── Constants ──────────────────────────────────────────────────────────────────

const EVENT_TYPES: EventType[] = ['change.created', 'incident.opened', 'contract.published', 'approval.expired'];

const DEFAULT_PAYLOAD = `{
  "event": "{{eventType}}",
  "service": "{{serviceName}}",
  "timestamp": "{{timestamp}}"
}`;

// ── Component ──────────────────────────────────────────────────────────────────

/**
 * WebhookTemplatesPage — gestão de templates de payload personalizados para webhooks.
 * Permite ao tenant definir o formato dos payloads enviados para destinos externos.
 * Pilar: Platform Customization — Integrations & API
 */
export function WebhookTemplatesPage() {
  const { t } = useTranslation();
  const { data, isLoading, isError } = useWebhookTemplates();
  const createTemplate = useCreateTemplate();
  const toggleTemplate = useToggleTemplate();
  const deleteTemplate = useDeleteTemplate();

  const [showForm, setShowForm] = useState(false);
  const [name, setName] = useState('');
  const [eventType, setEventType] = useState<EventType>('change.created');
  const [payloadTemplate, setPayloadTemplate] = useState(DEFAULT_PAYLOAD);

  if (isLoading) return <PageContainer><PageLoadingState /></PageContainer>;
  if (isError) return <PageContainer><PageErrorState /></PageContainer>;

  const items = data?.items ?? [];

  const handleCreate = () => {
    if (!name.trim() || !payloadTemplate.trim()) return;
    createTemplate.mutate(
      { name, eventType, payloadTemplate },
      {
        onSuccess: () => {
          setShowForm(false);
          setName('');
          setPayloadTemplate(DEFAULT_PAYLOAD);
          setEventType('change.created');
        },
      }
    );
  };

  return (
    <PageContainer>
      <PageHeader
        title={t('webhookTemplates.title')}
        actions={
          <Button variant="primary" onClick={() => setShowForm(s => !s)}>
            <Plus size={16} className="mr-1" />
            {t('webhookTemplates.create')}
          </Button>
        }
      />

      {showForm && (
        <Card className="mb-6">
          <CardBody>
            <div className="space-y-4">
              <div>
                <label className="block text-sm font-medium mb-1">{t('common.name', 'Name')}</label>
                <input
                  type="text"
                  value={name}
                  onChange={e => setName(e.target.value)}
                  className="w-full px-3 py-1.5 text-sm border rounded bg-transparent"
                  placeholder={t('common.name', 'Name')}
                />
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">{t('webhookTemplates.eventType')}</label>
                <select
                  value={eventType}
                  onChange={e => setEventType(e.target.value as EventType)}
                  className="w-64 px-3 py-1.5 text-sm border rounded bg-white dark:bg-gray-900"
                >
                  {EVENT_TYPES.map(et => (
                    <option key={et} value={et}>{et}</option>
                  ))}
                </select>
              </div>
              <div>
                <label className="block text-sm font-medium mb-1">{t('webhookTemplates.payload')}</label>
                <textarea
                  value={payloadTemplate}
                  onChange={e => setPayloadTemplate(e.target.value)}
                  rows={6}
                  className="w-full px-3 py-2 text-sm border rounded bg-transparent font-mono"
                />
              </div>
              <div className="flex gap-2">
                <Button
                  variant="primary"
                  onClick={handleCreate}
                  disabled={createTemplate.isPending}
                >
                  {t('webhookTemplates.create')}
                </Button>
                <Button variant="ghost" onClick={() => setShowForm(false)}>
                  {t('common.cancel', 'Cancel')}
                </Button>
              </div>
            </div>
          </CardBody>
        </Card>
      )}

      {items.length === 0 ? (
        <EmptyState
          icon={<Webhook size={32} />}
          title={t('webhookTemplates.empty')}
        />
      ) : (
        <Card>
          <CardBody className="p-0">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b text-left">
                  <th className="px-4 py-3 font-medium">{t('common.name', 'Name')}</th>
                  <th className="px-4 py-3 font-medium">{t('webhookTemplates.eventType')}</th>
                  <th className="px-4 py-3 font-medium">{t('common.status', 'Status')}</th>
                  <th className="px-4 py-3 font-medium w-24">{t('common.actions', 'Actions')}</th>
                </tr>
              </thead>
              <tbody>
                {items.map(item => (
                  <tr key={item.templateId} className="border-b last:border-0 hover:bg-muted/30 transition-colors">
                    <td className="px-4 py-3 font-medium">{item.name}</td>
                    <td className="px-4 py-3">
                      <code className="text-xs bg-muted px-2 py-0.5 rounded">{item.eventType}</code>
                    </td>
                    <td className="px-4 py-3">
                      <Badge variant={item.isEnabled ? 'success' : 'neutral'}>
                        {item.isEnabled ? t('webhookTemplates.enabled') : t('webhookTemplates.disabled')}
                      </Badge>
                    </td>
                    <td className="px-4 py-3">
                      <div className="flex gap-1">
                        <button
                          type="button"
                          title={item.isEnabled ? t('webhookTemplates.disabled') : t('webhookTemplates.enabled')}
                          onClick={() => toggleTemplate.mutate({ templateId: item.templateId, enabled: !item.isEnabled })}
                          className="p-1 rounded hover:bg-muted text-muted-foreground hover:text-foreground transition-colors"
                        >
                          {item.isEnabled ? <ToggleRight size={16} className="text-success" /> : <ToggleLeft size={16} />}
                        </button>
                        <button
                          type="button"
                          title={t('common.delete', 'Delete')}
                          onClick={() => deleteTemplate.mutate(item.templateId)}
                          className="p-1 rounded hover:bg-muted text-muted-foreground hover:text-critical transition-colors"
                        >
                          <Trash2 size={16} />
                        </button>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </CardBody>
        </Card>
      )}
    </PageContainer>
  );
}

export default WebhookTemplatesPage;
