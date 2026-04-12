/**
 * Builder visual para Webhooks.
 *
 * Permite criar/editar visualmente:
 * - nome, descrição, método HTTP, URL pattern, content type
 * - autenticação (none, hmac, basic, bearer, api-key)
 * - headers customizados, payload schema
 * - política de retry, eventos trigger
 * - sincronização com NTO YAML via builderSync
 */
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus, Trash2, ChevronDown, ChevronRight, AlertCircle } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import {
  Field, FieldArea, FieldSelect, FieldCheckbox, FieldTagInput,
} from './shared/BuilderFormPrimitives';
import { validateWebhookBuilder } from './shared/builderValidation';
import { webhookBuilderToYaml } from './shared/builderSync';
import type {
  WebhookBuilderState,
  WebhookHeader,
  BuilderValidationResult,
  SyncResult,
} from './shared/builderTypes';

const METHOD_OPTIONS = ['POST', 'PUT', 'PATCH'] as const;
const AUTH_OPTIONS = ['none', 'hmac-sha256', 'basic', 'bearer', 'api-key'] as const;

let nextId = 1;
function genId() { return `whk-${nextId++}`; }

function createHeader(): WebhookHeader {
  return { id: genId(), name: '', value: '', required: false };
}

interface VisualWebhookBuilderProps {
  initialState?: WebhookBuilderState;
  onChange?: (state: WebhookBuilderState) => void;
  onSync?: (result: SyncResult) => void;
  isReadOnly?: boolean;
  className?: string;
}

/**
 * Builder visual para Webhooks — permite definir URL, autenticação, headers,
 * payload schema e políticas de retry sem editar YAML manualmente.
 */
export function VisualWebhookBuilder({
  initialState,
  onChange,
  onSync,
  isReadOnly = false,
  className = '',
}: VisualWebhookBuilderProps) {
  const { t } = useTranslation();

  const [state, setState] = useState<WebhookBuilderState>(
    initialState ?? {
      name: '',
      description: '',
      method: 'POST',
      urlPattern: '',
      contentType: 'application/json',
      payloadSchema: '',
      headers: [],
      authentication: 'none',
      secretHeaderName: '',
      retryPolicy: '',
      retryCount: '3',
      timeout: '30',
      events: [],
      owner: '',
      observabilityNotes: '',
    },
  );
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [validation, setValidation] = useState<BuilderValidationResult | null>(null);

  const update = useCallback(
    (partial: Partial<WebhookBuilderState>) => {
      const next = { ...state, ...partial };
      setState(next);
      onChange?.(next);
    },
    [state, onChange],
  );

  const addHeader = () => {
    const h = createHeader();
    update({ headers: [...state.headers, h] });
    setExpandedId(h.id);
  };

  const updateHeader = (id: string, partial: Partial<WebhookHeader>) => {
    update({ headers: state.headers.map((h) => (h.id === id ? { ...h, ...partial } : h)) });
  };

  const removeHeader = (id: string) => {
    update({ headers: state.headers.filter((h) => h.id !== id) });
    if (expandedId === id) setExpandedId(null);
  };

  const handleValidate = () => { setValidation(validateWebhookBuilder(state)); };

  const handleGenerateSource = () => {
    const result = webhookBuilderToYaml(state);
    onSync?.(result);
  };

  const fieldError = (field: string) => validation?.errors.find((e) => e.field === field);

  const needsSecretHeader = state.authentication === 'hmac-sha256' || state.authentication === 'bearer' || state.authentication === 'api-key';

  return (
    <div className={`space-y-4 p-4 ${className}`}>
      {/* ── Validation banner ── */}
      {validation && !validation.valid && (
        <div className="flex items-start gap-2 px-3 py-2.5 text-xs rounded-md bg-danger/10 border border-danger/20 text-danger">
          <AlertCircle size={14} className="flex-shrink-0 mt-0.5" />
          <div>
            <p className="font-medium">{t('contracts.builder.validation.hasErrors', 'Please fix the following issues:')}</p>
            <ul className="mt-1 space-y-0.5">
              {validation.errors.map((e, i) => (
                <li key={i} className="text-[10px]">• {t(e.messageKey, e.fallback)}</li>
              ))}
            </ul>
          </div>
        </div>
      )}

      {/* ── Webhook metadata ── */}
      <Card>
        <CardHeader>
          <h3 className="text-xs font-semibold text-heading">
            {t('contracts.builder.webhook.title', 'Webhook Builder')}
          </h3>
        </CardHeader>
        <CardBody className="space-y-3">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <Field
              label={t('contracts.builder.webhook.name', 'Webhook Name')}
              value={state.name}
              onChange={(v) => update({ name: v })}
              placeholder={t('contracts.webhook.placeholder.name', 'order-created-webhook')}
              required
              error={fieldError('name') ? t(fieldError('name')!.messageKey, fieldError('name')!.fallback) : undefined}
              disabled={isReadOnly}
            />
            <FieldSelect
              label={t('contracts.builder.rest.method', 'Method')}
              value={state.method}
              onChange={(v) => update({ method: v as WebhookBuilderState['method'] })}
              options={METHOD_OPTIONS}
              disabled={isReadOnly}
            />
          </div>
          <Field
            label={t('contracts.builder.webhook.urlPattern', 'URL Pattern')}
            value={state.urlPattern}
            onChange={(v) => update({ urlPattern: v })}
            placeholder={t('contracts.builder.webhook.urlPatternPlaceholder', 'https://example.com/webhooks/{event}')}
            required
            mono
            error={fieldError('urlPattern') ? t(fieldError('urlPattern')!.messageKey, fieldError('urlPattern')!.fallback) : undefined}
            disabled={isReadOnly}
          />
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <Field
              label={t('contracts.builder.webhook.contentType', 'Content Type')}
              value={state.contentType}
              onChange={(v) => update({ contentType: v })}
              placeholder={t('contracts.webhook.placeholder.contentType', 'application/json')}
              disabled={isReadOnly}
            />
            <Field
              label={t('contracts.builder.rest.contact', 'Owner')}
              value={state.owner}
              onChange={(v) => update({ owner: v })}
              placeholder={t('contracts.webhook.placeholder.owner', 'integrations-team')}
              disabled={isReadOnly}
            />
          </div>
          <FieldArea
            label={t('contracts.builder.rest.description', 'Description')}
            value={state.description}
            onChange={(v) => update({ description: v })}
            placeholder={t('contracts.builder.soap.descPlaceholder', 'Describe this webhook...')}
            disabled={isReadOnly}
          />
        </CardBody>
      </Card>

      {/* ── Authentication ── */}
      <Card>
        <CardHeader>
          <h3 className="text-xs font-semibold text-heading">
            {t('contracts.builder.webhook.authentication', 'Authentication')}
          </h3>
        </CardHeader>
        <CardBody className="space-y-3">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <FieldSelect
              label={t('contracts.builder.webhook.authentication', 'Authentication')}
              value={state.authentication}
              onChange={(v) => update({ authentication: v as WebhookBuilderState['authentication'] })}
              options={AUTH_OPTIONS}
              disabled={isReadOnly}
            />
            {needsSecretHeader && (
              <Field
                label={t('contracts.builder.webhook.secretHeaderName', 'Secret Header Name')}
                value={state.secretHeaderName}
                onChange={(v) => update({ secretHeaderName: v })}
                placeholder={t('contracts.webhook.placeholder.secretHeader', 'X-Webhook-Secret')}
                required
                error={fieldError('secretHeaderName') ? t(fieldError('secretHeaderName')!.messageKey, fieldError('secretHeaderName')!.fallback) : undefined}
                disabled={isReadOnly}
              />
            )}
          </div>
        </CardBody>
      </Card>

      {/* ── Headers ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <h3 className="text-xs font-semibold text-heading">
              {t('contracts.builder.rest.parameters', 'Headers')} ({state.headers.length})
            </h3>
            {!isReadOnly && (
              <button type="button" onClick={addHeader} className="inline-flex items-center gap-1 px-2 py-1 text-[10px] font-medium rounded-md bg-accent/10 text-accent hover:bg-accent/20 transition-colors">
                <Plus size={10} /> {t('contracts.builder.webhook.addHeader', 'Add Header')}
              </button>
            )}
          </div>
        </CardHeader>
        <CardBody className="p-0">
          {state.headers.length === 0 && (
            <div className="py-8 text-center text-xs text-muted">
              {t('contracts.builder.webhook.noHeaders', 'No custom headers defined.')}
            </div>
          )}
          <div className="divide-y divide-edge">
            {state.headers.map((h) => {
              const isExpanded = expandedId === h.id;
              return (
                <div key={h.id} className="group">
                  <button type="button" onClick={() => setExpandedId(isExpanded ? null : h.id)} className="w-full flex items-center gap-3 px-4 py-3 text-left hover:bg-elevated/30 transition-colors">
                    {isExpanded ? <ChevronDown size={12} className="text-muted" /> : <ChevronRight size={12} className="text-muted" />}
                    <span className="px-2 py-0.5 text-[10px] font-bold rounded bg-accent/15 text-accent border border-accent/25">HDR</span>
                    <span className="text-xs font-mono text-heading flex-1 truncate">{h.name || t('contracts.builder.soap.unnamed', 'Unnamed Header')}</span>
                    {h.required && <span className="text-[10px] text-danger">*</span>}
                    {!isReadOnly && (
                      <button type="button" onClick={(e) => { e.stopPropagation(); removeHeader(h.id); }} className="opacity-0 group-hover:opacity-100 text-muted hover:text-danger transition-all">
                        <Trash2 size={12} />
                      </button>
                    )}
                  </button>
                  {isExpanded && (
                    <div className="px-4 pb-4 pt-1 bg-elevated/10 space-y-3">
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                        <Field label={t('contracts.builder.rest.paramName', 'Name')} value={h.name}
                          onChange={(v) => updateHeader(h.id, { name: v })} placeholder={t('contracts.webhook.placeholder.headerName', 'X-Custom-Header')} required disabled={isReadOnly} />
                        <Field label={t('contracts.builder.rest.defaultValue', 'Value')} value={h.value}
                          onChange={(v) => updateHeader(h.id, { value: v })} placeholder={t('contracts.webhook.placeholder.headerValue', 'header-value')} disabled={isReadOnly} />
                      </div>
                      <FieldCheckbox label={t('contracts.builder.rest.required', 'Required')} checked={h.required}
                        onChange={(v) => updateHeader(h.id, { required: v })} disabled={isReadOnly} />
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </CardBody>
      </Card>

      {/* ── Payload & Retry ── */}
      <Card>
        <CardHeader>
          <h3 className="text-xs font-semibold text-heading">
            {t('contracts.builder.webhook.payloadSchema', 'Payload Schema')}
          </h3>
        </CardHeader>
        <CardBody className="space-y-3">
          <FieldArea
            label=""
            value={state.payloadSchema}
            onChange={(v) => update({ payloadSchema: v })}
            placeholder={t('contracts.builder.webhook.payloadSchemaPlaceholder', 'Define the expected payload schema...')}
            rows={5}
            mono
            disabled={isReadOnly}
          />
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <Field
              label={t('contracts.builder.webhook.retryCount', 'Retry Count')}
              value={state.retryCount}
              onChange={(v) => update({ retryCount: v })}
              placeholder={t('contracts.builder.webhook.placeholder.retryCount', '3')}
              disabled={isReadOnly}
            />
            <Field
              label={t('contracts.builder.webhook.timeout', 'Timeout (seconds)')}
              value={state.timeout}
              onChange={(v) => update({ timeout: v })}
              placeholder={t('contracts.builder.webhook.placeholder.timeout', '30')}
              disabled={isReadOnly}
            />
            <Field
              label={t('contracts.builder.webhook.retryPolicy', 'Retry Policy')}
              value={state.retryPolicy}
              onChange={(v) => update({ retryPolicy: v })}
              placeholder={t('contracts.webhook.placeholder.retryStrategy', 'exponential-backoff')}
              disabled={isReadOnly}
            />
          </div>
          <FieldTagInput
            label={t('contracts.builder.webhook.events', 'Trigger Events')}
            tags={state.events}
            onChange={(v) => update({ events: v })}
            placeholder={t('contracts.webhook.placeholder.events', 'order.created, order.updated')}
          />
          <FieldArea
            label={t('contracts.builder.workservice.observability', 'Observability Notes')}
            value={state.observabilityNotes}
            onChange={(v) => update({ observabilityNotes: v })}
            placeholder={t('contracts.builder.workservice.obsPlaceholder', 'Metrics, logs, traces, health checks...')}
            rows={2}
            disabled={isReadOnly}
          />
        </CardBody>
      </Card>

      {/* ── Action bar ── */}
      {!isReadOnly && (
        <div className="flex items-center justify-end gap-2">
          <button type="button" onClick={handleValidate}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-[10px] font-medium rounded-md bg-elevated border border-edge text-body hover:bg-elevated/80 transition-colors">
            {t('contracts.builder.soap.validate', 'Validate')}
          </button>
          <button type="button" onClick={handleGenerateSource}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-[10px] font-medium rounded-md bg-accent/10 text-accent border border-accent/20 hover:bg-accent/20 transition-colors">
            {t('contracts.builder.workservice.generateSource', 'Generate Definition')}
          </button>
        </div>
      )}

      {/* ── Sync warning ── */}
      <div className="px-3 py-2 text-[10px] text-muted bg-warning/5 border border-warning/15 rounded-md">
        {t('contracts.builder.soap.roundtripWarning', 'Round-trip is partial — some features may not be editable visually. Use source editor for advanced constructs.')}
      </div>
    </div>
  );
}
