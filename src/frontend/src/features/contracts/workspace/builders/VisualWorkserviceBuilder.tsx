/**
 * Builder visual completo para Workservices / Background Services.
 *
 * Permite modelar:
 * - nome, trigger, cron/schedule
 * - inputs, outputs, dependencies
 * - retries, timeout, error handling
 * - side effects, ownership, observability, health check
 */
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus, Trash2, AlertCircle } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import {
  Field, FieldArea, FieldSelect, FieldCheckbox,
} from './shared/BuilderFormPrimitives';
import { validateWorkserviceBuilder } from './shared/builderValidation';
import { workserviceBuilderToYaml } from './shared/builderSync';
import type {
  WorkserviceBuilderState,
  WorkserviceDependency,
  MessagingTopic,
  ConsumedService,
  ProducedEvent,
  TriggerType,
  MessagingRole,
  BuilderValidationResult,
  SyncResult,
} from './shared/builderTypes';

const TRIGGER_TYPES: readonly TriggerType[] = ['Cron', 'Queue', 'Event', 'Manual', 'Webhook'];
const DEP_TYPES = ['Service', 'Database', 'Queue', 'ExternalApi', 'Cache', 'Storage'] as const;
const MESSAGING_ROLES: readonly MessagingRole[] = ['None', 'Producer', 'Consumer', 'Both'];
const MSG_FORMATS = ['json', 'avro', 'protobuf', ''] as const;
const SVC_PROTOCOLS = ['REST', 'gRPC', 'SOAP', ''] as const;

function genId() { return crypto.randomUUID(); }

function createDependency(): WorkserviceDependency {
  return { id: genId(), name: '', type: 'Service', required: true };
}

function createTopic(): MessagingTopic {
  return { id: genId(), topicName: '', entityType: '', format: '' };
}

function createConsumedService(): ConsumedService {
  return { id: genId(), serviceName: '', protocol: '' };
}

function createProducedEvent(): ProducedEvent {
  return { id: genId(), eventName: '', targetTopic: '' };
}

interface VisualWorkserviceBuilderProps {
  initialState?: WorkserviceBuilderState;
  onChange?: (state: WorkserviceBuilderState) => void;
  onSync?: (result: SyncResult) => void;
  isReadOnly?: boolean;
  className?: string;
}

/**
 * Builder visual para Workservices / Background Services — permite definir
 * jobs, triggers, I/O, retries, timeouts e dependências sem specification code.
 */
export function VisualWorkserviceBuilder({
  initialState,
  onChange,
  onSync,
  isReadOnly = false,
  className = '',
}: VisualWorkserviceBuilderProps) {
  const { t } = useTranslation();

  const [state, setState] = useState<WorkserviceBuilderState>(
    initialState ?? {
      name: '',
      trigger: 'Cron',
      schedule: '',
      description: '',
      inputs: '',
      outputs: '',
      dependencies: [],
      retries: '3',
      timeout: '300',
      errorHandling: '',
      sideEffects: '',
      owner: '',
      observabilityNotes: '',
      healthCheck: '',
      messagingRole: 'None',
      consumedTopics: [],
      producedTopics: [],
      consumedServices: [],
      producedEvents: [],
    },
  );
  const [validation, setValidation] = useState<BuilderValidationResult | null>(null);

  const update = useCallback(
    (partial: Partial<WorkserviceBuilderState>) => {
      const next = { ...state, ...partial };
      setState(next);
      onChange?.(next);
    },
    [state, onChange],
  );

  const addDependency = () => {
    update({ dependencies: [...state.dependencies, createDependency()] });
  };

  const updateDep = (id: string, partial: Partial<WorkserviceDependency>) => {
    update({ dependencies: state.dependencies.map((d) => (d.id === id ? { ...d, ...partial } : d)) });
  };

  const removeDep = (id: string) => {
    update({ dependencies: state.dependencies.filter((d) => d.id !== id) });
  };

  // ── Messaging Role helpers ──────────────────────────────────────────────────
  const isConsumer = state.messagingRole === 'Consumer' || state.messagingRole === 'Both';
  const isProducer = state.messagingRole === 'Producer' || state.messagingRole === 'Both';

  const addConsumedTopic = () => update({ consumedTopics: [...state.consumedTopics, createTopic()] });
  const updateConsumedTopic = (id: string, partial: Partial<MessagingTopic>) =>
    update({ consumedTopics: state.consumedTopics.map((t) => (t.id === id ? { ...t, ...partial } : t)) });
  const removeConsumedTopic = (id: string) =>
    update({ consumedTopics: state.consumedTopics.filter((t) => t.id !== id) });

  const addProducedTopic = () => update({ producedTopics: [...state.producedTopics, createTopic()] });
  const updateProducedTopic = (id: string, partial: Partial<MessagingTopic>) =>
    update({ producedTopics: state.producedTopics.map((t) => (t.id === id ? { ...t, ...partial } : t)) });
  const removeProducedTopic = (id: string) =>
    update({ producedTopics: state.producedTopics.filter((t) => t.id !== id) });

  const addConsumedService = () => update({ consumedServices: [...state.consumedServices, createConsumedService()] });
  const updateConsumedSvc = (id: string, partial: Partial<ConsumedService>) =>
    update({ consumedServices: state.consumedServices.map((s) => (s.id === id ? { ...s, ...partial } : s)) });
  const removeConsumedSvc = (id: string) =>
    update({ consumedServices: state.consumedServices.filter((s) => s.id !== id) });

  const addProducedEvent = () => update({ producedEvents: [...state.producedEvents, createProducedEvent()] });
  const updateProducedEvt = (id: string, partial: Partial<ProducedEvent>) =>
    update({ producedEvents: state.producedEvents.map((e) => (e.id === id ? { ...e, ...partial } : e)) });
  const removeProducedEvt = (id: string) =>
    update({ producedEvents: state.producedEvents.filter((e) => e.id !== id) });

  const handleValidate = () => { setValidation(validateWorkserviceBuilder(state)); };

  const handleGenerateSource = () => {
    const result = workserviceBuilderToYaml(state);
    onSync?.(result);
  };

  const fieldError = (field: string) => validation?.errors.find((e) => e.field === field);

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

      {/* ── Service info ── */}
      <Card>
        <CardHeader>
          <h3 className="text-xs font-semibold text-heading">
            {t('contracts.builder.workservice.info', 'Service Information')}
          </h3>
        </CardHeader>
        <CardBody className="space-y-3">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <Field label={t('contracts.builder.workservice.name', 'Service Name')} value={state.name}
              onChange={(v) => update({ name: v })} placeholder="order-processor" required
              error={fieldError('name') ? t(fieldError('name')!.messageKey, fieldError('name')!.fallback) : undefined}
              disabled={isReadOnly} />
            <Field label={t('contracts.builder.workservice.owner', 'Owner')} value={state.owner}
              onChange={(v) => update({ owner: v })} placeholder="team-orders" disabled={isReadOnly} />
          </div>
          <FieldArea label={t('contracts.builder.workservice.description', 'Description')} value={state.description}
            onChange={(v) => update({ description: v })}
            placeholder={t('contracts.builder.workservice.descPlaceholder', 'Describe what this worker/job does...')}
            disabled={isReadOnly} />
        </CardBody>
      </Card>

      {/* ── Trigger & Schedule ── */}
      <Card>
        <CardHeader>
          <h3 className="text-xs font-semibold text-heading">
            {t('contracts.builder.workservice.trigger', 'Trigger & Schedule')}
          </h3>
        </CardHeader>
        <CardBody className="space-y-3">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <FieldSelect
              label={t('contracts.builder.workservice.triggerType', 'Trigger Type')}
              value={state.trigger}
              onChange={(v) => update({ trigger: v as TriggerType })}
              options={TRIGGER_TYPES}
              disabled={isReadOnly}
            />
            {state.trigger === 'Cron' && (
              <Field label={t('contracts.builder.workservice.schedule', 'Schedule (Cron)')} value={state.schedule}
                onChange={(v) => update({ schedule: v })} placeholder="0 */5 * * *" mono required
                error={fieldError('schedule') ? t(fieldError('schedule')!.messageKey, fieldError('schedule')!.fallback) : undefined}
                disabled={isReadOnly} />
            )}
            {(state.trigger === 'Queue' || state.trigger === 'Event') && (
              <Field label={t('contracts.builder.workservice.source', 'Source')} value={state.schedule}
                onChange={(v) => update({ schedule: v })}
                placeholder={state.trigger === 'Queue' ? 'orders-queue' : 'order.created'}
                mono disabled={isReadOnly} />
            )}
            {state.trigger === 'Webhook' && (
              <Field label={t('contracts.builder.workservice.webhookUrl', 'Webhook Path')} value={state.schedule}
                onChange={(v) => update({ schedule: v })} placeholder="/webhooks/process" mono disabled={isReadOnly} />
            )}
          </div>
        </CardBody>
      </Card>

      {/* ── I/O & Behavior ── */}
      <Card>
        <CardHeader>
          <h3 className="text-xs font-semibold text-heading">
            {t('contracts.builder.workservice.io', 'Input / Output & Behavior')}
          </h3>
        </CardHeader>
        <CardBody className="space-y-3">
          <FieldArea label={t('contracts.builder.workservice.inputs', 'Inputs')} value={state.inputs}
            onChange={(v) => update({ inputs: v })}
            placeholder={t('contracts.builder.workservice.inputsPlaceholder', 'Describe expected inputs (schema, payload, parameters)...')}
            rows={2} disabled={isReadOnly} />
          <FieldArea label={t('contracts.builder.workservice.outputs', 'Outputs')} value={state.outputs}
            onChange={(v) => update({ outputs: v })}
            placeholder={t('contracts.builder.workservice.outputsPlaceholder', 'Describe expected outputs or side effects...')}
            rows={2} disabled={isReadOnly} />
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <Field label={t('contracts.builder.workservice.retries', 'Max Retries')} value={state.retries}
              onChange={(v) => update({ retries: v })} placeholder="3" disabled={isReadOnly} />
            <Field label={t('contracts.builder.workservice.timeout', 'Timeout (seconds)')} value={state.timeout}
              onChange={(v) => update({ timeout: v })} placeholder="300" disabled={isReadOnly} />
          </div>
          <FieldArea label={t('contracts.builder.workservice.errorHandling', 'Error Handling')} value={state.errorHandling}
            onChange={(v) => update({ errorHandling: v })}
            placeholder={t('contracts.builder.workservice.errorPlaceholder', 'How errors are handled (DLQ, alert, retry, skip)...')}
            rows={2} disabled={isReadOnly} />
          <FieldArea label={t('contracts.builder.workservice.sideEffects', 'Side Effects')} value={state.sideEffects}
            onChange={(v) => update({ sideEffects: v })}
            placeholder={t('contracts.builder.workservice.sideEffectsPlaceholder', 'External calls, database writes, events published...')}
            rows={2} disabled={isReadOnly} />
        </CardBody>
      </Card>

      {/* ── Structured Dependencies ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <h3 className="text-xs font-semibold text-heading">
              {t('contracts.builder.workservice.dependencies', 'Dependencies')} ({state.dependencies.length})
            </h3>
            {!isReadOnly && (
              <button type="button" onClick={addDependency} className="inline-flex items-center gap-1 px-2 py-1 text-[10px] font-medium rounded-md bg-accent/10 text-accent hover:bg-accent/20 transition-colors">
                <Plus size={10} /> {t('contracts.builder.workservice.addDep', 'Add Dependency')}
              </button>
            )}
          </div>
        </CardHeader>
        <CardBody className="space-y-2">
          {state.dependencies.length === 0 && (
            <div className="py-4 text-center text-xs text-muted">
              {t('contracts.builder.workservice.noDeps', 'No dependencies defined.')}
            </div>
          )}
          {state.dependencies.map((dep) => (
            <div key={dep.id} className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-2 items-end">
              <Field label={t('contracts.builder.workservice.depName', 'Name')} value={dep.name}
                onChange={(v) => updateDep(dep.id, { name: v })} placeholder="orders-db" disabled={isReadOnly} />
              <FieldSelect label={t('contracts.builder.workservice.depType', 'Type')} value={dep.type}
                onChange={(v) => updateDep(dep.id, { type: v as WorkserviceDependency['type'] })}
                options={DEP_TYPES} disabled={isReadOnly} />
              <FieldCheckbox label={t('contracts.builder.rest.required', 'Required')} checked={dep.required}
                onChange={(v) => updateDep(dep.id, { required: v })} disabled={isReadOnly} />
              {!isReadOnly && (
                <button type="button" onClick={() => removeDep(dep.id)} className="text-muted hover:text-danger transition-colors pb-1">
                  <Trash2 size={11} />
                </button>
              )}
            </div>
          ))}
        </CardBody>
      </Card>

      {/* ── Messaging Role — Producer / Consumer ── */}
      <Card>
        <CardHeader>
          <h3 className="text-xs font-semibold text-heading">
            {t('contracts.builder.workservice.messagingRole', 'Messaging Role')}
          </h3>
        </CardHeader>
        <CardBody className="space-y-3">
          <FieldSelect
            label={t('contracts.builder.workservice.messagingRoleLabel', 'Role')}
            value={state.messagingRole}
            onChange={(v) => update({ messagingRole: v as MessagingRole })}
            options={MESSAGING_ROLES}
            disabled={isReadOnly}
          />
          <p className="text-[10px] text-muted">
            {t('contracts.builder.workservice.messagingRoleHint', 'Indicate if this background service produces and/or consumes messages from topics/queues.')}
          </p>

          {/* Consumed Topics */}
          {isConsumer && (
            <div className="space-y-2 pt-2 border-t border-edge">
              <div className="flex items-center justify-between">
                <span className="text-[10px] font-medium text-heading uppercase tracking-wider">
                  {t('contracts.builder.workservice.consumedTopics', 'Consumed Topics')} ({state.consumedTopics.length})
                </span>
                {!isReadOnly && (
                  <button type="button" onClick={addConsumedTopic} className="inline-flex items-center gap-1 px-2 py-1 text-[10px] font-medium rounded-md bg-accent/10 text-accent hover:bg-accent/20 transition-colors">
                    <Plus size={10} /> {t('contracts.builder.workservice.addTopic', 'Add Topic')}
                  </button>
                )}
              </div>
              {state.consumedTopics.length === 0 && (
                <div className="py-2 text-center text-xs text-muted">
                  {t('contracts.builder.workservice.noConsumedTopics', 'No consumed topics defined.')}
                </div>
              )}
              {state.consumedTopics.map((topic) => (
                <div key={topic.id} className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-2 items-end">
                  <Field label={t('contracts.builder.workservice.topicName', 'Topic')} value={topic.topicName}
                    onChange={(v) => updateConsumedTopic(topic.id, { topicName: v })} placeholder="orders.created" mono disabled={isReadOnly} />
                  <Field label={t('contracts.builder.workservice.entityType', 'Entity Type')} value={topic.entityType}
                    onChange={(v) => updateConsumedTopic(topic.id, { entityType: v })} placeholder="OrderEvent" disabled={isReadOnly} />
                  <FieldSelect label={t('contracts.builder.workservice.format', 'Format')} value={topic.format}
                    onChange={(v) => updateConsumedTopic(topic.id, { format: v as MessagingTopic['format'] })}
                    options={MSG_FORMATS} disabled={isReadOnly} />
                  {!isReadOnly && (
                    <button type="button" onClick={() => removeConsumedTopic(topic.id)} className="text-muted hover:text-danger transition-colors pb-1">
                      <Trash2 size={11} />
                    </button>
                  )}
                </div>
              ))}
            </div>
          )}

          {/* Produced Topics */}
          {isProducer && (
            <div className="space-y-2 pt-2 border-t border-edge">
              <div className="flex items-center justify-between">
                <span className="text-[10px] font-medium text-heading uppercase tracking-wider">
                  {t('contracts.builder.workservice.producedTopics', 'Produced Topics')} ({state.producedTopics.length})
                </span>
                {!isReadOnly && (
                  <button type="button" onClick={addProducedTopic} className="inline-flex items-center gap-1 px-2 py-1 text-[10px] font-medium rounded-md bg-accent/10 text-accent hover:bg-accent/20 transition-colors">
                    <Plus size={10} /> {t('contracts.builder.workservice.addTopic', 'Add Topic')}
                  </button>
                )}
              </div>
              {state.producedTopics.length === 0 && (
                <div className="py-2 text-center text-xs text-muted">
                  {t('contracts.builder.workservice.noProducedTopics', 'No produced topics defined.')}
                </div>
              )}
              {state.producedTopics.map((topic) => (
                <div key={topic.id} className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-2 items-end">
                  <Field label={t('contracts.builder.workservice.topicName', 'Topic')} value={topic.topicName}
                    onChange={(v) => updateProducedTopic(topic.id, { topicName: v })} placeholder="orders.processed" mono disabled={isReadOnly} />
                  <Field label={t('contracts.builder.workservice.entityType', 'Entity Type')} value={topic.entityType}
                    onChange={(v) => updateProducedTopic(topic.id, { entityType: v })} placeholder="OrderProcessedEvent" disabled={isReadOnly} />
                  <FieldSelect label={t('contracts.builder.workservice.format', 'Format')} value={topic.format}
                    onChange={(v) => updateProducedTopic(topic.id, { format: v as MessagingTopic['format'] })}
                    options={MSG_FORMATS} disabled={isReadOnly} />
                  {!isReadOnly && (
                    <button type="button" onClick={() => removeProducedTopic(topic.id)} className="text-muted hover:text-danger transition-colors pb-1">
                      <Trash2 size={11} />
                    </button>
                  )}
                </div>
              ))}
            </div>
          )}

          {/* Consumed Services */}
          {isConsumer && (
            <div className="space-y-2 pt-2 border-t border-edge">
              <div className="flex items-center justify-between">
                <span className="text-[10px] font-medium text-heading uppercase tracking-wider">
                  {t('contracts.builder.workservice.consumedServices', 'Consumed Services')} ({state.consumedServices.length})
                </span>
                {!isReadOnly && (
                  <button type="button" onClick={addConsumedService} className="inline-flex items-center gap-1 px-2 py-1 text-[10px] font-medium rounded-md bg-accent/10 text-accent hover:bg-accent/20 transition-colors">
                    <Plus size={10} /> {t('contracts.builder.workservice.addService', 'Add Service')}
                  </button>
                )}
              </div>
              {state.consumedServices.length === 0 && (
                <div className="py-2 text-center text-xs text-muted">
                  {t('contracts.builder.workservice.noConsumedServices', 'No consumed services defined.')}
                </div>
              )}
              {state.consumedServices.map((svc) => (
                <div key={svc.id} className="grid grid-cols-1 sm:grid-cols-3 gap-2 items-end">
                  <Field label={t('contracts.builder.workservice.serviceName', 'Service Name')} value={svc.serviceName}
                    onChange={(v) => updateConsumedSvc(svc.id, { serviceName: v })} placeholder="user-api" disabled={isReadOnly} />
                  <FieldSelect label={t('contracts.builder.workservice.protocol', 'Protocol')} value={svc.protocol}
                    onChange={(v) => updateConsumedSvc(svc.id, { protocol: v as ConsumedService['protocol'] })}
                    options={SVC_PROTOCOLS} disabled={isReadOnly} />
                  {!isReadOnly && (
                    <button type="button" onClick={() => removeConsumedSvc(svc.id)} className="text-muted hover:text-danger transition-colors pb-1">
                      <Trash2 size={11} />
                    </button>
                  )}
                </div>
              ))}
            </div>
          )}

          {/* Produced Events */}
          {isProducer && (
            <div className="space-y-2 pt-2 border-t border-edge">
              <div className="flex items-center justify-between">
                <span className="text-[10px] font-medium text-heading uppercase tracking-wider">
                  {t('contracts.builder.workservice.producedEvents', 'Produced Events')} ({state.producedEvents.length})
                </span>
                {!isReadOnly && (
                  <button type="button" onClick={addProducedEvent} className="inline-flex items-center gap-1 px-2 py-1 text-[10px] font-medium rounded-md bg-accent/10 text-accent hover:bg-accent/20 transition-colors">
                    <Plus size={10} /> {t('contracts.builder.workservice.addEvent', 'Add Event')}
                  </button>
                )}
              </div>
              {state.producedEvents.length === 0 && (
                <div className="py-2 text-center text-xs text-muted">
                  {t('contracts.builder.workservice.noProducedEvents', 'No produced events defined.')}
                </div>
              )}
              {state.producedEvents.map((evt) => (
                <div key={evt.id} className="grid grid-cols-1 sm:grid-cols-3 gap-2 items-end">
                  <Field label={t('contracts.builder.workservice.eventName', 'Event Name')} value={evt.eventName}
                    onChange={(v) => updateProducedEvt(evt.id, { eventName: v })} placeholder="OrderProcessed" disabled={isReadOnly} />
                  <Field label={t('contracts.builder.workservice.targetTopic', 'Target Topic')} value={evt.targetTopic}
                    onChange={(v) => updateProducedEvt(evt.id, { targetTopic: v })} placeholder="orders.processed" mono disabled={isReadOnly} />
                  {!isReadOnly && (
                    <button type="button" onClick={() => removeProducedEvt(evt.id)} className="text-muted hover:text-danger transition-colors pb-1">
                      <Trash2 size={11} />
                    </button>
                  )}
                </div>
              ))}
            </div>
          )}
        </CardBody>
      </Card>

      {/* ── Observability & Health ── */}
      <Card>
        <CardHeader>
          <h3 className="text-xs font-semibold text-heading">
            {t('contracts.builder.workservice.deps', 'Observability & Health')}
          </h3>
        </CardHeader>
        <CardBody className="space-y-3">
          <FieldArea label={t('contracts.builder.workservice.observability', 'Observability')} value={state.observabilityNotes}
            onChange={(v) => update({ observabilityNotes: v })}
            placeholder={t('contracts.builder.workservice.obsPlaceholder', 'Metrics, logs, traces, health checks...')}
            rows={2} disabled={isReadOnly} />
          <Field label={t('contracts.builder.workservice.healthCheck', 'Health Check Endpoint')} value={state.healthCheck}
            onChange={(v) => update({ healthCheck: v })} placeholder="/health" mono disabled={isReadOnly} />
        </CardBody>
      </Card>

      {/* ── Action bar ── */}
      {!isReadOnly && (
        <div className="flex items-center justify-end gap-2">
          <button type="button" onClick={handleValidate}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-[10px] font-medium rounded-md bg-elevated border border-edge text-body hover:bg-elevated/80 transition-colors">
            {t('contracts.builder.workservice.validate', 'Validate')}
          </button>
          <button type="button" onClick={handleGenerateSource}
            className="inline-flex items-center gap-1.5 px-3 py-1.5 text-[10px] font-medium rounded-md bg-accent/10 text-accent border border-accent/20 hover:bg-accent/20 transition-colors">
            {t('contracts.builder.workservice.generateSource', 'Generate Definition')}
          </button>
        </div>
      )}
    </div>
  );
}
