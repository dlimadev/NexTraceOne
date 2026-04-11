/**
 * Builder visual genérico para contratos mainframe legados.
 *
 * Cobre 4 tipos: Copybook, MqMessage, FixedLayout, CicsCommarea.
 * Campos específicos são apresentados condicionalmente conforme o `kind`.
 *
 * Permite criar/editar visualmente:
 * - metadados comuns: nome, versão, descrição, encoding, total length, owner
 * - campos específicos por tipo (programName, queueManager, transactionId, etc.)
 * - definição de fields: level, name, type, length, offset, PIC, occurs, redefines
 * - sincronização com NTO YAML via builderSync
 */
import { useState, useCallback } from 'react';
import { useTranslation } from 'react-i18next';
import { Plus, Trash2, ChevronDown, ChevronRight, AlertCircle } from 'lucide-react';
import { Card, CardBody, CardHeader } from '../../../../components/Card';
import {
  Field, FieldArea, FieldSelect,
} from './shared/BuilderFormPrimitives';
import { validateLegacyContractBuilder } from './shared/builderValidation';
import { legacyContractBuilderToYaml } from './shared/builderSync';
import type {
  LegacyContractBuilderState,
  LegacyContractKind,
  LegacyField,
  LegacyFieldType,
  LegacyEncoding,
  BuilderValidationResult,
  SyncResult,
} from './shared/builderTypes';

const ENCODING_OPTIONS = ['EBCDIC', 'ASCII', 'UTF-8'] as const;
const FIELD_TYPE_OPTIONS: readonly LegacyFieldType[] = ['alphanumeric', 'numeric', 'packed-decimal', 'binary', 'display', 'group', 'filler'] as const;

const KIND_LABELS: Record<LegacyContractKind, string> = {
  Copybook: 'COBOL Copybook',
  MqMessage: 'MQ Message',
  FixedLayout: 'Fixed Layout',
  CicsCommarea: 'CICS Commarea',
};

let nextId = 1;
function genId() { return `lgc-${nextId++}`; }

function createField(): LegacyField {
  return { id: genId(), name: '', level: '05', type: 'alphanumeric', length: '', offset: '', picture: '', description: '', occurs: '', redefines: '' };
}

interface VisualLegacyContractBuilderProps {
  kind: LegacyContractKind;
  initialState?: LegacyContractBuilderState;
  onChange?: (state: LegacyContractBuilderState) => void;
  onSync?: (result: SyncResult) => void;
  isReadOnly?: boolean;
  className?: string;
}

/**
 * Builder visual genérico para contratos legados — Copybook, MQ Message,
 * Fixed Layout e CICS Commarea. Campos específicos por tipo são
 * exibidos condicionalmente com base na prop `kind`.
 */
export function VisualLegacyContractBuilder({
  kind,
  initialState,
  onChange,
  onSync,
  isReadOnly = false,
  className = '',
}: VisualLegacyContractBuilderProps) {
  const { t } = useTranslation();

  const [state, setState] = useState<LegacyContractBuilderState>(
    initialState ?? {
      kind,
      name: '',
      version: '1.0.0',
      description: '',
      encoding: 'EBCDIC',
      totalLength: '',
      owner: '',
      programName: '',
      queueManager: '',
      queueName: '',
      messageFormat: '',
      transactionId: '',
      commareaLength: '',
      fields: [],
      observabilityNotes: '',
    },
  );
  const [expandedId, setExpandedId] = useState<string | null>(null);
  const [validation, setValidation] = useState<BuilderValidationResult | null>(null);

  const update = useCallback(
    (partial: Partial<LegacyContractBuilderState>) => {
      const next = { ...state, ...partial };
      setState(next);
      onChange?.(next);
    },
    [state, onChange],
  );

  const addField = () => {
    const f = createField();
    update({ fields: [...state.fields, f] });
    setExpandedId(f.id);
  };

  const updateField = (id: string, partial: Partial<LegacyField>) => {
    update({ fields: state.fields.map((f) => (f.id === id ? { ...f, ...partial } : f)) });
  };

  const removeField = (id: string) => {
    update({ fields: state.fields.filter((f) => f.id !== id) });
    if (expandedId === id) setExpandedId(null);
  };

  const handleValidate = () => { setValidation(validateLegacyContractBuilder(state)); };

  const handleGenerateSource = () => {
    const result = legacyContractBuilderToYaml(state);
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

      {/* ── Contract metadata ── */}
      <Card>
        <CardHeader>
          <h3 className="text-xs font-semibold text-heading">
            {t('contracts.builder.legacy.title', 'Legacy Contract Builder')} — {KIND_LABELS[kind]}
          </h3>
        </CardHeader>
        <CardBody className="space-y-3">
          <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
            <Field
              label={t('contracts.builder.workservice.name', 'Contract Name')}
              value={state.name}
              onChange={(v) => update({ name: v })}
              placeholder={t('contracts.legacy.placeholder.recordName', 'CUSTOMER-RECORD')}
              required
              error={fieldError('name') ? t(fieldError('name')!.messageKey, fieldError('name')!.fallback) : undefined}
              disabled={isReadOnly}
            />
            <Field
              label={t('contracts.builder.rest.version', 'Version')}
              value={state.version}
              onChange={(v) => update({ version: v })}
              placeholder="1.0.0"
              disabled={isReadOnly}
            />
          </div>
          <div className="grid grid-cols-1 md:grid-cols-3 gap-3">
            <FieldSelect
              label={t('contracts.builder.legacy.encoding', 'Character Encoding')}
              value={state.encoding}
              onChange={(v) => update({ encoding: v as LegacyEncoding })}
              options={ENCODING_OPTIONS}
              required
              disabled={isReadOnly}
            />
            <Field
              label={t('contracts.builder.legacy.totalLength', 'Total Record Length (bytes)')}
              value={state.totalLength}
              onChange={(v) => update({ totalLength: v })}
              placeholder="1024"
              error={fieldError('totalLength') ? t(fieldError('totalLength')!.messageKey, fieldError('totalLength')!.fallback) : undefined}
              disabled={isReadOnly}
            />
            <Field
              label={t('contracts.builder.rest.contact', 'Owner')}
              value={state.owner}
              onChange={(v) => update({ owner: v })}
              placeholder="mainframe-team"
              disabled={isReadOnly}
            />
          </div>
          <FieldArea
            label={t('contracts.builder.rest.description', 'Description')}
            value={state.description}
            onChange={(v) => update({ description: v })}
            placeholder={t('contracts.builder.soap.descPlaceholder', 'Describe this contract...')}
            disabled={isReadOnly}
          />
        </CardBody>
      </Card>

      {/* ── Type-specific fields ── */}
      {kind === 'Copybook' && (
        <Card>
          <CardHeader>
            <h3 className="text-xs font-semibold text-heading">{KIND_LABELS.Copybook}</h3>
          </CardHeader>
          <CardBody>
            <Field
              label={t('contracts.builder.legacy.programName', 'COBOL Program Name')}
              value={state.programName}
              onChange={(v) => update({ programName: v })}
              placeholder={t('contracts.legacy.placeholder.programName', 'CUSTPROG')}
              required
              mono
              error={fieldError('programName') ? t(fieldError('programName')!.messageKey, fieldError('programName')!.fallback) : undefined}
              disabled={isReadOnly}
            />
          </CardBody>
        </Card>
      )}

      {kind === 'MqMessage' && (
        <Card>
          <CardHeader>
            <h3 className="text-xs font-semibold text-heading">{KIND_LABELS.MqMessage}</h3>
          </CardHeader>
          <CardBody className="space-y-3">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <Field
                label={t('contracts.builder.legacy.queueManager', 'Queue Manager')}
                value={state.queueManager}
                onChange={(v) => update({ queueManager: v })}
                placeholder={t('contracts.legacy.placeholder.queueManager', 'QMGR01')}
                mono
                disabled={isReadOnly}
              />
              <Field
                label={t('contracts.builder.legacy.queueName', 'Queue Name')}
                value={state.queueName}
                onChange={(v) => update({ queueName: v })}
                placeholder={t('contracts.legacy.placeholder.queueName', 'CUST.REQUEST.Q')}
                required
                mono
                error={fieldError('queueName') ? t(fieldError('queueName')!.messageKey, fieldError('queueName')!.fallback) : undefined}
                disabled={isReadOnly}
              />
            </div>
            <Field
              label={t('contracts.builder.legacy.messageFormat', 'Message Format')}
              value={state.messageFormat}
              onChange={(v) => update({ messageFormat: v })}
              placeholder={t('contracts.legacy.placeholder.messageFormat', 'MQHRF2')}
              mono
              disabled={isReadOnly}
            />
          </CardBody>
        </Card>
      )}

      {kind === 'CicsCommarea' && (
        <Card>
          <CardHeader>
            <h3 className="text-xs font-semibold text-heading">{KIND_LABELS.CicsCommarea}</h3>
          </CardHeader>
          <CardBody className="space-y-3">
            <div className="grid grid-cols-1 md:grid-cols-2 gap-3">
              <Field
                label={t('contracts.builder.legacy.transactionId', 'CICS Transaction ID')}
                value={state.transactionId}
                onChange={(v) => update({ transactionId: v })}
                placeholder={t('contracts.legacy.placeholder.copybook', 'CUST')}
                required
                mono
                error={fieldError('transactionId') ? t(fieldError('transactionId')!.messageKey, fieldError('transactionId')!.fallback) : undefined}
                disabled={isReadOnly}
              />
              <Field
                label={t('contracts.builder.legacy.commareaLength', 'COMMAREA Length (bytes)')}
                value={state.commareaLength}
                onChange={(v) => update({ commareaLength: v })}
                placeholder="2048"
                mono
                disabled={isReadOnly}
              />
            </div>
          </CardBody>
        </Card>
      )}

      {/* ── Fields definition ── */}
      <Card>
        <CardHeader>
          <div className="flex items-center justify-between">
            <h3 className="text-xs font-semibold text-heading">
              {t('contracts.builder.rest.parameters', 'Fields')} ({state.fields.length})
            </h3>
            {!isReadOnly && (
              <button type="button" onClick={addField} className="inline-flex items-center gap-1 px-2 py-1 text-[10px] font-medium rounded-md bg-accent/10 text-accent hover:bg-accent/20 transition-colors">
                <Plus size={10} /> {t('contracts.builder.legacy.addField', 'Add Field')}
              </button>
            )}
          </div>
        </CardHeader>
        <CardBody className="p-0">
          {state.fields.length === 0 && (
            <div className="py-8 text-center text-xs text-muted">
              {t('contracts.builder.legacy.noFields', 'No fields defined yet. Add your first field definition.')}
            </div>
          )}
          <div className="divide-y divide-edge">
            {state.fields.map((f) => {
              const isExpanded = expandedId === f.id;
              return (
                <div key={f.id} className="group">
                  <button type="button" onClick={() => setExpandedId(isExpanded ? null : f.id)} className="w-full flex items-center gap-3 px-4 py-3 text-left hover:bg-elevated/30 transition-colors">
                    {isExpanded ? <ChevronDown size={12} className="text-muted" /> : <ChevronRight size={12} className="text-muted" />}
                    <span className="px-2 py-0.5 text-[10px] font-bold rounded bg-accent/15 text-accent border border-accent/25">{f.level || '??'}</span>
                    <span className="text-xs font-mono text-heading flex-1 truncate">{f.name || t('contracts.builder.soap.unnamed', 'Unnamed Field')}</span>
                    <span className="text-[10px] text-muted">{f.type}</span>
                    {f.picture && <span className="text-[10px] text-muted font-mono truncate max-w-[100px]">{f.picture}</span>}
                    {!isReadOnly && (
                      <button type="button" onClick={(e) => { e.stopPropagation(); removeField(f.id); }} className="opacity-0 group-hover:opacity-100 text-muted hover:text-danger transition-all">
                        <Trash2 size={12} />
                      </button>
                    )}
                  </button>
                  {isExpanded && (
                    <div className="px-4 pb-4 pt-1 bg-elevated/10 space-y-3">
                      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
                        <Field label={t('contracts.builder.legacy.fieldLevel', 'Level')} value={f.level}
                          onChange={(v) => updateField(f.id, { level: v })} placeholder="05" mono disabled={isReadOnly} />
                        <Field label={t('contracts.builder.legacy.fieldName', 'Field Name')} value={f.name}
                          onChange={(v) => updateField(f.id, { name: v })} placeholder={t('contracts.legacy.placeholder.fieldName', 'CUST-NAME')} required mono disabled={isReadOnly} />
                        <FieldSelect label={t('contracts.builder.legacy.fieldType', 'Data Type')} value={f.type}
                          onChange={(v) => updateField(f.id, { type: v as LegacyFieldType })} options={FIELD_TYPE_OPTIONS} disabled={isReadOnly} />
                        <Field label={t('contracts.builder.legacy.fieldLength', 'Length')} value={f.length}
                          onChange={(v) => updateField(f.id, { length: v })} placeholder="30" mono disabled={isReadOnly} />
                      </div>
                      <div className="grid grid-cols-2 md:grid-cols-4 gap-3">
                        <Field label={t('contracts.builder.legacy.fieldOffset', 'Offset')} value={f.offset}
                          onChange={(v) => updateField(f.id, { offset: v })} placeholder="0" mono disabled={isReadOnly} />
                        <Field label={t('contracts.builder.legacy.fieldPicture', 'PIC Clause')} value={f.picture}
                          onChange={(v) => updateField(f.id, { picture: v })} placeholder={t('contracts.legacy.placeholder.picture', 'PIC X(30)')} mono disabled={isReadOnly} />
                        <Field label={t('contracts.builder.legacy.fieldOccurs', 'OCCURS')} value={f.occurs}
                          onChange={(v) => updateField(f.id, { occurs: v })} placeholder="10" mono disabled={isReadOnly} />
                        <Field label={t('contracts.builder.legacy.fieldRedefines', 'REDEFINES')} value={f.redefines}
                          onChange={(v) => updateField(f.id, { redefines: v })} placeholder={t('contracts.legacy.placeholder.redefines', 'CUST-ID')} mono disabled={isReadOnly} />
                      </div>
                      <FieldArea label={t('contracts.builder.rest.description', 'Description')} value={f.description}
                        onChange={(v) => updateField(f.id, { description: v })} rows={2} disabled={isReadOnly} />
                    </div>
                  )}
                </div>
              );
            })}
          </div>
        </CardBody>
      </Card>

      {/* ── Observability ── */}
      <Card>
        <CardHeader>
          <h3 className="text-xs font-semibold text-heading">
            {t('contracts.builder.workservice.observability', 'Observability Notes')}
          </h3>
        </CardHeader>
        <CardBody>
          <FieldArea
            label=""
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
