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
  TriggerType,
  BuilderValidationResult,
  SyncResult,
} from './shared/builderTypes';

const TRIGGER_TYPES: readonly TriggerType[] = ['Cron', 'Queue', 'Event', 'Manual', 'Webhook'];
const DEP_TYPES = ['Service', 'Database', 'Queue', 'ExternalApi', 'Cache', 'Storage'] as const;

let nextId = 1;
function genId() { return `dep-${nextId++}`; }

function createDependency(): WorkserviceDependency {
  return { id: genId(), name: '', type: 'Service', required: true };
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
