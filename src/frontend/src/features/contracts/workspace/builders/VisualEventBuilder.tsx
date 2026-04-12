/**
 * Builder visual completo para Event APIs / Kafka.
 *
 * Permite modelar:
 * - topic, producer, consumer, event name, version
 * - key schema, payload schema, headers
 * - compatibility rules, retention, partitions
 * - ordering, retry, DLQ, idempotency
 * - ownership, observability metadata
 */
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus, Trash2, ChevronDown, ChevronRight, AlertCircle } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import {
  Field, FieldArea, FieldSelect, FieldCheckbox,
} from './shared/BuilderFormPrimitives';
import { validateEventBuilder } from './shared/builderValidation';
import { eventBuilderToYaml } from './shared/builderSync';
import type {
  EventBuilderState,
  EventChannel,
  CompatibilityMode,
  BuilderValidationResult,
  SyncResult,
} from './shared/builderTypes';

const COMPAT_OPTIONS: readonly CompatibilityMode[] = ['BACKWARD', 'FORWARD', 'FULL', 'NONE'];

let nextId = 1;
function genId() { return `evt-${nextId++}`; }

function createChannel(): EventChannel {
  return {
    id: genId(), topicName: '', eventName: '', version: '1.0.0',
    keySchema: '', payloadSchema: '', headers: '', producer: '', consumer: '',
    compatibility: 'BACKWARD', retention: '7d', partitions: '3', ordering: '',
    retries: '3', dlq: '', idempotent: false, description: '',
    owner: '', observabilityNotes: '',
  };
}

interface VisualEventBuilderProps {
  initialState?: EventBuilderState;
  onChange?: (state: EventBuilderState) => void;
  onSync?: (result: SyncResult) => void;
  isReadOnly?: boolean;
  className?: string;
}

/**
 * Builder visual para Event APIs / Kafka — permite definir tópicos, producers,
 * consumers, schemas e configuração de compatibilidade sem AsyncAPI manual.
 */
export function VisualEventBuilder({
  initialState,
  onChange,
  onSync,
  isReadOnly = false,
  className = '',
}: VisualEventBuilderProps) {
  const { t } = useTranslation();

  const [state, setState] = useState<EventBuilderState>(
    initialState ?? {
      title: '',
      version: '1.0.0',
      description: '',
      defaultBroker: '',
      channels: [],
    },
  );
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [validation, setValidation] = useState<BuilderValidationResult | null>(null);

  const update = useCallback(
    (partial: Partial<EventBuilderState>) => {
      const next = { ...state, ...partial };
      setState(next);
      onChange?.(next);
    },
    [state, onChange],
  );

  const addChannel = () => {
    const ch = createChannel();
    update({ channels: [...state.channels, ch] });
    setExpandedId(ch.id);
  };

  const updateChannel = (id: string, partial: Partial<EventChannel>) => {
    update({ channels: state.channels.map((c) => (c.id === id ? { ...c, ...partial } : c)) });
  };

  const removeChannel = (id: string) => {
    update({ channels: state.channels.filter((c) => c.id !== id) });
    if (expandedId === id) setExpandedId(null);
  };

  const handleValidate = () => { setValidation(validateEventBuilder(state)); };

  const handleGenerateSource = () => {
    const result = eventBuilderToYaml(state);
    onSync?.(result);
  };

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

      {/* ── Event API metadata ── */}
      <Card>
        <CardHeader>
          <h3 className="text-xs font-semibold text-heading">
            {t('contracts.builder.event.apiInfo', 'Event API Information')}
          </h3>
        </CardHeader>
        <CardBody className="space-y-3">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <Field label={t('contracts.builder.event.title', 'Title')} value={state.title}
              onChange={(v) => update({ title: v })} placeholder={t('contracts.builder.event.titlePlaceholder', 'Order Events')} required disabled={isReadOnly} />
            <Field label={t('contracts.builder.event.version', 'Version')} value={state.version}
              onChange={(v) => update({ version: v })} placeholder="1.0.0" mono disabled={isReadOnly} />
          </div>
          <Field label={t('contracts.builder.event.broker', 'Default Broker')} value={state.defaultBroker}
            onChange={(v) => update({ defaultBroker: v })} placeholder={t('contracts.builder.event.brokerPlaceholder', 'kafka://broker:9092')} mono disabled={isReadOnly} />
          <FieldArea label={t('contracts.builder.event.description', 'Description')} value={state.description}
            onChange={(v) => update({ description: v })} disabled={isReadOnly} />
        </CardBody>
      </Card>

      {/* ── Channels / Topics ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <h3 className="text-xs font-semibold text-heading">
              {t('contracts.builder.event.channels', 'Channels / Topics')} ({state.channels.length})
            </h3>
            {!isReadOnly && (
              <button type="button" onClick={addChannel} className="inline-flex items-center gap-1 px-2 py-1 text-[10px] font-medium rounded-md bg-accent/10 text-accent hover:bg-accent/20 transition-colors">
                <Plus size={10} /> {t('contracts.builder.event.addChannel', 'Add Channel')}
              </button>
            )}
          </div>
        </CardHeader>
        <CardBody className="p-0">
          {state.channels.length === 0 && (
            <div className="py-8 text-center text-xs text-muted">
              {t('contracts.builder.event.noChannels', 'No channels yet. Click "Add Channel" to define a topic/event.')}
            </div>
          )}
          <div className="divide-y divide-edge">
            {state.channels.map((ch) => {
              const isExpanded = expandedId === ch.id;
              return (
                <div key={ch.id} className="group">
                  <button type="button" onClick={() => setExpandedId(isExpanded ? null : ch.id)} className="w-full flex items-center gap-3 px-4 py-3 text-left hover:bg-elevated/30 transition-colors">
                    {isExpanded ? <ChevronDown size={12} className="text-muted" /> : <ChevronRight size={12} className="text-muted" />}
                    <span className="px-2 py-0.5 text-[10px] font-bold rounded bg-cyan/15 text-cyan border border-cyan/25">EVT</span>
                    <span className="text-xs font-mono text-heading flex-1 truncate">{ch.topicName || t('contracts.builder.event.unnamed', 'Unnamed Topic')}</span>
                    {ch.eventName && <span className="text-[10px] text-muted truncate max-w-[180px]">{ch.eventName}</span>}
                    {!isReadOnly && (
                      <button type="button" onClick={(e) => { e.stopPropagation(); removeChannel(ch.id); }} className="opacity-0 group-hover:opacity-100 text-muted hover:text-danger transition-all">
                        <Trash2 size={12} />
                      </button>
                    )}
                  </button>
                  {isExpanded && (
                    <div className="px-4 pb-4 pt-1 bg-elevated/10 space-y-3">
                      {/* Identity */}
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                        <Field label={t('contracts.builder.event.topicName', 'Topic Name')} value={ch.topicName}
                          onChange={(v) => updateChannel(ch.id, { topicName: v })} placeholder={t('contracts.builder.event.topicNamePlaceholder', 'orders.created')} required mono disabled={isReadOnly} />
                        <Field label={t('contracts.builder.event.eventName', 'Event Name')} value={ch.eventName}
                          onChange={(v) => updateChannel(ch.id, { eventName: v })} placeholder={t('contracts.builder.event.eventNamePlaceholder', 'OrderCreated')} disabled={isReadOnly} />
                      </div>
                      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
                        <Field label={t('contracts.builder.event.eventVersion', 'Event Version')} value={ch.version}
                          onChange={(v) => updateChannel(ch.id, { version: v })} placeholder="1.0.0" mono disabled={isReadOnly} />
                        <Field label={t('contracts.builder.event.producer', 'Producer')} value={ch.producer}
                          onChange={(v) => updateChannel(ch.id, { producer: v })} placeholder={t('contracts.builder.event.producerPlaceholder', 'order-service')} disabled={isReadOnly} />
                        <Field label={t('contracts.builder.event.consumer', 'Consumer')} value={ch.consumer}
                          onChange={(v) => updateChannel(ch.id, { consumer: v })} placeholder={t('contracts.builder.event.consumerPlaceholder', 'notification-service')} disabled={isReadOnly} />
                      </div>

                      {/* Schema */}
                      <FieldArea label={t('contracts.builder.event.keySchema', 'Key Schema')} value={ch.keySchema}
                        onChange={(v) => updateChannel(ch.id, { keySchema: v })} placeholder='{ "type": "string" }' rows={2} mono disabled={isReadOnly} />
                      <FieldArea label={t('contracts.builder.event.payloadSchema', 'Payload Schema')} value={ch.payloadSchema}
                        onChange={(v) => updateChannel(ch.id, { payloadSchema: v })} rows={3} mono disabled={isReadOnly} />
                      <FieldArea label={t('contracts.builder.event.headers', 'Headers')} value={ch.headers}
                        onChange={(v) => updateChannel(ch.id, { headers: v })} placeholder='correlationId: string, traceId: string' rows={2} mono disabled={isReadOnly} />

                      {/* Config */}
                      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
                        <FieldSelect label={t('contracts.builder.event.compatibility', 'Compatibility')} value={ch.compatibility}
                          onChange={(v) => updateChannel(ch.id, { compatibility: v as CompatibilityMode })}
                          options={COMPAT_OPTIONS} disabled={isReadOnly} />
                        <Field label={t('contracts.builder.event.retention', 'Retention')} value={ch.retention}
                          onChange={(v) => updateChannel(ch.id, { retention: v })} placeholder="7d" disabled={isReadOnly} />
                        <Field label={t('contracts.builder.event.partitions', 'Partitions')} value={ch.partitions}
                          onChange={(v) => updateChannel(ch.id, { partitions: v })} placeholder="3" disabled={isReadOnly} />
                      </div>
                      <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-3">
                        <Field label={t('contracts.builder.event.ordering', 'Ordering')} value={ch.ordering}
                          onChange={(v) => updateChannel(ch.id, { ordering: v })} placeholder={t('contracts.builder.event.orderingPlaceholder', 'by-key')} disabled={isReadOnly} />
                        <Field label={t('contracts.builder.event.retries', 'Retries')} value={ch.retries}
                          onChange={(v) => updateChannel(ch.id, { retries: v })} placeholder="3" disabled={isReadOnly} />
                        <Field label={t('contracts.builder.event.dlq', 'DLQ Topic')} value={ch.dlq}
                          onChange={(v) => updateChannel(ch.id, { dlq: v })} placeholder={t('contracts.builder.event.dlqPlaceholder', 'orders.created.dlq')} mono disabled={isReadOnly} />
                      </div>
                      <FieldCheckbox label={t('contracts.builder.event.idempotent', 'Idempotent')} checked={ch.idempotent}
                        onChange={(v) => updateChannel(ch.id, { idempotent: v })} disabled={isReadOnly} />

                      {/* Ownership & Observability */}
                      <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
                        <Field label={t('contracts.builder.event.owner', 'Owner')} value={ch.owner}
                          onChange={(v) => updateChannel(ch.id, { owner: v })} placeholder={t('contracts.builder.event.channelOwnerPlaceholder', 'team-orders')} disabled={isReadOnly} />
                        <FieldArea label={t('contracts.builder.event.observabilityNotes', 'Observability')} value={ch.observabilityNotes}
                          onChange={(v) => updateChannel(ch.id, { observabilityNotes: v })}
                          placeholder={t('contracts.builder.event.obsPlaceholder', 'Metrics, tracing, alerting...')}
                          rows={2} disabled={isReadOnly} />
                      </div>
                      <FieldArea label={t('contracts.builder.event.channelDescription', 'Description')} value={ch.description}
                        onChange={(v) => updateChannel(ch.id, { description: v })} rows={2} disabled={isReadOnly} />
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </CardBody>
      </Card>

      {/* ── Action bar ── */}
      {!isReadOnly && (
        <div className="flex items-center justify-end gap-2">
          <button type="button" onClick={handleValidate}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-[10px] font-medium rounded-md bg-elevated border border-edge text-body hover:bg-elevated/80 transition-colors">
            {t('contracts.builder.event.validate', 'Validate')}
          </button>
          <button type="button" onClick={handleGenerateSource}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-[10px] font-medium rounded-md bg-accent/10 text-accent border border-accent/20 hover:bg-accent/20 transition-colors">
            {t('contracts.builder.event.generateSource', 'Generate AsyncAPI')}
          </button>
        </div>
      )}
    </div>
  );
}
